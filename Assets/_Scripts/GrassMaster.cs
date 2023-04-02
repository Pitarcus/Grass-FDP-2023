using System.Collections;
using System.Collections.Generic;
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

    [SerializeField] Texture2D[] positionMaps;   // Should be an array with all of the textures? maybe the quadtree stores it
    [SerializeField] Texture2D[] heightMaps;   // Should be an array with all of the textures? maybe the quadtree stores it
    [SerializeField] float heightDisplacementStrenght = 600f;

    [Space]

    // QUADTREE STUFF
    GrassQuadtree grassQuadtree;
    GrassQuadtree[] grassQuadtrees;

    List<GrassQuadtree> visibleGrassQuadtrees;

    [Space]

    // The compute shader, assigned in editor
    [SerializeField] ComputeShader grassCompute;

    uint numThreadsX;
    uint numThreadsY;

    // Grass mesh and material
    [SerializeField] Mesh grassMesh;
    [SerializeField] Material grassMaterial;

    // Data structure to communicate GPU and CPU
    private struct GrassData
    {
        public Vector3 position;
        public Vector3 scale;
    }

    // Compute Buffer, to store the grass data inside the GPU
    ComputeBuffer grassDataBuffer;


    ComputeBuffer argumentBuffer;
    private uint[] mainLODArgs = new uint[5] { 0, 1, 0, 0, 0 };
    private uint[] LOD1Args = new uint[5] { 0, 1, 0, 0, 0 };


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
        scalesId = Shader.PropertyToID("_YScales");


    // CONST
    private int positionsBufferSize = 3 * 4; // 3 floats per position * 4 bytes per float
    private int scalesBufferSize = 4;
    private int grassDataBufferSize = 3 * 4 + 3 * 4;


    List<GrassQuadtree> debugList;

    public Texture2D debugPlacementTexture;

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

        for (int i = 0; i < grassPainters.Length; i++)
        {
            positionMaps[i] = grassPainters[i].GetComponent<TerrainPainterComponent>().maskTexture;
            heightMaps[i] = grassPainters[i].GetComponent<TerrainPainterComponent>().heightMap;

            grassQuadtrees[i] = new GrassQuadtree(new AABB(grassPainters[i].transform.position.x, grassPainters[i].transform.position.z, 64),   // last number is half the size of the terrain
                0,
                4,
                positionMaps[i],
                heightMaps[i],
                grassMaterial);

            grassQuadtrees[i].Build();
        }

        visibleGrassQuadtrees = new List<GrassQuadtree>();

        debugList = new List<GrassQuadtree>();
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

        // Create the compute buffers
        //grassDataBuffer = new ComputeBuffer(grassResolution * grassResolution, grassDataBufferSize, ComputeBufferType.Append);
        //grassDataBuffer.SetCounterValue(0);

        grassCompute.GetKernelThreadGroupSizes(0, out numThreadsX, out numThreadsY, out _);

        InitializeQuadtreeNodes();

        //SetShaderParameters();

    }


    // Making sure the garbage collector does its job and hot reload stuff
    void OnDisable()
    {
        if(grassDataBuffer != null)
        grassDataBuffer.Release();
        grassDataBuffer = null;

        if (argumentBuffer != null)
            argumentBuffer.Release();
        argumentBuffer = null;

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
            int nodeResolution = (int)(qt.boundary.halfDimension * 2 * grassDensity);

            qt.grassCompute = Resources.Load<ComputeShader>("GrassCompute");

            qt.grassDataBuffer = new ComputeBuffer(nodeResolution * nodeResolution, grassDataBufferSize, ComputeBufferType.Append);
            qt.grassDataBuffer.SetCounterValue(0);

            qt.argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);

            qt.grassCompute.SetInt(sizeId, (int) qt.boundary.halfDimension * 2);
            qt.grassCompute.SetInt(resolutionId, nodeResolution);
            qt.grassCompute.SetFloat(stepId, grassStep);
            qt.grassCompute.SetFloat("_NodePositionX", qt.boundary.p.x);
            qt.grassCompute.SetFloat("_NodePositionY", qt.boundary.p.y);
            qt.grassCompute.SetFloat(offsetXAmountId, offsetXAmount);
            qt.grassCompute.SetFloat(offsetYAmountId, offsetYAmount);
            qt.grassCompute.SetFloat(heightDisplacementStrenghtId, heightDisplacementStrenght);
            qt.grassCompute.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);

            qt.grassCompute.SetTexture(0, "_HeightMap", qt.heightMap);
            qt.grassCompute.SetTexture(0, positionMapID, qt.grassMask);
            qt.grassCompute.SetBuffer(0, "_GrassData", qt.grassDataBuffer);

            qt.material.SetBuffer("_GrassData", qt.grassDataBuffer);


            // Drawign the meshes

            // NOT IDEAL AS WE ARE STORING ALL THE POSITIONS BEFORE HAND... MAYBE IDK WE HAVE TO TRY IT

            Debug.Log("Current qt position x: " + qt.boundary.p.x + " y: " + qt.boundary.p.y);
            Debug.Log("Current qt resolution: " + nodeResolution);

            qt.grassDataBuffer.SetCounterValue(0);

            qt.grassCompute.Dispatch(0, (int)(qt.boundary.halfDimension * 2 * grassDensity / numThreadsX), (int)(qt.boundary.halfDimension * 2 * grassDensity / numThreadsY), 1);

            mainLODArgs = new uint[5] { 0, 1, 0, 0, 0 };
            qt.argsBuffer.SetData(mainLODArgs);
            ComputeBuffer.CopyCount(qt.grassDataBuffer, qt.argsBuffer, sizeof(uint));
            qt.argsBuffer.GetData(mainLODArgs);

            qt.numberOfInstances = (int) mainLODArgs[1];

            Debug.Log("Number of instances in node: " + qt.numberOfInstances);
        }
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
    }

    private void SetShaderParameters()
    {
        // Setting the parameters
        grassCompute.SetInt(sizeId, grassSquareSize);
        grassCompute.SetInt(resolutionId, grassResolution);
        grassCompute.SetFloat(stepId, grassStep);
        grassCompute.SetFloat(offsetXAmountId, offsetXAmount);
        grassCompute.SetFloat(offsetYAmountId, offsetYAmount);
        grassCompute.SetFloat(heightDisplacementStrenghtId, heightDisplacementStrenght);
        grassCompute.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);

        //grassCompute.SetTexture(0, "_HeightMap", heightMap);
        //grassCompute.SetTexture(0, positionMapID, positionMap);
        grassCompute.SetBuffer(0, "_GrassData", grassDataBuffer);
        //grassCompute.SetBuffer(0, "_SourceVertices", m_SourceVertBuffer);


        // Dispatching the actual shader

        uint numThreadsX;
        uint numThreadsY;
        grassCompute.GetKernelThreadGroupSizes(0, out numThreadsX, out numThreadsY, out _);

        int threadGroupsX = Mathf.CeilToInt(grassResolution / numThreadsX);
        int threadGroupsY = Mathf.CeilToInt(grassResolution / numThreadsY);

        grassDataBuffer.SetCounterValue(0);

        // DISPATCH COMPUTE SHADER !!!
        //grassCompute.Dispatch(0, threadGroupsX, threadGroupsY, 1);  

        // Get arguments buffer
        argumentBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        mainLODArgs = new uint[5] { 0, 1, 0, 0, 0 };
        argumentBuffer.SetData(mainLODArgs);
        
        ComputeBuffer.CopyCount(grassDataBuffer, argumentBuffer, sizeof(uint));
        argumentBuffer.GetData(mainLODArgs);

        Debug.Log(mainLODArgs[1]);

        // Set Material attributes
        grassMaterial.SetBuffer("_GrassData", grassDataBuffer);


        //positionsBuffer.GetData(grassPositions);
        
    }

    void LateUpdate()
    {
        UpdateGrassAttributes();

        visibleGrassQuadtrees.Clear();
        
        for (int i = 0; i < grassQuadtrees.Length; i++)
        {
            grassQuadtrees[i].TestFrustum(Camera.main.transform.position, GeometryUtility.CalculateFrustumPlanes(Camera.main), ref visibleGrassQuadtrees);  // Hay error aquí, coge de los que no debería
            
        }

        debugPlacementTexture = grassQuadtrees[0].northWest.southWest.northWest.grassMask;
       
        for (int i = 1; i < visibleGrassQuadtrees.Count; i++)
        {
            GrassQuadtree currentQT = visibleGrassQuadtrees[i];

            if (currentQT.grassCompute != null)
            {
                var bounds = new Bounds(new Vector3(currentQT.boundary.p.x, 0, currentQT.boundary.p.y), new Vector3(currentQT.boundary.halfDimension * 2, 600, currentQT.boundary.halfDimension * 2));

                Graphics.DrawMeshInstancedProcedural(grassMesh, 0, currentQT.material, bounds, currentQT.numberOfInstances);
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

            
        /*if (grassQuadtrees != null)
        {
            for (int i = 0; i < grassQuadtrees.Length; i++)
            {
                if (i == 0)
                    Gizmos.color = Color.blue;
                else
                    Gizmos.color = Color.magenta;

                GrassQuadtree quadTree = grassQuadtrees[i];
                Queue<GrassQuadtree> queue = new Queue<GrassQuadtree>();

                if (quadTree == null)
                    return;

                queue.Clear();
                queue.Enqueue(quadTree);

                while (queue.Count > 0)
                {
                    GrassQuadtree currentQT = queue.Dequeue();

                    Gizmos.DrawWireCube(new Vector3(currentQT.boundary.p.x, 0, currentQT.boundary.p.y),
                        new Vector3(currentQT.boundary.halfDimension * 2, 0, currentQT.boundary.halfDimension * 2));

                    if (currentQT.subdivided)
                    {
                        queue.Enqueue(currentQT.northEast);
                        queue.Enqueue(currentQT.northWest);
                        queue.Enqueue(currentQT.southEast);
                        queue.Enqueue(currentQT.southWest);
                    }
                }
            }
        }
        */
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

    private void IterateThroughQuadtreeChildren2(ref GrassQuadtree qt)
    {
        debugList.Add(qt);

        if (qt.subdivided)
        {
            IterateThroughQuadtreeChildren2(ref qt.northEast);
            IterateThroughQuadtreeChildren2(ref qt.northWest);
            IterateThroughQuadtreeChildren2(ref qt.southEast);
            IterateThroughQuadtreeChildren2(ref qt.southWest);
        }
    }
}
