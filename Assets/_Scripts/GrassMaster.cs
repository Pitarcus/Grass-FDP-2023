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

    [SerializeField] Texture heightMap;
    [SerializeField] Texture2D positionMap;   // Should be an array with all of the textures? maybe the quadtree stores it
    [SerializeField] Texture2D[] positionMaps;   // Should be an array with all of the textures? maybe the quadtree stores it
    [SerializeField] float heightDisplacementStrenght = 600f;

    [Space]

    // QUADTREE STUFF
    GrassQuadtree grassQuadtree;
    GrassQuadtree[] grassQuadtrees;

    List<GrassQuadtree> visibleGrassQuadtrees;

    [Space]

    // The compute shader, assigned in editor
    [SerializeField] ComputeShader grassCompute;

    // Grass mesh and material
    [SerializeField] Mesh grassMesh;
    [SerializeField] Material grassMaterial;

    // Data structure to communicate GPU and CPU
    private struct GrassData
    {
        public Vector3 position;
        public Vector3 scale;
    }

    // Compute Buffer, to store the positions inside the GPU
    ComputeBuffer grassDataBuffer;


    ComputeBuffer argumentBuffer;
    private uint[] mainLODArgs = new uint[5] { 0, 1, 0, 0, 0 };
    private uint[] LOD1Args = new uint[5] { 0, 1, 0, 0, 0 };


    // Internal values for grass positions
    int grassResolution;
    float grassStep;


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
    private const int SOURCE_VERT_STRIDE = sizeof(float) * (3 + 3 + 2 + 3);
    private int positionsBufferSize = 3 * 4; // 3 floats per position * 4 bytes per float
    private int scalesBufferSize = 4;
    private int grassDataBufferSize = 3 * 4 + 3 * 4;


    // ------------- FUNCTIONS --------------

    private void Awake()
    {
        grassResolution = grassSquareSize * grassDensity;
        grassStep = grassSquareSize / (float) grassResolution;

        positionMap = GameObject.FindGameObjectWithTag("GrassPainter").GetComponent<TerrainPainterComponent>().maskTexture;
        //grassQuadtree = new GrassQuadtree(new AABB(0, 0, 64), 0, 4, positionMap);

        // A quadtree for each "tile"
        GameObject[] grassPainters = GameObject.FindGameObjectsWithTag("GrassPainter");
        positionMaps = new Texture2D[grassPainters.Length];
        grassQuadtrees = new GrassQuadtree[grassPainters.Length];

        for (int i = 0; i < grassPainters.Length; i++)
        {
            positionMaps[i] = grassPainters[i].GetComponent<TerrainPainterComponent>().maskTexture;

            grassQuadtrees[i] = new GrassQuadtree(new AABB(grassPainters[i].transform.position.x, grassPainters[i].transform.position.y, 64),
                0,
                4,
                positionMap,
                grassMaterial,
                grassCompute);

            grassQuadtrees[i].Build();
        }

        visibleGrassQuadtrees = new List<GrassQuadtree>();
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
        grassDataBuffer = new ComputeBuffer(grassResolution * grassResolution, grassDataBufferSize, ComputeBufferType.Append);
        grassDataBuffer.SetCounterValue(0);

        SetShaderParameters();

    }


    // Making sure the garbage collector does its job and hot reload stuff
    void OnDisable()
    {
        if(grassDataBuffer != null)
        grassDataBuffer.Release();
        grassDataBuffer = null;


        argumentBuffer.Release();
        argumentBuffer = null;
    }
    #endregion

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

        grassCompute.SetTexture(0, "_HeightMap", heightMap);
        grassCompute.SetTexture(0, positionMapID, positionMap);
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
        grassCompute.Dispatch(0, threadGroupsX, threadGroupsY, 1);  

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

        /*//SetShaderParameters();

        // Drawign the meshes
        var bounds = new Bounds(Vector3.zero, Vector3.one * grassSquareSize);

        //Graphics.DrawMeshInstancedIndirect(grassMesh, 0, grassMaterial, bounds, argumentBuffer);
        Graphics.DrawMeshInstancedProcedural(grassMesh, 0, grassMaterial, bounds, (int)mainLODArgs[1]);*/

        visibleGrassQuadtrees.Clear();

        
        for (int i = 0; i < grassQuadtrees.Length; i++)
        {
            grassQuadtrees[i].TestFrustum(GeometryUtility.CalculateFrustumPlanes(Camera.main), ref visibleGrassQuadtrees);
        }
        Debug.Log(visibleGrassQuadtrees.Count);
        for (int i = 0; i < visibleGrassQuadtrees.Count; i++)
        {
            /*GrassQuadtree currentQT = visibleGrassQuadtrees[i];
            // Drawign the meshes
            var bounds = new Bounds(new Vector3(currentQT.boundary.p.x, currentQT.boundary.p.y), new Vector3(currentQT.boundary.halfDimension, currentQT.boundary.halfDimension, 600));

            //Graphics.DrawMeshInstancedIndirect(grassMesh, 0, grassMaterial, bounds, argumentBuffer);
            Graphics.DrawMeshInstancedProcedural(grassMesh, 0, grassMaterial, bounds, (int)mainLODArgs[1]);*/
        }
    }

    private void OnDrawGizmos()
    {
        if (visibleGrassQuadtrees != null)
        {
            for (int i = 0; i < visibleGrassQuadtrees.Count; i++)
            {
                Gizmos.DrawWireCube(new Vector3(visibleGrassQuadtrees[i].boundary.p.x, 0, visibleGrassQuadtrees[i].boundary.p.y),
                    new Vector3(visibleGrassQuadtrees[i].boundary.halfDimension * 2, 0, visibleGrassQuadtrees[i].boundary.halfDimension * 2));
            }
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
}
