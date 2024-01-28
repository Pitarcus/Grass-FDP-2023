using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class GrassMaster : MonoBehaviour
{
    // General grass parameters
    [Range(1, 2000)]
    [SerializeField] int grassSquareSize = 128;
    [Range(1, 10)]
    [SerializeField] int grassDensity = 1;
    [Range(0, 2)]
    [SerializeField] float offsetXAmount = 0.5f;
    [Range(0, 2)]
    [SerializeField] float offsetYAmount = 0.5f;
    [Range(0, 1000)]
    [SerializeField] float cutoffDistance = 65f;
    [Range(0, 500)]
    [SerializeField] float LODDistance = 25f;

    Texture2D[] positionMaps;   // Should be an array with all of the textures? maybe the quadtree stores it
    Texture2D[] heightMaps;   // Should be an array with all of the textures? maybe the quadtree stores it
    [SerializeField] float heightDisplacementStrenght = 600f;

    [Space]

    // Quadtree parameters
    [SerializeField] int quadtreeMaxDepth = 4;

    [Range(0, 1000)]
    [SerializeField] float quadtreeCutoffDistance = 200f;
    [Range(0, 500)]
    [SerializeField] float leafCutoffDistance = 120f;
    GrassQuadtree[] _grassQuadtrees;

    List<GrassQuadtree> _visibleGrassQuadtrees;
    List<GrassQuadtree> _pastVisibleGrassQuadtrees;

    [Space]

    // The compute shader, assigned in editor
    [SerializeField] ComputeShader grassCompute;
    [SerializeField] ComputeShader cullGrassCompute;

    uint _numThreadsX;
    uint _numThreadsY;

    [Space]

    // Grass mesh and material
    [SerializeField] Mesh grassMesh;
    [SerializeField] Mesh grassMeshLOD;
    [SerializeField] Material grassMaterial;

    [Space]

    [SerializeField] GrassMaterialParameters_SO grassParameters;


    // PRIVATE VARIABLES

    private Camera _currentCamera;
    private float _cameraAngleToGroundNormalized;

    // Data structure to communicate GPU and CPU
    private struct GrassData
    {
        public Vector3 position;
        public Vector3 scale;
        Matrix4x4 objectToWorld;
    }
    private struct SmallVegetationData
    {
        public Vector3 position;
        public Vector3 scale;
        public int type;
    }

    private uint[] _mainLODArgs;
    private uint[] _lowerLODArgs;


    // Internal values for grass positions
    private int _grassResolution;
    private float _grassStep;

    // CMD Buffers for rendering
    GraphicsBuffer commandBuf;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
    const int commandCount = 1;


    // Grass material properties to id
    static readonly int
        sizeId = Shader.PropertyToID("_Size"),
        resolutionId = Shader.PropertyToID("_Resolution"),
        stepId = Shader.PropertyToID("_Step"),
        offsetXAmountId = Shader.PropertyToID("_OffsetXAmount"),
        offsetYAmountId = Shader.PropertyToID("_OffsetYAmount"),
        heightDisplacementStrenghtId = Shader.PropertyToID("_HeightDisplacementStrenght"),
        positionMapID = Shader.PropertyToID("_PositionMap"),

        bottomColorId = Shader.PropertyToID("_BottomColor"),
        topColorId = Shader.PropertyToID("_TopColor"),
        tipColorId = Shader.PropertyToID("_TipColor"),
        SSSColorId = Shader.PropertyToID("_SSSColor"),
        worldUVTilingId = Shader.PropertyToID("_WorldUV_Tiling"),
        scaleYId = Shader.PropertyToID("_ScaleY"),
        randomYScaleNoiseId = Shader.PropertyToID("_RandomYNoiseScale"),
        minRandomYId = Shader.PropertyToID("_MinRandomY"),
        maxRandomYId = Shader.PropertyToID("_MaxRandomY"),
        maxYRotationId = Shader.PropertyToID("_MaxYRotation"),
        randomYRotationNoiseId = Shader.PropertyToID("_RandomYRotationNoiseScale"),
        maxBendId = Shader.PropertyToID("_MaxBend"),
        randomBendId = Shader.PropertyToID("_RandomBend"),
        bendRandomnessScaleId = Shader.PropertyToID("_BendRandomnessScale"),
        baseWindDisplacementId = Shader.PropertyToID("_BaseWindDisplacement"),
        baseWindYDisplacementId = Shader.PropertyToID("_BaseWindYDisplacement"),
        staticWindXZMultiplierId = Shader.PropertyToID("_StaticWindXZMultiplier"),
        staticWindYMultiplierId = Shader.PropertyToID("_StaticWindY"),
        staticWindBladeHashIntensityId = Shader.PropertyToID("_SaticWindBladeHashIntensity"),
        dynamicWindStrengthId = Shader.PropertyToID("_DynamicWindStrength"),
        dynamicWindNoiseStrengthId = Shader.PropertyToID("_DynamicWindNoiseStrenght"),
        playerPositionModifierXId = Shader.PropertyToID("_PositionModifierX"),
        playerPositionModifierYId = Shader.PropertyToID("_PositionModifierY"),
        playerPositionModifierZId = Shader.PropertyToID("_PositionModifierZ"),
        smooothnessId = Shader.PropertyToID("_Smoothness"),
        ambientOclussionId = Shader.PropertyToID("_Ambient_Occlusion")
        ;


    // CONST
    //private int _positionsBufferSize = 3 * sizeof(float); // 3 floats per position * 4 bytes per float
    //private int _scalesBufferSize = 4;
    private int _grassDataBufferSize = 3 * sizeof(float) + 3 * sizeof(float);
    private int _nodeResolution = 0;


    List<GrassQuadtree> debugList;

    // ------------- FUNCTIONS --------------

    private void Start()
    {
        InitializeGrass();
    }

    void InitializeGrass()
    {
        _grassResolution = grassSquareSize * grassDensity;
        _grassStep = grassSquareSize / (float)_grassResolution;

        // A quadtree for each "tile"
        GameObject[] grassPainters = GameObject.FindGameObjectsWithTag("GrassPainter");
        positionMaps = new Texture2D[grassPainters.Length];
        heightMaps = new Texture2D[grassPainters.Length];
        _grassQuadtrees = new GrassQuadtree[grassPainters.Length];

        // GENERATE QUADTREE
        for (int i = 0; i < grassPainters.Length; i++)
        {
            positionMaps[i] = grassPainters[i].GetComponent<TerrainPainterComponent>().GetMaskTexture();
            heightMaps[i] = grassPainters[i].GetComponent<TerrainPainterComponent>().GetHeightMap();

            _grassQuadtrees[i] = new GrassQuadtree(new AABB(grassPainters[i].transform.position.x, grassPainters[i].transform.position.z, grassSquareSize / 2),   // last number is half the size of the terrain
                0,
                quadtreeMaxDepth,
                positionMaps[i],
                heightMaps[i],
                heightDisplacementStrenght,
                new Vector2(0, 0));

            _grassQuadtrees[i].SetRootHeightmap(heightMaps[i]);
            _grassQuadtrees[i].Build();
        }


        _visibleGrassQuadtrees = new List<GrassQuadtree>();
        _pastVisibleGrassQuadtrees = new List<GrassQuadtree>();

        _mainLODArgs = new uint[5] { 0, 0, 0, 0, 0 };
        _mainLODArgs[0] = (uint)grassMesh.GetIndexCount(0);
        _mainLODArgs[1] = (uint)0;
        _mainLODArgs[2] = (uint)grassMesh.GetIndexStart(0);
        _mainLODArgs[3] = (uint)grassMesh.GetBaseVertex(0);

        _lowerLODArgs = new uint[5] { 0, 0, 0, 0, 0 };
        _lowerLODArgs[0] = (uint)grassMeshLOD.GetIndexCount(0);
        _lowerLODArgs[1] = (uint)0;
        _lowerLODArgs[2] = (uint)grassMeshLOD.GetIndexStart(0);
        _lowerLODArgs[3] = (uint)grassMeshLOD.GetBaseVertex(0);

        // Commands for rendering stuff
        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[commandCount];
    }

    void UpdateGrassAttributes()
    {
        _grassResolution = grassSquareSize * grassDensity;
        _grassStep = grassSquareSize / (float)_grassResolution;
    }


    #region GoodPracticesCleanness
    // Stuff for creating the buffer
    void OnEnable()
    {
        grassCompute.GetKernelThreadGroupSizes(0, out _numThreadsX, out _numThreadsY, out _);
        
        #if UNITY_EDITOR
        InitializeGrass();
        #endif
    }


    // Making sure the garbage collector does its job and hot reload stuff
    void OnDisable()
    {
        FreeQuadtreeNodes();
    }
    #endregion

    private void SetAllChildren(ref GrassQuadtree qt)
    {
        SetQuadtreeNode(ref qt);

        if(qt.subdivided)
        {
            SetAllChildren(ref qt.northEast);
            SetAllChildren(ref qt.northWest);
            SetAllChildren(ref qt.southEast);
            SetAllChildren(ref qt.southWest);
        }
    }

    private void SetQuadtreeNode(ref GrassQuadtree qt)
    {
        if (!qt.subdivided && qt.containsGrass)   // Leaf node with grass
        {
            if(!qt.hasBeenSet)
                _nodeResolution = (int)(qt.boundary.halfDimension * 2 * grassDensity);

            if (!qt.hasBeenSet)
                qt.grassCompute = Resources.Load<ComputeShader>("GrassCompute");

            qt.grassDataBuffer = new ComputeBuffer(_nodeResolution * _nodeResolution, _grassDataBufferSize, ComputeBufferType.Append);
            qt.grassDataBuffer.SetCounterValue(0);

            qt.argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
            qt.argsLODBuffer= new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);

            qt.grassCompute.SetInt(sizeId, (int)qt.boundary.halfDimension * 2);

            qt.grassCompute.SetInt("heightmapWidth", qt.rootHeightmap.width);
            qt.grassCompute.SetInt("heightmapHeight", qt.rootHeightmap.height);
            qt.grassCompute.SetInt("heightmapSampleOffsetX", qt.textureOffsetX);
            qt.grassCompute.SetInt("heightmapSampleOffsetY", qt.textureOffsetY);

            qt.grassCompute.SetInt(resolutionId, _nodeResolution);
            qt.grassCompute.SetInt("_rootSize", grassSquareSize);
            qt.grassCompute.SetFloat(stepId, _grassStep);
            qt.grassCompute.SetFloat("_NodePositionX", qt.boundary.p.x);
            qt.grassCompute.SetFloat("_NodePositionY", qt.boundary.p.y);
            qt.grassCompute.SetFloat(offsetXAmountId, offsetXAmount);
            qt.grassCompute.SetFloat(offsetYAmountId, offsetYAmount);
            qt.grassCompute.SetFloat(heightDisplacementStrenghtId, heightDisplacementStrenght);

            qt.grassCompute.SetTexture(0, "_HeightMap", qt.rootHeightmap);
            qt.grassCompute.SetTexture(0, positionMapID, qt.grassMask);
            qt.grassCompute.SetBuffer(0, "_GrassData", qt.grassDataBuffer);

            qt.grassDataBuffer.SetCounterValue(0);
            qt.grassCompute.Dispatch(0, (int)(qt.boundary.halfDimension * 2 * grassDensity / _numThreadsX), (int)(qt.boundary.halfDimension * 2 * grassDensity / _numThreadsY), 1);

            uint[] newArgs = new uint[5] { 0, 1, 0, 0, 0 };
            qt.argsLODBuffer.SetData(_lowerLODArgs);
            ComputeBuffer.CopyCount(qt.grassDataBuffer, qt.argsLODBuffer, sizeof(uint));
            qt.argsLODBuffer.GetData(newArgs);

            //uint[] newArgs = new uint[5] { 0, 1, 0, 0, 0 };
            //qt.argsBuffer.SetData(_mainLODArgs);
            //ComputeBuffer.CopyCount(qt.grassDataBuffer, qt.argsBuffer, sizeof(uint));
            //qt.argsBuffer.GetData(newArgs);

            qt.numberOfGrassBlades = newArgs[1];

            qt.culledGrassDataBuffer = new ComputeBuffer((int)qt.numberOfGrassBlades, _grassDataBufferSize, ComputeBufferType.Append);
            qt.culledGrassDataBufferLOD = new ComputeBuffer((int)qt.numberOfGrassBlades, _grassDataBufferSize, ComputeBufferType.Append);


            // Material parameters
            qt.material = new Material(grassMaterial);
            qt.materialLOD = new Material(grassMaterial);
            qt.material.SetBuffer("_GrassData", qt.culledGrassDataBuffer);
            qt.materialLOD.SetBuffer("_GrassData", qt.culledGrassDataBufferLOD);
            SetMaterialProperties(ref qt.material);
            SetMaterialProperties(ref qt.materialLOD);

            qt.hasBeenSet = true;
        }
    }


    private void SetMaterialProperties(ref Material grassMaterial)
    {
        grassMaterial.SetColor(topColorId, grassParameters.topColor);
        grassMaterial.SetColor(bottomColorId, grassParameters.bottomColor);
        grassMaterial.SetColor(tipColorId, grassParameters.tipColor);
        grassMaterial.SetColor(SSSColorId, grassParameters.SSSColor);

        grassMaterial.SetFloat(worldUVTilingId, grassParameters.worldUVTiling);

        grassMaterial.SetFloat(scaleYId, grassParameters.scaleY);
        grassMaterial.SetFloat(randomYScaleNoiseId, grassParameters.randomYScaleNoise);
        grassMaterial.SetFloat(minRandomYId, grassParameters.minRandomY);
        grassMaterial.SetFloat(maxRandomYId, grassParameters.maxRandomY);

        grassMaterial.SetFloat(maxYRotationId, grassParameters.maxYRotation);
        grassMaterial.SetFloat(randomYRotationNoiseId, grassParameters.randomYRotationNoise);
        grassMaterial.SetFloat(maxBendId, grassParameters.maxBend + grassParameters.maxAdditionalBend * _cameraAngleToGroundNormalized);
        grassMaterial.SetInt(randomBendId, grassParameters.randomBend ? 1 : 0);
        grassMaterial.SetFloat(bendRandomnessScaleId, grassParameters.bendRandomnessScale);

        grassMaterial.SetFloat(baseWindDisplacementId, grassParameters.baseWindDisplacement);
        grassMaterial.SetFloat(baseWindYDisplacementId, grassParameters.baseWindYDisplacement);
        grassMaterial.SetFloat(staticWindYMultiplierId, grassParameters.staticWindYMultiplier);
        grassMaterial.SetFloat(staticWindXZMultiplierId, grassParameters.staticWindXZMultiplier);
        grassMaterial.SetFloat(staticWindBladeHashIntensityId, grassParameters.staticWindBladeHashIntensity);

        grassMaterial.SetFloat(dynamicWindStrengthId, grassParameters.dynamicWindStrength);
        grassMaterial.SetFloat(dynamicWindNoiseStrengthId, grassParameters.dynamicWindNoiseStrength);

        //grassMaterial.SetVector(playerPositionId, _playerPosition); Set in SetShaderPlayerPosition.cs (in camera that follows the player)

        grassMaterial.SetFloat(playerPositionModifierXId, grassParameters.playerPositionModifierX);
        grassMaterial.SetFloat(playerPositionModifierYId, grassParameters.playerPositionModifierY);
        grassMaterial.SetFloat(playerPositionModifierZId, grassParameters.playerPositionModifierZ);


        grassMaterial.SetFloat(smooothnessId, grassParameters.smoothness);
        grassMaterial.SetFloat(ambientOclussionId, grassParameters.ao);

    }


    private void FreeQuadtreeNodes()
    {
        if (_grassQuadtrees != null)
        {
            for (int i = 0; i < _grassQuadtrees.Length; i++)
            {
                FreeAllChildren(ref _grassQuadtrees[i]);
            }
        }
    }

    private void FreeAllChildren(ref GrassQuadtree qt)
    {
        FreeQuadtreeNode(ref qt);

        if (qt.subdivided)
        {
            FreeAllChildren(ref qt.northEast);
            FreeAllChildren(ref qt.northWest);
            FreeAllChildren(ref qt.southEast);
            FreeAllChildren(ref qt.southWest);
        }
    }

    private void FreeQuadtreeNode(ref GrassQuadtree qt)
    {
        if(qt.grassDataBuffer != null)
            qt.grassDataBuffer.Release();
        qt.grassDataBuffer = null;

        if (qt.argsBuffer != null)
            qt.argsBuffer.Release();
        qt.argsBuffer = null;

        if (qt.argsLODBuffer != null)
            qt.argsLODBuffer.Release();
        qt.argsLODBuffer = null;

        if (qt.culledGrassDataBuffer != null)
            qt.culledGrassDataBuffer.Release();
        qt.culledGrassDataBuffer = null;

        if (qt.culledGrassDataBufferLOD != null)
            qt.culledGrassDataBufferLOD.Release();
        qt.culledGrassDataBufferLOD = null;
    }

    /// <summary>
    /// Set everything needed for the compute shader in charge of culling. The argument buffers are reset everyframe in order to 
    /// count how many instances are going to be rendered.
    /// </summary>
    /// <param name="qt"> Quadtree </param>
    /// <param name="VP"> View-projection matrix </param>
    void CullGrass(GrassQuadtree qt, Matrix4x4 VP)
    {
        // Reset Args Buffer by setting default arrays
        qt.argsBuffer.SetData(_mainLODArgs);
        qt.argsLODBuffer.SetData(_lowerLODArgs);

        cullGrassCompute.SetMatrix("MATRIX_VP", VP);
        cullGrassCompute.SetFloat("_CullDistance", cutoffDistance);
        cullGrassCompute.SetVector("_CameraPosition", _currentCamera.transform.position);
        cullGrassCompute.SetFloat("_GrassDataNumberofElements", qt.numberOfGrassBlades);
        cullGrassCompute.SetFloat("_LOD_DISTANCE", LODDistance);

        cullGrassCompute.SetBuffer(0, "_GrassDataBuffer", qt.grassDataBuffer);
        cullGrassCompute.SetBuffer(0, "_CulledGrassOutputBuffer", qt.culledGrassDataBuffer);
        cullGrassCompute.SetBuffer(0, "_CulledGrassOutputBufferLOD", qt.culledGrassDataBufferLOD);
        cullGrassCompute.SetBuffer(0, "_ArgsBuffer", qt.argsBuffer);        // sent to count the number of instances
        cullGrassCompute.SetBuffer(0, "_ArgsBufferLOD", qt.argsLODBuffer);  // sent to count the number of instances

        uint culledNumThreadsX;
        cullGrassCompute.GetKernelThreadGroupSizes(0, out culledNumThreadsX, out _, out _);

        qt.culledGrassDataBuffer.SetCounterValue(0);    // THIS IS SUPER FUCKING IMPORTANT
        qt.culledGrassDataBufferLOD.SetCounterValue(0);
       
        cullGrassCompute.Dispatch(0, Mathf.CeilToInt((float)qt.numberOfGrassBlades / (float) culledNumThreadsX), 1, 1);
    }

    void Update()
    {
        if(_currentCamera == null)
        {
            _currentCamera = Camera.main;
            return;
        }

        UpdateGrassAttributes();

        Matrix4x4 P = _currentCamera.projectionMatrix;
        Matrix4x4 V = _currentCamera.transform.worldToLocalMatrix;
        Matrix4x4 VP = P * V;

        // Update material parameters

        _cameraAngleToGroundNormalized = Vector3.Angle(_currentCamera.transform.forward, Vector3.up);
        if(_cameraAngleToGroundNormalized > 100)
        {
            _cameraAngleToGroundNormalized = (_cameraAngleToGroundNormalized - 100) / (180 - 100);    // Remap value to [0, 1]
        }
        else
        {
            _cameraAngleToGroundNormalized = 0;
        }

        _visibleGrassQuadtrees.Clear();
        
        // Get visible nodes
        for (int i = 0; i < _grassQuadtrees.Length; i++)
        {
            _grassQuadtrees[i].TestFrustum(_currentCamera.transform.position, leafCutoffDistance, quadtreeCutoffDistance, GeometryUtility.CalculateFrustumPlanes(_currentCamera), ref _visibleGrassQuadtrees);  // Hay error aquí, coge de los que no debería
        }

        // Free up the memory of non-visible nodes
        if (_pastVisibleGrassQuadtrees.Count > 0)
        {
            List<GrassQuadtree> removedElements = _pastVisibleGrassQuadtrees.Except(_visibleGrassQuadtrees).ToList(); // Except is O(n + m)
            for (int i = 1; i < removedElements.Count; i++)
            {
                GrassQuadtree currentQT = removedElements[i];

                if (currentQT.grassDataBuffer != null)
                {
                    //Debug.Log("Freeing up quadtree at position: " + currentQT.boundary.p.x + ", " + currentQT.boundary.p.y);
                    FreeQuadtreeNode(ref currentQT);
                }
            }
        }

        _pastVisibleGrassQuadtrees = new List<GrassQuadtree>(_visibleGrassQuadtrees);
        
        // RENDER GRASS IN NODES
        for (int i = 0; i < _visibleGrassQuadtrees.Count; i++)
        {
            
            GrassQuadtree currentQT = _visibleGrassQuadtrees[i];

            if (!currentQT.subdivided)
            {
                if (currentQT.grassDataBuffer == null)
                {
                    SetQuadtreeNode(ref currentQT);
                } 

                CullGrass(currentQT, VP);

                SetMaterialProperties(ref currentQT.material);
                SetMaterialProperties(ref currentQT.materialLOD);

                var bounds = new Bounds(new Vector3(currentQT.boundary.p.x, 0, currentQT.boundary.p.y), new Vector3(currentQT.boundary.halfDimension * 2, 600, currentQT.boundary.halfDimension * 2));

                Graphics.DrawMeshInstancedIndirect(grassMesh, 0, currentQT.material, bounds, currentQT.argsBuffer, 0, new MaterialPropertyBlock());
                Graphics.DrawMeshInstancedIndirect(grassMeshLOD, 0, currentQT.materialLOD, bounds, currentQT.argsLODBuffer);

            }
        }
        
    }


    //private void OnDrawGizmos()
    //{
    //    if (_visibleGrassQuadtrees != null)
    //    {
    //        Gizmos.color = Color.red;
    //        DrawQuadtreesWithColors();
    //    }
    //}

    //void DrawQuadtreesWithColors()
    //{
    //    for (int i = 0; i < _visibleGrassQuadtrees.Count; i++)
    //    {
    //        if (!_visibleGrassQuadtrees[i].subdivided)
    //        {
    //            Gizmos.color = Color.blue;
    //        }
    //        Gizmos.DrawWireCube(new Vector3(_visibleGrassQuadtrees[i].boundary.p.x, 0, _visibleGrassQuadtrees[i].boundary.p.y),
    //           new Vector3(_visibleGrassQuadtrees[i].boundary.halfDimension * 2, 0, _visibleGrassQuadtrees[i].boundary.halfDimension * 2));
    //    }
    //}
}
