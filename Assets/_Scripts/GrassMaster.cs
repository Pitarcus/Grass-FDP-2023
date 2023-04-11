using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GrassMaster : MonoBehaviour
{
    // Parameters
    [Range(1, 1000)]
    [SerializeField] int grassSquareSize = 300;
    [Range(1, 6)]
    [SerializeField] int grassDensity = 1;
    [Range(0, 2)]
    [SerializeField] float offsetXAmount = 0.5f;
    [Range(0, 2)]
    [SerializeField] float offsetYAmount = 0.5f;
    [Range(0, 1000)]
    [SerializeField] float cutoffDistance = 1000f;
    [Range(0, 1000)]
    [SerializeField] float quadtreeCutoffDistance = 120f;
    [Range(0, 500)]
    [SerializeField] float LODDistance = 50f;

    Texture2D[] positionMaps;   // Should be an array with all of the textures? maybe the quadtree stores it
    Texture2D[] heightMaps;   // Should be an array with all of the textures? maybe the quadtree stores it
    [SerializeField] float heightDisplacementStrenght = 600f;

    [Space]

    // QUADTREE STUFF
    GrassQuadtree[] grassQuadtrees;

    List<GrassQuadtree> visibleGrassQuadtrees;
    List<GrassQuadtree> pastVisibleGrassQuadtrees;

    [Space]

    // The compute shader, assigned in editor
    [SerializeField] ComputeShader grassCompute;
    [SerializeField] ComputeShader cullGrassCompute;

    uint numThreadsX;
    uint numThreadsY;

    [Space]

    // Grass mesh and material
    [SerializeField] Mesh grassMesh;
    [SerializeField] Mesh grassMeshLOD;
    [SerializeField] Material grassMaterial;

    [Space]

    // Grass Material parameters
    [Header("Grass Material parameters")]
    [SerializeField] Color bottomColor;
    [SerializeField] Color topColor;
    [SerializeField] float worldUVTiling;
    [Header("Scale")]
    [SerializeField] float scaleY;
    [SerializeField] float randomYScaleNoise;
    [SerializeField] float minRandomY;
    [SerializeField] float maxRandomY;
    [Header("Rotation")]
    [SerializeField][Range(0, 360)] float maxYRotation;
    [SerializeField] float randomYRotationNoise;
    [SerializeField][Range(0, 90)] float maxBend;
    [SerializeField] float bendRandomnessScale;
    [Header("Wind")]
    [SerializeField] float windStrenght;
    [SerializeField] float windSpeed;
    [SerializeField][Range(0, 360)] float windRotation;
    [SerializeField] float windScaleNoise;
    [SerializeField] float windDistortion;
    [SerializeField] Transform playerTransform;
    Vector3 playerPosition;

    // Data structure to communicate GPU and CPU
    private struct GrassData
    {
        public Vector3 position;
        public Vector3 scale;
    }

    private uint[] mainLODArgs;
    private uint[] lowerLODArgs;


    // Internal values for grass positions
    private int grassResolution;
    private float grassStep;


    // Grass material properties to id
    static readonly int
        positionsId = Shader.PropertyToID("_Positions"),
        sizeId = Shader.PropertyToID("_Size"),
        resolutionId = Shader.PropertyToID("_Resolution"),
        stepId = Shader.PropertyToID("_Step"),
        offsetXAmountId = Shader.PropertyToID("_OffsetXAmount"),
        offsetYAmountId = Shader.PropertyToID("_OffsetYAmount"),
        heightDisplacementStrenghtId = Shader.PropertyToID("_HeightDisplacementStrenght"),
        positionMapID = Shader.PropertyToID("_PositionMap"),
        scalesId = Shader.PropertyToID("_YScales"),

        bottomColorId = Shader.PropertyToID("_BottomColor"),
        topColorId = Shader.PropertyToID("_TopColor"),
        worldUVTilingId = Shader.PropertyToID("_WorldUV_Tiling"),
        scaleYId = Shader.PropertyToID("_ScaleY"),
        randomYScaleNoiseId = Shader.PropertyToID("_RandomYNoiseScale"),
        minRandomYId = Shader.PropertyToID("_MinRandomY"),
        maxRandomYId = Shader.PropertyToID("_MaxRandomY"),
        maxYRotationId = Shader.PropertyToID("_MaxYRotation"),
        randomYRotationNoiseId = Shader.PropertyToID("_RandomYRotationNoiseScale"),
        maxBendId = Shader.PropertyToID("_MaxBend"),
        bendRandomnessScaleId = Shader.PropertyToID("_BendRandomnessScale"),
        windStrenghtId = Shader.PropertyToID("_WindStrenght"),
        windSpeedId = Shader.PropertyToID("_WindSpeed"),
        windRotationId = Shader.PropertyToID("_WindRotation"),
        windScaleNoiseId = Shader.PropertyToID("_WindNoiseScale"),
        windDistortionId = Shader.PropertyToID("_WindDistortion"),
        playerPositionId = Shader.PropertyToID("_PlayerPosition")
        ;


    // CONST
    private int positionsBufferSize = 3 * 4; // 3 floats per position * 4 bytes per float
    private int scalesBufferSize = 4;
    private int grassDataBufferSize = 3 * 4 + 3 * 4;
    private int nodeResolution = 0;


    List<GrassQuadtree> debugList;

    // ------------- FUNCTIONS --------------

    private void Awake()
    {
        grassResolution = grassSquareSize * grassDensity;
        grassStep = grassSquareSize / (float) grassResolution;

        // A quadtree for each "tile"
        GameObject[] grassPainters = GameObject.FindGameObjectsWithTag("GrassPainter");

        positionMaps = new Texture2D[grassPainters.Length];
        heightMaps = new Texture2D[grassPainters.Length];
        grassQuadtrees = new GrassQuadtree[grassPainters.Length];

        // GENERATE QUADTREE
        for (int i = 0; i < grassPainters.Length; i++)
        {
            positionMaps[i] = grassPainters[i].GetComponent<TerrainPainterComponent>().maskTexture;
            heightMaps[i] = grassPainters[i].GetComponent<TerrainPainterComponent>().heightMap;

            grassQuadtrees[i] = new GrassQuadtree(new AABB(grassPainters[i].transform.position.x, grassPainters[i].transform.position.z, 64),   // last number is half the size of the terrain
                0,
                4,
                positionMaps[i],
                heightMaps[i]);

            grassQuadtrees[i].Build();
        }

        visibleGrassQuadtrees = new List<GrassQuadtree>();
        pastVisibleGrassQuadtrees = new List<GrassQuadtree>();

        mainLODArgs = new uint[5] { 0, 0, 0, 0, 0 };
        mainLODArgs[0] = (uint)grassMesh.GetIndexCount(0);
        mainLODArgs[1] = (uint)0;
        mainLODArgs[2] = (uint)grassMesh.GetIndexStart(0);
        mainLODArgs[3] = (uint)grassMesh.GetBaseVertex(0);

        lowerLODArgs = new uint[5] { 0, 0, 0, 0, 0 };
        lowerLODArgs[0] = (uint)grassMeshLOD.GetIndexCount(0);
        lowerLODArgs[1] = (uint)0;
        lowerLODArgs[2] = (uint)grassMeshLOD.GetIndexStart(0);
        lowerLODArgs[3] = (uint)grassMeshLOD.GetBaseVertex(0);
    }

    void UpdateGrassAttributes()
    {
        grassResolution = grassSquareSize * grassDensity;
        grassStep = grassSquareSize / (float)grassResolution;
    }


    #region GoodPracticesCleanness
    // Stuff for creating the buffer
    void OnEnable()
    {
        grassCompute.GetKernelThreadGroupSizes(0, out numThreadsX, out numThreadsY, out _);

        //InitializeQuadtreeNodes();
    }


    // Making sure the garbage collector does its job and hot reload stuff
    void OnDisable()
    {
        FreeQuadtreeNodes();
    }
    #endregion

    private void InitializeQuadtreeNodes()
    {
        if (grassQuadtrees != null)
        {
            for (int i = 0; i < grassQuadtrees.Length; i++)
            {
                SetAllChildren(ref grassQuadtrees[i]);
            }
        }
    }

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
                nodeResolution = (int)(qt.boundary.halfDimension * 2 * grassDensity);

            if(!qt.hasBeenSet)
                qt.grassCompute = Resources.Load<ComputeShader>("GrassCompute");

            qt.grassDataBuffer = new ComputeBuffer(nodeResolution * nodeResolution, grassDataBufferSize, ComputeBufferType.Append);
            qt.grassDataBuffer.SetCounterValue(0);

            qt.argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
            qt.argsLODBuffer= new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);

            qt.grassCompute.SetInt(sizeId, (int)qt.boundary.halfDimension * 2);
            qt.grassCompute.SetInt(resolutionId, nodeResolution);
            qt.grassCompute.SetFloat(stepId, grassStep);
            qt.grassCompute.SetFloat("_NodePositionX", qt.boundary.p.x);
            qt.grassCompute.SetFloat("_NodePositionY", qt.boundary.p.y);
            qt.grassCompute.SetFloat(offsetXAmountId, offsetXAmount);
            qt.grassCompute.SetFloat(offsetYAmountId, offsetYAmount);
            qt.grassCompute.SetFloat(heightDisplacementStrenghtId, heightDisplacementStrenght);
            //qt.grassCompute.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);

            qt.grassCompute.SetTexture(0, "_HeightMap", qt.heightMap);
            qt.grassCompute.SetTexture(0, positionMapID, qt.grassMask);
            qt.grassCompute.SetBuffer(0, "_GrassData", qt.grassDataBuffer);

            qt.grassDataBuffer.SetCounterValue(0);
            qt.grassCompute.Dispatch(0, (int)(qt.boundary.halfDimension * 2 * grassDensity / numThreadsX), (int)(qt.boundary.halfDimension * 2 * grassDensity / numThreadsY), 1);

            uint[] newArgs = new uint[5] { 0, 1, 0, 0, 0 };
            qt.argsLODBuffer.SetData(lowerLODArgs);
            ComputeBuffer.CopyCount(qt.grassDataBuffer, qt.argsLODBuffer, sizeof(uint));
            qt.argsLODBuffer.GetData(newArgs);

            qt.numberOfGrassBlades = newArgs[1];

            qt.culledGrassDataBuffer = new ComputeBuffer((int)qt.numberOfGrassBlades, grassDataBufferSize, ComputeBufferType.Append);
            qt.culledGrassDataBufferLOD = new ComputeBuffer((int)qt.numberOfGrassBlades, grassDataBufferSize, ComputeBufferType.Append);

           
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
        grassMaterial.SetColor(topColorId, topColor);
        grassMaterial.SetColor(bottomColorId, bottomColor);

        grassMaterial.SetFloat(worldUVTilingId, worldUVTiling);
        grassMaterial.SetFloat(scaleYId, scaleY);
        grassMaterial.SetFloat(randomYScaleNoiseId, randomYScaleNoise);
        grassMaterial.SetFloat(minRandomYId, minRandomY);
        grassMaterial.SetFloat(maxRandomYId, maxRandomY);
        grassMaterial.SetFloat(maxYRotationId, maxYRotation);
        grassMaterial.SetFloat(randomYRotationNoiseId, randomYRotationNoise);
        grassMaterial.SetFloat(maxBendId, maxBend);
        grassMaterial.SetFloat(bendRandomnessScaleId, bendRandomnessScale);
        grassMaterial.SetFloat(windStrenghtId, windStrenght);
        grassMaterial.SetFloat(windSpeedId, windSpeed);
        grassMaterial.SetFloat(windRotationId, windRotation);
        grassMaterial.SetFloat(windScaleNoiseId, windScaleNoise);
        grassMaterial.SetFloat(windDistortionId, windDistortion);
        grassMaterial.SetVector(playerPositionId, playerPosition);
    }

    private void FreeQuadtreeNodes()
    {
        if (grassQuadtrees != null)
        {
            for (int i = 0; i < grassQuadtrees.Length; i++)
            {
                FreeAllChildren(ref grassQuadtrees[i]);
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

    void CullGrass(GrassQuadtree qt, Matrix4x4 VP, bool noLOD)
    {
        // Reset Args Buffer by setting default arrays
        qt.argsBuffer.SetData(mainLODArgs);
        qt.argsLODBuffer.SetData(lowerLODArgs);

        cullGrassCompute.SetMatrix("MATRIX_VP", VP);
        cullGrassCompute.SetFloat("_CullDistance", cutoffDistance);
        cullGrassCompute.SetVector("_CameraPosition", Camera.main.transform.position);
        cullGrassCompute.SetFloat("_GrassDataNumberofElements", qt.numberOfGrassBlades);
        cullGrassCompute.SetFloat("_LOD_DISTANCE", LODDistance);

        cullGrassCompute.SetBuffer(0, "_GrassDataBuffer", qt.grassDataBuffer);
        cullGrassCompute.SetBuffer(0, "_CulledGrassOutputBuffer", qt.culledGrassDataBuffer);
        cullGrassCompute.SetBuffer(0, "_CulledGrassOutputBufferLOD", qt.culledGrassDataBufferLOD);
        cullGrassCompute.SetBuffer(0, "_ArgsBuffer", qt.argsBuffer);    // sent to count the number of instances
        cullGrassCompute.SetBuffer(0, "_ArgsBufferLOD", qt.argsLODBuffer);

        uint culledNumThreadsX;
        cullGrassCompute.GetKernelThreadGroupSizes(0, out culledNumThreadsX, out _, out _);

        qt.culledGrassDataBuffer.SetCounterValue(0);    // THIS IS SUPER FUCKING IMPORTANT
        qt.culledGrassDataBufferLOD.SetCounterValue(0);
        cullGrassCompute.Dispatch(0, Mathf.CeilToInt(nodeResolution * nodeResolution / culledNumThreadsX), 1, 1);

        /*uint[] newArgs = new uint[5] { 0, 1, 0, 0, 0 };

        ComputeBuffer.CopyCount(qt.culledGrassDataBufferLOD, qt.argsLODBuffer, sizeof(uint));
        qt.argsLODBuffer.GetData(newArgs);

        //qt.numberOfInstances = newArgs[1];

        Debug.Log("Number of LOD: " + newArgs[1]);*/
    }

    void Update()
    {
        UpdateGrassAttributes();

        Matrix4x4 P = Camera.main.projectionMatrix;
        Matrix4x4 V = Camera.main.transform.worldToLocalMatrix;
        Matrix4x4 VP = P * V;

        playerPosition = playerTransform.position;

        visibleGrassQuadtrees.Clear();
        
        // Get visible nodes
        for (int i = 0; i < grassQuadtrees.Length; i++)
        {
            grassQuadtrees[i].TestFrustum(Camera.main.transform.position, quadtreeCutoffDistance, GeometryUtility.CalculateFrustumPlanes(Camera.main), ref visibleGrassQuadtrees);  // Hay error aqu�, coge de los que no deber�a
        }

        // Free up the memory of non visible nodes
        if (pastVisibleGrassQuadtrees.Count > 0)
        {
            List<GrassQuadtree> removedElements = pastVisibleGrassQuadtrees.Except(visibleGrassQuadtrees).ToList();
            for (int i = 1; i < removedElements.Count; i++)
            {
                GrassQuadtree currentQT = removedElements[i];

                if (currentQT.grassCompute != null)
                {
                    FreeQuadtreeNode(ref currentQT);
                }
            }
        }

        pastVisibleGrassQuadtrees = new List<GrassQuadtree>(visibleGrassQuadtrees);
        
        // RENDER GRASS IN NODES
        for (int i = 1; i < visibleGrassQuadtrees.Count; i++)
        {
            GrassQuadtree currentQT = visibleGrassQuadtrees[i];

            if (!currentQT.subdivided)
            {
                if (currentQT.grassDataBuffer == null)
                {
                    SetQuadtreeNode(ref currentQT);
                } 

                CullGrass(currentQT, VP, true);

                SetMaterialProperties(ref currentQT.material);
                SetMaterialProperties(ref currentQT.materialLOD);

                var bounds = new Bounds(new Vector3(currentQT.boundary.p.x, 0, currentQT.boundary.p.y), new Vector3(currentQT.boundary.halfDimension * 2, 600, currentQT.boundary.halfDimension * 2));

                Graphics.DrawMeshInstancedIndirect(grassMesh, 0, currentQT.material, bounds, currentQT.argsBuffer);
                Graphics.DrawMeshInstancedIndirect(grassMeshLOD, 0, currentQT.materialLOD, bounds, currentQT.argsLODBuffer);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (visibleGrassQuadtrees != null)
        {
            Gizmos.color = Color.red;
            DrawQuadtreesWithColors();
        }
    }

    void DrawQuadtreesWithColors()
    {
        for (int i = 0; i < visibleGrassQuadtrees.Count; i++)
        {
            if (!visibleGrassQuadtrees[i].subdivided)
            {
                Gizmos.color = Color.blue;
            }
            Gizmos.DrawWireCube(new Vector3(visibleGrassQuadtrees[i].boundary.p.x, 0, visibleGrassQuadtrees[i].boundary.p.y),
               new Vector3(visibleGrassQuadtrees[i].boundary.halfDimension * 2, 0, visibleGrassQuadtrees[i].boundary.halfDimension * 2));
        }
    }
}
