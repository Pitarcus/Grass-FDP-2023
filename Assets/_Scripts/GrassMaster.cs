using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassMaster : MonoBehaviour
{
    // Parameters
    [Range(1, 1000)]
    [SerializeField] int grassSquareSize = 300;
    [Range(1, 5)]
    [SerializeField] int grassDensity = 1;
    [Range(0, 2)]
    [SerializeField] float offsetXAmount = 0.5f;
    [Range(0, 2)]
    [SerializeField] float offsetYAmount = 0.5f;

    [SerializeField] Texture heightMap;
    [SerializeField] float heightDisplacementStrenght = 600f;

    [Space]
     


    // The compute shader, assigned in editor
    [SerializeField] ComputeShader grassCompute;



    // Grass mesh and material
    [SerializeField] Mesh grassMesh;
    [SerializeField] Material grassMaterial;



    // Grass Positions
    [SerializeField] GrassPainter grassPainter;
    private Mesh grassPositionsMesh;

    private int numberOfSourceVertices;

    // The structure to send to the compute shader
    // This layout kind assures that the data is laid out sequentially
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind
        .Sequential)]
    private struct SourceVertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
        public Vector3 color;
    }

    SourceVertex[] vertices;


    // Compute Buffer, to store the positions inside the GPU
    ComputeBuffer positionsBuffer;
    // A compute buffer to hold vertex data of the source mesh
    private ComputeBuffer m_SourceVertBuffer;


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
        heightDisplacementStrenghtId = Shader.PropertyToID("_HeightDisplacementStrenght");  // TODO: Probably will be discarded


    // CONST
    private const int SOURCE_VERT_STRIDE = sizeof(float) * (3 + 3 + 2 + 3);
    private int positionsBufferSize = 3 * 4; // 3 floats per position * 4 bytes per float



    // ----- FUNCTIONS ------


    private void Awake()
    {
        grassResolution = grassSquareSize * grassDensity;
        grassStep = grassSquareSize / (float) grassResolution;
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

        //positionsBuffer = new ComputeBuffer(grassResolution * grassResolution, positionsBufferSize);




        // Grab data from the source mesh

        grassPositionsMesh = grassPainter.positionsMesh;

        Vector3[] positions = grassPositionsMesh.vertices;
        Vector3[] normals = grassPositionsMesh.normals;
        Vector2[] uvs = grassPositionsMesh.uv;
        Color[] colors = grassPositionsMesh.colors;

        // Create the data to upload to the source vert buffer
        vertices = new SourceVertex[positions.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Color color = colors[i];
            vertices[i] = new SourceVertex()
            {
                position = positions[i],
                normal = normals[i],
                uv = uvs[i],
                color = new Vector3(color.r, color.g, color.b) // Color --> Vector3
            };
        }

        numberOfSourceVertices = vertices.Length;



        // Create the compute buffers
        positionsBuffer = new ComputeBuffer(numberOfSourceVertices, positionsBufferSize, ComputeBufferType.Append);
        positionsBuffer.SetCounterValue(0);

        m_SourceVertBuffer = new ComputeBuffer(numberOfSourceVertices, SOURCE_VERT_STRIDE,
           ComputeBufferType.Structured, ComputeBufferMode.Immutable);

        m_SourceVertBuffer.SetData(vertices);


        SetShaderParameters();
    }


    // Making sure the garbage collector does its job and hot reload stuff
    void OnDisable()
    {
        positionsBuffer.Release();
        m_SourceVertBuffer.Release();
        positionsBuffer = null;
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
        grassCompute.SetBuffer(0, positionsId, positionsBuffer);
        grassCompute.SetBuffer(0, "_SourceVertices", m_SourceVertBuffer);


        // Dispatching the actual shader
        //uint threadGroupsX = Mathf.CeilToInt(grassResolution / 8.0f);
        //uint threadGroupsY = Mathf.CeilToInt(grassResolution / 8.0f);
        uint threadGroupsX;
        uint threadGroupsY;
        grassCompute.GetKernelThreadGroupSizes(0, out threadGroupsX, out threadGroupsY, out _);

        int dispatchSizeX = Mathf.CeilToInt((float)numberOfSourceVertices / threadGroupsX);
        //int dispatchSizeY = Mathf.CeilToInt((float)numberOfSourceVertices / threadGroupsY);

        grassCompute.Dispatch(0, dispatchSizeX, 1, 1);

        // Set Material attributes
        grassMaterial.SetBuffer(positionsId, positionsBuffer);
        //grassMaterial.SetBuffer("_BottomColor", );



        // Getting info of the gras positions �...?
        Vector3[] grassPositions = new Vector3[grassPositionsMesh.vertexCount];

        positionsBuffer.GetData(grassPositions);
    }

    void LateUpdate()
    {
        UpdateGrassAttributes();
        //SetShaderParameters();

        // Drawign the meshes
        var bounds = new Bounds(Vector3.zero, Vector3.one * grassSquareSize);
        Graphics.DrawMeshInstancedProcedural(grassMesh, 0, grassMaterial, bounds, positionsBuffer.count);
    }
}
