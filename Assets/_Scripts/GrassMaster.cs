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
    [Range(0, 3000)]
    [SerializeField] float cutoffDistance = 1000f;

    Texture2D[] positionMaps;   // Should be an array with all of the textures? maybe the quadtree stores it
    Texture2D[] heightMaps;   // Should be an array with all of the textures? maybe the quadtree stores it
    [SerializeField] float heightDisplacementStrenght = 600f;

    [Space]

    // QUADTREE STUFF
    GrassQuadtree grassQuadtree;
    GrassQuadtree[] grassQuadtrees;

    List<GrassQuadtree> visibleGrassQuadtrees;

    [Space]

    // The compute shader, assigned in editor
    [SerializeField] ComputeShader grassCompute;
    [SerializeField] ComputeShader cullGrassCompute;

    uint numThreadsX;
    uint numThreadsY;

    [Space]

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
    private uint[] mainLODArgs;
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
    private int nodeResolution;


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

        // GENERATE QUADTREE
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

     

        mainLODArgs = new uint[5] { 0, 0, 0, 0, 0 };
        mainLODArgs[0] = (uint)grassMesh.GetIndexCount(0);
        mainLODArgs[1] = (uint)0;
        mainLODArgs[2] = (uint)grassMesh.GetIndexStart(0);
        mainLODArgs[3] = (uint)grassMesh.GetBaseVertex(0);
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

        InitializeQuadtreeNodes();

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
            nodeResolution = (int)(qt.boundary.halfDimension * 2 * grassDensity);

            qt.grassCompute = Resources.Load<ComputeShader>("GrassCompute");

            qt.grassDataBuffer = new ComputeBuffer(nodeResolution * nodeResolution, grassDataBufferSize, ComputeBufferType.Append);
            qt.grassDataBuffer.SetCounterValue(0);

            qt.argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
            //qt.argsLODBuffer= new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
            qt.culledGrassDataBuffer = new ComputeBuffer(nodeResolution * nodeResolution, grassDataBufferSize, ComputeBufferType.Append);

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
            // qt.grassCompute.SetBuffer(0, "_ArgsBuffer", qt.argsBuffer);

            qt.material.SetBuffer("_GrassData", qt.grassDataBuffer);

            // THIS IS NOT IDEAL AS WE ARE STORING ALL THE POSITIONS BEFORE HAND... MAYBE IDK WE HAVE TO TRY IT

            //Debug.Log("Current qt position x: " + qt.boundary.p.x + " y: " + qt.boundary.p.y);
            //Debug.Log("Current qt resolution: " + nodeResolution);

            qt.grassDataBuffer.SetCounterValue(0);

            qt.grassCompute.Dispatch(0, (int)(qt.boundary.halfDimension * 2 * grassDensity / numThreadsX), (int)(qt.boundary.halfDimension * 2 * grassDensity / numThreadsY), 1);

            uint[] newArgs = new uint[5] { 0, 1, 0, 0, 0 };
            qt.argsBuffer.SetData(mainLODArgs);
            ComputeBuffer.CopyCount(qt.grassDataBuffer, qt.argsBuffer, sizeof(uint));
            qt.argsBuffer.GetData(newArgs);

            qt.numberOfGrassBlades = (int)newArgs[1];

            //Debug.Log("Number of grass blades in node: " + qt.numberOfGrassBlades);
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

        if (qt.culledGrassDataBuffer != null)
            qt.culledGrassDataBuffer.Release();
        qt.culledGrassDataBuffer = null;
    }

    void CullGrass(ref GrassQuadtree qt, Matrix4x4 VP, bool noLOD)
    {
        // Reset Args Buffer by setting default (0)
        qt.argsBuffer.SetData(mainLODArgs);

        cullGrassCompute.SetMatrix("MATRIX_VP", VP);
        cullGrassCompute.SetFloat("_CullDistance", cutoffDistance);
        cullGrassCompute.SetVector("_CameraPosition", Camera.main.transform.position);
        cullGrassCompute.SetFloat("_GrassDataNumberofElements", qt.numberOfGrassBlades);

        cullGrassCompute.SetBuffer(0, "_GrassDataBuffer", qt.grassDataBuffer);
        cullGrassCompute.SetBuffer(0, "_CulledGrassOutputBuffer", qt.culledGrassDataBuffer);
        cullGrassCompute.SetBuffer(0, "_ArgsBuffer", qt.argsBuffer);    // sent to count the number of instances

        uint culledNumThreadsX;
        cullGrassCompute.GetKernelThreadGroupSizes(0, out culledNumThreadsX, out numThreadsY, out _);

        cullGrassCompute.Dispatch(0, Mathf.CeilToInt(nodeResolution * nodeResolution / culledNumThreadsX), 1, 1);

        /*uint[] newArgs = new uint[5] { 0, 1, 0, 0, 0 };
       // qt.argsBuffer.GetData(newArgs);

        qt.argsLODBuffer.SetData(newArgs);
        ComputeBuffer.CopyCount(qt.culledGrassDataBuffer, qt.argsBuffer, sizeof(uint));
        qt.argsLODBuffer.GetData(newArgs);

        qt.numberOfInstances = newArgs[1];

        Debug.Log(qt.numberOfInstances);*/
    }

    void LateUpdate()
    {
        UpdateGrassAttributes();

        Matrix4x4 P = Camera.main.projectionMatrix;
        Matrix4x4 V = Camera.main.transform.worldToLocalMatrix;
        Matrix4x4 VP = P * V;

        visibleGrassQuadtrees.Clear();
        
        // Get visible nodes
        for (int i = 0; i < grassQuadtrees.Length; i++)
        {
            grassQuadtrees[i].TestFrustum(Camera.main.transform.position, GeometryUtility.CalculateFrustumPlanes(Camera.main), ref visibleGrassQuadtrees);  // Hay error aqu�, coge de los que no deber�a
        }
       
        // RENDER GRASS IN NODES
        for (int i = 1; i < visibleGrassQuadtrees.Count; i++)
        {
            GrassQuadtree currentQT = visibleGrassQuadtrees[i];

            if (currentQT.grassCompute != null)
            {
                CullGrass(ref currentQT, VP, true);

                var bounds = new Bounds(new Vector3(currentQT.boundary.p.x, 0, currentQT.boundary.p.y), new Vector3(currentQT.boundary.halfDimension * 2, 600, currentQT.boundary.halfDimension * 2));

                Graphics.DrawMeshInstancedIndirect(grassMesh, 0, currentQT.material, bounds, currentQT.argsBuffer);
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
}
