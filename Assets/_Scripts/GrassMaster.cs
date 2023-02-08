using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
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


    // Compute Buffer, to store the positions inside the GPU
    ComputeBuffer positionsBuffer;

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
        heightDisplacementStrenghtId = Shader.PropertyToID("_HeightDisplacementStrenght");


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
    // Stuff for creatinf the buffer
    void OnEnable()
    {
        int bufferSize = 3 * 4; // 3 floats per position * 4 bytes per float
        positionsBuffer = new ComputeBuffer(grassResolution * grassResolution, bufferSize);
    }
    // Making sure the garbage collector does its job and hot reload stuff
    void OnDisable()
    {
        positionsBuffer.Release();
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

        grassCompute.SetTexture(0, "_HeightMap", heightMap);
        grassCompute.SetBuffer(0, positionsId, positionsBuffer);

        // Dispatching the actual shader
        int threadGroupsX = Mathf.CeilToInt(grassResolution / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(grassResolution / 8.0f);
        grassCompute.Dispatch(0, threadGroupsX, threadGroupsY, 1);


        grassMaterial.SetBuffer(positionsId, positionsBuffer);

        // Drawign the meshes
        var bounds = new Bounds(Vector3.zero, Vector3.one * grassSquareSize);
        Graphics.DrawMeshInstancedProcedural(grassMesh, 0, grassMaterial, bounds, positionsBuffer.count);

    }

    void Update()
    {
        UpdateGrassAttributes();
        SetShaderParameters();
    }
}
