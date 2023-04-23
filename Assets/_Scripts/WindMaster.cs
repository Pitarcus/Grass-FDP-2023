using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct DirectionalMotor
{
    public float motorStrenght;
    public Vector3 motorDirection;
    public Vector3 motorPosWS;
    public float motorRadius;

    public DirectionalMotor(float strength, Vector3 direction, Vector3 position, float radius)
    {
        motorStrenght = strength;
        motorDirection = direction;
        motorPosWS = position;
        motorRadius = radius;
    }
}

public class WindMaster : MonoBehaviour
{
    [Header("Compute Shader references")]
    [SerializeField] ComputeShader windCompute;
    [SerializeField] ComputeShader windComputeAddForces;
    [SerializeField] ComputeShader windComputeAdvection;
    [SerializeField] ComputeShader windComputePoissonSolver;
    [SerializeField] ComputeShader windComputeProject;
    [SerializeField] ComputeShader windComputeDirectionalMotor;
    [SerializeField] ComputeShader swapTexturesCompute;
    public RenderTexture renderTexture;
   

    ComputeBuffer velocityBuffer;
    ComputeBuffer prevVelocityBuffer;   // Read only
    ComputeBuffer velocitySourcesBuffer;

    [Space]

    [Header("Texture buffers")]
    // trying out using textures instead of buffers
    [SerializeField] public RenderTexture velocityX;
    [SerializeField] RenderTexture prevVelocityX;
    [SerializeField] RenderTexture velocitySourceX;
    public RenderTexture velocityY;
    RenderTexture prevVelocityY;
    [SerializeField] RenderTexture velocitySourceY;
    public RenderTexture velocityZ;
    RenderTexture prevVelocityZ;
    [SerializeField] RenderTexture velocitySourceZ;

    [SerializeField] RenderTexture pressureTex;
    [SerializeField] RenderTexture prevPressureTex;
    /*[SerializeField] RenderTexture pressureX;
    [SerializeField] RenderTexture prevPressureX;
    RenderTexture pressureY;
    RenderTexture prevPressureY;
    RenderTexture pressureZ;
    RenderTexture prevPressureZ;*/

    RenderTexture divergenceField;

    RenderTexture auxTexture;   // Auxiliar texture for swapping

    [Space]

    [Header("Directional Motor (testing)")]
    // Test directional motor
    public DirectionalMotor directionalMotor;

    Vector3[] sourceVelocities;
    Vector3[] testvector;

    ComputeBuffer testBuffer;

    ComputeBuffer pressureBuffer;
    ComputeBuffer prevPressureBuffer;   // Read only

    ComputeBuffer tmpBuffer;

    [Space]

    [Header("Volume parameters")]
    [SerializeField] int volumeSizeX = 16;
    [SerializeField] int volumeSizeY = 16;
    [SerializeField] int volumeSizeZ = 16;
    [SerializeField] float viscosity = 1;
    static int JACOBI_ITERATIONS = 35;
    int numberOfVoxels;

    private int velocityBufferSize = sizeof(float) * 3;
    private int pressureBufferSize = sizeof(float) * 3;

    int forceFluidId, advectionId, poissonSolverId, divergenceId, gradientId, subtractId, boundaryId, copyId;

    public bool first = false;


    private void InitTextures()
    {
        // visualization RT
        renderTexture = new RenderTexture(volumeSizeX, volumeSizeY, 0);
        renderTexture.enableRandomWrite = true;
        renderTexture.filterMode = FilterMode.Point;
        renderTexture.Create();

        // --- VELOCITIES BUFFERS ---

        // X vel
        velocitySourceX = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
        velocitySourceX.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        velocitySourceX.volumeDepth = volumeSizeZ;
        velocitySourceX.enableRandomWrite = true;
        velocitySourceX.filterMode = FilterMode.Point;

        velocityX = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
        velocityX.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        velocityX.volumeDepth = volumeSizeZ;
        velocityX.filterMode = FilterMode.Point;
        velocityX.enableRandomWrite = true;

        prevVelocityX = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
        prevVelocityX.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        prevVelocityX.volumeDepth = volumeSizeZ;
        prevVelocityX.filterMode = FilterMode.Point;
        prevVelocityX.enableRandomWrite = true;

        // Y vel
        velocitySourceY = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
        velocitySourceY.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        velocitySourceY.volumeDepth = volumeSizeZ;
        velocitySourceY.enableRandomWrite = true;
        velocitySourceY.filterMode = FilterMode.Point;

        velocityY = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
        velocityY.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        velocityY.volumeDepth = volumeSizeZ;
        velocityY.filterMode = FilterMode.Point;
        velocityY.enableRandomWrite = true;

        prevVelocityY = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
        prevVelocityY.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        prevVelocityY.volumeDepth = volumeSizeZ;
        prevVelocityY.filterMode = FilterMode.Point;
        prevVelocityY.enableRandomWrite = true;

        // Z vel
        velocitySourceZ = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
        velocitySourceZ.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        velocitySourceZ.volumeDepth = volumeSizeZ;
        velocitySourceZ.enableRandomWrite = true;
        velocitySourceZ.filterMode = FilterMode.Point;

        velocityZ = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
        velocityZ.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        velocityZ.volumeDepth = volumeSizeZ;
        velocityZ.filterMode = FilterMode.Point;
        velocityZ.enableRandomWrite = true;

        prevVelocityZ = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
        prevVelocityZ.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        prevVelocityZ.volumeDepth = volumeSizeZ;
        prevVelocityZ.filterMode = FilterMode.Point;
        prevVelocityZ.enableRandomWrite = true;
        /*
        // X pressure
        pressureX = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
        pressureX.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        pressureX.volumeDepth = volumeSizeZ;
        pressureX.filterMode = FilterMode.Point;
        pressureX.enableRandomWrite = true;

        prevPressureX = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
        prevPressureX.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        prevPressureX.volumeDepth = volumeSizeZ;
        prevPressureX.filterMode = FilterMode.Point;
        prevPressureX.enableRandomWrite = true;
        // Y pressure
        pressureY = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
        pressureY.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        pressureY.volumeDepth = volumeSizeZ;
        pressureY.filterMode = FilterMode.Point;
        pressureY.enableRandomWrite = true;

        prevPressureY = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
        prevPressureY.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        prevPressureY.volumeDepth = volumeSizeZ;
        prevPressureY.filterMode = FilterMode.Point;
        prevPressureY.enableRandomWrite = true;

        // Z pressure
        pressureZ = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
        pressureZ.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        pressureZ.volumeDepth = volumeSizeZ;
        pressureZ.filterMode = FilterMode.Point;
        pressureZ.enableRandomWrite = true;

        prevPressureZ = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
        prevPressureZ.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        prevPressureZ.volumeDepth = volumeSizeZ;
        prevPressureZ.filterMode = FilterMode.Point;
        prevPressureZ.enableRandomWrite = true;
        */

        pressureTex = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat);
        pressureTex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        pressureTex.volumeDepth = volumeSizeZ;
        pressureTex.filterMode = FilterMode.Point;
        pressureTex.enableRandomWrite = true;

        prevPressureTex = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat);
        prevPressureTex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        prevPressureTex.volumeDepth = volumeSizeZ;
        prevPressureTex.filterMode = FilterMode.Point;
        prevPressureTex.enableRandomWrite = true;

        // Diverngence field
        divergenceField = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
        divergenceField.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        divergenceField.volumeDepth = volumeSizeZ;
        divergenceField.enableRandomWrite = true;
        divergenceField.filterMode = FilterMode.Point;
        // Aux
        auxTexture = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
        auxTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        auxTexture.volumeDepth = volumeSizeZ;
        auxTexture.enableRandomWrite = true;
        auxTexture.filterMode = FilterMode.Point;

    }
    // Start is called before the first frame update
    void Awake()
    {
        numberOfVoxels = volumeSizeX * volumeSizeY * volumeSizeZ;
        Debug.Log("Number of voxels: " + numberOfVoxels);
        
        // TEXTURES INSTEAD OF BUFFERS
        InitTextures();

        // INIT DIRECTIONAL MOTOR
        windComputeDirectionalMotor.SetTexture(0, "_velocitySourcesX", velocitySourceX);
        windComputeDirectionalMotor.SetTexture(0, "_velocitySourcesY", velocitySourceY);
        windComputeDirectionalMotor.SetTexture(0, "_velocitySourcesZ", velocitySourceZ);
        windComputeDirectionalMotor.SetFloat("_motorStrenght", directionalMotor.motorStrenght);
        windComputeDirectionalMotor.SetVector("_motorDirection", directionalMotor.motorDirection);
        windComputeDirectionalMotor.SetVector("_motorPosWS", directionalMotor.motorPosWS);
        windComputeDirectionalMotor.SetFloat("_motorRadius", directionalMotor.motorRadius);

        windComputeDirectionalMotor.Dispatch(0, volumeSizeX, volumeSizeY, volumeSizeZ);

        // starting velocities
        sourceVelocities = new Vector3[numberOfVoxels];
        testvector = new Vector3[numberOfVoxels];

    }

    void AddForces()
    {
        windComputeAddForces.SetInt("_numberOfVoxels", numberOfVoxels);
        windComputeAddForces.SetInt("_sizeX", volumeSizeX - 2);
        windComputeAddForces.SetInt("_sizeY", volumeSizeY - 2);
        windComputeAddForces.SetInt("_sizeZ", volumeSizeZ - 2);
        windComputeAddForces.SetFloat("_gridCellSize", 1);

        // X
        windComputeAddForces.SetTexture(0, "velocityBuffer", velocityX);
        windComputeAddForces.SetTexture(0, "prevVelocityBuffer", prevVelocityX);
        windComputeAddForces.SetTexture(0, "velocitySourcesBuffer", velocitySourceX);

        windComputeAddForces.Dispatch(0, volumeSizeX, volumeSizeY, volumeSizeZ);

        SwapTextures(ref velocityX, ref prevVelocityX);

        // Y
        windComputeAddForces.SetTexture(0, "velocityBuffer", velocityY);
        windComputeAddForces.SetTexture(0, "prevVelocityBuffer", prevVelocityY);
        windComputeAddForces.SetTexture(0, "velocitySourcesBuffer", velocitySourceY);

        windComputeAddForces.Dispatch(0, volumeSizeX, volumeSizeY, volumeSizeZ);

        SwapTextures(ref velocityY, ref prevVelocityY);

        // Z
        windComputeAddForces.SetTexture(0, "velocityBuffer", velocityZ);
        windComputeAddForces.SetTexture(0, "prevVelocityBuffer", prevVelocityZ);
        windComputeAddForces.SetTexture(0, "velocitySourcesBuffer", velocitySourceZ);

        windComputeAddForces.Dispatch(0, volumeSizeX, volumeSizeY, volumeSizeZ);

        SwapTextures(ref velocityZ, ref prevVelocityZ);
    }

    void Advection()
    {
        Vector3 domainSize = new Vector3(volumeSizeX, volumeSizeY, volumeSizeZ);

        windComputeAdvection.SetVector("domainSize", domainSize);
        windComputeAdvection.SetFloat("deltaTime", 1);

        // X
        windComputeAdvection.SetTexture(0, "velocityField", prevVelocityX);
        windComputeAdvection.SetTexture(0, "prevQuantity", prevVelocityX);
        windComputeAdvection.SetTexture(0, "newQuantity", velocityX);
        
        windComputeAdvection.Dispatch(0, volumeSizeX, volumeSizeY, volumeSizeZ);

        SwapTextures(ref velocityX, ref prevVelocityX);

        // Y
        windComputeAdvection.SetTexture(1, "velocityField", prevVelocityY);
        windComputeAdvection.SetTexture(1, "prevQuantity", prevVelocityY);
        windComputeAdvection.SetTexture(1, "newQuantity", velocityY);

        windComputeAdvection.Dispatch(1, volumeSizeX, volumeSizeY, volumeSizeZ);

        SwapTextures(ref velocityY, ref prevVelocityY);

        // Z
        windComputeAdvection.SetTexture(2, "velocityField", prevVelocityZ);
        windComputeAdvection.SetTexture(2, "prevQuantity", prevVelocityZ);
        windComputeAdvection.SetTexture(2, "newQuantity", velocityZ);

        windComputeAdvection.Dispatch(2, volumeSizeX, volumeSizeY, volumeSizeZ);

        SwapTextures(ref velocityZ, ref prevVelocityZ);
    }

    void Diffusion()
    {
       
        windComputePoissonSolver.SetFloat("_alpha", 1 / (viscosity * Time.deltaTime));
        windComputePoissonSolver.SetFloat("_beta", 1 / (viscosity * Time.deltaTime) + 6);

        // X velocity diffusion
        windComputePoissonSolver.SetTexture(0, "b", prevVelocityX);
        
        for(int i = 0; i < JACOBI_ITERATIONS; i++)
        {
            windComputePoissonSolver.SetTexture(0, "x", prevVelocityX);
            windComputePoissonSolver.SetTexture(0, "Result", velocityX);

            windComputePoissonSolver.Dispatch(0, volumeSizeX, volumeSizeY, volumeSizeZ);

            SwapTextures(ref velocityX, ref prevVelocityX);
        }

        // Y velocity diffusion
        windComputePoissonSolver.SetTexture(0, "b", prevVelocityY);

        for (int i = 0; i < JACOBI_ITERATIONS; i++)
        {
            windComputePoissonSolver.SetTexture(0, "x", prevVelocityY);
            windComputePoissonSolver.SetTexture(0, "Result", velocityY);

            windComputePoissonSolver.Dispatch(0, volumeSizeX, volumeSizeY, volumeSizeZ);

            SwapTextures(ref velocityY, ref prevVelocityY);
        }

        // Z velocity diffusion
        windComputePoissonSolver.SetTexture(0, "b", prevVelocityZ);

        for (int i = 0; i < JACOBI_ITERATIONS; i++)
        {
            windComputePoissonSolver.SetTexture(0, "x", prevVelocityZ);
            windComputePoissonSolver.SetTexture(0, "Result", velocityZ);

            windComputePoissonSolver.Dispatch(0, volumeSizeX, volumeSizeY, volumeSizeZ);

            SwapTextures(ref velocityY, ref prevVelocityZ);
        }
    }

    void Project()
    {
        // Divergence of velocity
        windComputeProject.SetTexture(0, "fieldX", prevVelocityX);
        windComputeProject.SetTexture(0, "fieldY", prevVelocityY);
        windComputeProject.SetTexture(0, "fieldZ", prevVelocityZ);
        windComputeProject.SetTexture(0, "divergenceField", divergenceField);

        windComputeProject.SetFloat("gridCellSize", 1);

        windComputeProject.Dispatch(0, volumeSizeX, volumeSizeY, volumeSizeZ);


        // Solve Poisson equation for pressure
        windComputePoissonSolver.SetTexture(0, "b", divergenceField);

        for (int i = 0; i < JACOBI_ITERATIONS; i++)
        {
            windComputePoissonSolver.SetTexture(0, "x", prevPressureTex);
            windComputePoissonSolver.SetTexture(0, "jacobiResult", pressureTex);

            windComputePoissonSolver.Dispatch(0, volumeSizeX, volumeSizeY, volumeSizeZ);

            SwapTextures(ref pressureTex, ref prevPressureTex);
        }


        // Calculate gradient of pressure
        windComputeProject.SetTexture(1, "gradientInputField", prevPressureTex);
        windComputeProject.SetTexture(1, "gradientField", pressureTex);

        windComputeProject.Dispatch(1, volumeSizeX, volumeSizeY, volumeSizeZ);
        SwapTextures(ref pressureTex, ref prevPressureTex);


        // Subtract X
        windComputeProject.SetTexture(2, "a", divergenceField);
        windComputeProject.SetTexture(2, "b", pressureTex);
        windComputeProject.SetTexture(2, "result", velocityX);

        windComputeProject.Dispatch(2, volumeSizeX, volumeSizeY, volumeSizeZ);
        SwapTextures(ref velocityX, ref prevVelocityX);

        // Subtract X
        windComputeProject.SetTexture(3, "a", divergenceField);
        windComputeProject.SetTexture(3, "b", pressureTex);
        windComputeProject.SetTexture(3, "result", velocityY);

        windComputeProject.Dispatch(3, volumeSizeX, volumeSizeY, volumeSizeZ);
        SwapTextures(ref velocityY, ref prevVelocityY);

        // Subtract X
        windComputeProject.SetTexture(4, "a", divergenceField);
        windComputeProject.SetTexture(4, "b", pressureTex);
        windComputeProject.SetTexture(4, "result", velocityZ);

        windComputeProject.Dispatch(4, volumeSizeX, volumeSizeY, volumeSizeZ);
        SwapTextures(ref velocityZ, ref prevVelocityZ);
    }

    void Boundary()
    {
        windCompute.SetBuffer(boundaryId, "newBoundary", velocityBuffer);
        windCompute.SetBuffer(boundaryId, "prevBoundary", prevVelocityBuffer);
        windCompute.SetInt("boundaryCondition", 1);
        windCompute.Dispatch(boundaryId, 1, 1, 1);

        windCompute.SetBuffer(boundaryId, "newBoundary", pressureBuffer);
        windCompute.SetBuffer(boundaryId, "prevBoundary", prevPressureBuffer);
        windCompute.SetInt("boundaryCondition", -1);
        windCompute.Dispatch(boundaryId, 1, 1, 1);

        prevVelocityBuffer.GetData(testvector);
        for (int i = 0; i < numberOfVoxels; i++)
        {
            Debug.Log("Boundary: " + testvector[i]);
        }
    }

    void SwapTextures(ref RenderTexture t1, ref RenderTexture t2)
    {
        // B TO A
        swapTexturesCompute.SetTexture(0, "textureA", auxTexture);
        swapTexturesCompute.SetTexture(0, "textureB", t1);

        swapTexturesCompute.Dispatch(0, volumeSizeX, volumeSizeY, volumeSizeZ);

        swapTexturesCompute.SetTexture(0, "textureA", t1);
        swapTexturesCompute.SetTexture(0, "textureB", t2);

        swapTexturesCompute.Dispatch(0, volumeSizeX, volumeSizeY, volumeSizeZ);

        swapTexturesCompute.SetTexture(0, "textureA", t2);
        swapTexturesCompute.SetTexture(0, "textureB", auxTexture);

        swapTexturesCompute.Dispatch(0, volumeSizeX, volumeSizeY, volumeSizeZ);
    }

    private void OnDisable()
    {
        if (velocityBuffer != null)
            velocityBuffer.Release();
        velocityBuffer = null;

        if (prevVelocityBuffer != null)
            prevVelocityBuffer.Release();
        prevVelocityBuffer = null;

        if (velocitySourcesBuffer != null)
            velocitySourcesBuffer.Release();
        velocitySourcesBuffer = null;

        if (pressureBuffer != null)
            pressureBuffer.Release();
        pressureBuffer = null;

        if (prevPressureBuffer != null)
            prevPressureBuffer.Release();
        prevPressureBuffer = null;

        if (tmpBuffer != null)
            tmpBuffer.Release();
        tmpBuffer = null;

    }

    // Update is called once per frame
    void Update()
    {
        if (!first)
        {
            //sourceVelocities[0] = new Vector3(1, 1, 1);

            //velocitySourcesBuffer.SetData(sourceVelocities);

            //velocitySourcesBuffer.GetData(sourceVelocities);
            //for (int i = 0; i < numberOfVoxels; i++)
            //{
            //    Debug.Log("First: " + sourceVelocities[i]);
            //}

            AddForces();
            Advection();
            Diffusion();
            //Project();
            //Boundary(); 


            //first = true;
           // prevVelocityBuffer.GetData(testvector);
           // for (int i = 0; i < numberOfVoxels; i++)
           // {
           //     Debug.Log("Second: " + testvector[i]);
           //}
        }

    }
}
