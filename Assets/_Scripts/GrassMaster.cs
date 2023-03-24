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
    [SerializeField] Texture positionMap;   // Should be an array with all of the textures? maybe the quadtree stores it
    [SerializeField] float heightDisplacementStrenght = 600f;

    [Space]

    // QUADTREE SHIT

    [Space]

    // The compute shader, assigned in editor
    [SerializeField] ComputeShader grassCompute;

    // Grass mesh and material
    [SerializeField] Mesh grassMesh;
    [SerializeField] Material grassMaterial;

    private struct GrassData
    {
        public Vector3 position;
        public float scale;
    }

    // Compute Buffer, to store the positions inside the GPU
    ComputeBuffer positionsBuffer;

    ComputeBuffer scalesBuffer;

    ComputeBuffer grassDataBuffer;


    ComputeBuffer argumentBuffer;
    private uint[] args = new uint[5] { 0, 1, 0, 0, 0 };


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
        scalesId = Shader.PropertyToID("_YScales");  // TODO: Probably will be discarded


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
        positionsBuffer = new ComputeBuffer(grassResolution * grassResolution, positionsBufferSize, ComputeBufferType.Append);  
        positionsBuffer.SetCounterValue(0);

        scalesBuffer = new ComputeBuffer(grassResolution * grassResolution, scalesBufferSize, ComputeBufferType.Append);
        scalesBuffer.SetCounterValue(0);

        grassDataBuffer = new ComputeBuffer(grassResolution * grassResolution, grassDataBufferSize, ComputeBufferType.Append);
        grassDataBuffer.SetCounterValue(0);

        SetShaderParameters();

    }


    // Making sure the garbage collector does its job and hot reload stuff
    void OnDisable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;

        scalesBuffer.Release();
        scalesBuffer = null;

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
        grassCompute.SetBuffer(0, positionsId, positionsBuffer);
        grassCompute.SetBuffer(0, scalesId, scalesBuffer);
        grassCompute.SetBuffer(0, "_GrassData", grassDataBuffer);
        //grassCompute.SetBuffer(0, "_SourceVertices", m_SourceVertBuffer);


        // Dispatching the actual shader

        uint numThreadsX;
        uint numThreadsY;
        grassCompute.GetKernelThreadGroupSizes(0, out numThreadsX, out numThreadsY, out _);

        int threadGroupsX = Mathf.CeilToInt(grassResolution / numThreadsX);
        int threadGroupsY = Mathf.CeilToInt(grassResolution / numThreadsY);


        positionsBuffer.SetCounterValue(0);
        scalesBuffer.SetCounterValue(0);
        grassDataBuffer.SetCounterValue(0);

        // DISPATCH COMPUTE SHADER !!!
        grassCompute.Dispatch(0, threadGroupsX, threadGroupsY, 1);  

        // Get arguments buffer
        argumentBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        args = new uint[5] { 0, 1, 0, 0, 0 };
        argumentBuffer.SetData(args);
        
        ComputeBuffer.CopyCount(grassDataBuffer, argumentBuffer, sizeof(uint));
        argumentBuffer.GetData(args);

        Debug.Log(args[1]);

        // Set Material attributes
        grassMaterial.SetBuffer(positionsId, positionsBuffer);
        grassMaterial.SetBuffer(scalesId, scalesBuffer);
        grassMaterial.SetBuffer("_GrassData", grassDataBuffer);

        //grassMaterial.SetBuffer("_BottomColor", );

        //positionsBuffer.GetData(grassPositions);
        
    }

    void LateUpdate()
    {
        UpdateGrassAttributes();
        //SetShaderParameters();

        // Drawign the meshes
        var bounds = new Bounds(Vector3.zero, Vector3.one * grassSquareSize);

        //Graphics.DrawMeshInstancedIndirect(grassMesh, 0, grassMaterial, bounds, argumentBuffer);
        Graphics.DrawMeshInstancedProcedural(grassMesh, 0, grassMaterial, bounds, (int)args[1]);
    }
}
