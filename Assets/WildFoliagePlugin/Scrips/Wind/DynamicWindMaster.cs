using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct DirectionalMotorStruct
{
    public float motorStrenght;
    public Vector3 motorDirection;
    public Vector3 motorPosWS;
    public float motorRadius;

    public DirectionalMotorStruct(float strength, Vector3 direction, Vector3 position, float radius)
    {
        motorStrenght = strength;
        motorDirection = direction;
        motorPosWS = position;
        motorRadius = radius;
    }
}

public class DynamicWindMaster : MonoBehaviour
{
    [Header("Compute Shader references")]
    [SerializeField] ComputeShader windComputeAddForces;
    [SerializeField] ComputeShader windComputeAdvection;
    [SerializeField] ComputeShader windComputePoissonSolver;
    [SerializeField] ComputeShader windComputeProject;
    [SerializeField] ComputeShader windComputeDirectionalMotor;
    [SerializeField] ComputeShader swapTexturesCompute;
    [SerializeField] ComputeShader clearRT;
    [SerializeField] ComputeShader moveTexture;


    // trying out using textures instead of buffers TEXTURE ARE SERIALIZED ONLY FOR TESTING and DEBUGGING
    RenderTexture velocityX;
    RenderTexture prevVelocityX;
    RenderTexture velocitySourceX;
    RenderTexture prevVelocitySourceX;

    RenderTexture velocityY;
    RenderTexture prevVelocityY;
    RenderTexture velocitySourceY;
    RenderTexture prevVelocitySourceY;

    RenderTexture velocityZ;
    RenderTexture prevVelocityZ;
    RenderTexture velocitySourceZ;
    RenderTexture prevVelocitySourceZ;

    RenderTexture pressureTex;
    RenderTexture prevPressureTex;
   
    RenderTexture divergenceField;

    RenderTexture auxTexture;   // Auxiliar texture for swapping


    [Space]

    [Header("Volume parameters")]
    [SerializeField] int volumeSizeX = 16;
    [SerializeField] int volumeSizeY = 16;
    [SerializeField] int volumeSizeZ = 16;
    [SerializeField] float viscosity = 1;


    // Player position and grid displacement
    [SerializeField] Transform playerTransform;
    public Vector3 prevPlayerPosition { get; private set; }
    Vector3 playerPositionFloored;
    [SerializeField] public Vector3 gridPosition;
    private Vector3 gridDisplacement; // Actual value that should be displaced


    private static int JACOBI_ITERATIONS = 30;
    private int numberOfVoxels;
    private Vector3 gridSize = new Vector3(8, 8, 8);


    private void InitTextures()
    {
        // --- VELOCITIES BUFFERS ---

        // X vel
        velocitySourceX = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
        velocitySourceX.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        velocitySourceX.volumeDepth = volumeSizeZ;
        velocitySourceX.enableRandomWrite = true;
        velocitySourceX.filterMode = FilterMode.Point;

        prevVelocitySourceX = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
        prevVelocitySourceX.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        prevVelocitySourceX.volumeDepth = volumeSizeZ;
        prevVelocitySourceX.enableRandomWrite = true;
        prevVelocitySourceX.filterMode = FilterMode.Point;

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

        prevVelocitySourceY = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
        prevVelocitySourceY.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        prevVelocitySourceY.volumeDepth = volumeSizeZ;
        prevVelocitySourceY.enableRandomWrite = true;
        prevVelocitySourceY.filterMode = FilterMode.Point;

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

        prevVelocitySourceZ = new RenderTexture(volumeSizeX, volumeSizeY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
        prevVelocitySourceZ.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        prevVelocitySourceZ.volumeDepth = volumeSizeZ;
        prevVelocitySourceZ.enableRandomWrite = true;
        prevVelocitySourceZ.filterMode = FilterMode.Point;

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
       
        // Pressure
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


        // START VALUES
        //ClearRenderTexture(ref prevVelocityX, 0.5f);
        //ClearRenderTexture(ref prevVelocityY, 0.5f);
        //ClearRenderTexture(ref prevVelocityZ, 0.5f);
    }
    // Start is called before the first frame update
    void Awake()
    {
        numberOfVoxels = volumeSizeX * volumeSizeY * volumeSizeZ;
        Debug.Log("Number of voxels: " + numberOfVoxels);
        
        // TEXTURES INSTEAD OF BUFFERS
        InitTextures();

        
        gridDisplacement = gridPosition;


        // SET GLOBAL PARAMETERS OF TEXTURE AND PLAYER POSITION
        Shader.SetGlobalTexture("_WindTextureX", velocityX);
        Shader.SetGlobalTexture("_WindTextureY", velocityY);
        Shader.SetGlobalTexture("_WindTextureZ", velocityZ);

        gridSize = new Vector3(volumeSizeX, volumeSizeY, volumeSizeZ);
        Shader.SetGlobalVector("_GridSize", gridSize);

        Shader.SetGlobalVector("_PlayerPositionFloored", playerPositionFloored);
    }

    public void UpdateDirectionalMotor(DirectionalMotorStruct directionalMotor)
    {
        Vector3 directionSigned = new Vector3(directionalMotor.motorDirection.normalized.x, directionalMotor.motorDirection.normalized.y,
            directionalMotor.motorDirection.normalized.z);

        // INIT DIRECTIONAL MOTOR
        windComputeDirectionalMotor.SetTexture(0, "_velocitySourcesX", velocitySourceX);
        windComputeDirectionalMotor.SetTexture(0, "_velocitySourcesY", velocitySourceY);
        windComputeDirectionalMotor.SetTexture(0, "_velocitySourcesZ", velocitySourceZ);
        windComputeDirectionalMotor.SetFloat("_motorStrenght", directionalMotor.motorStrenght);
        windComputeDirectionalMotor.SetVector("_motorDirection", directionSigned);
        windComputeDirectionalMotor.SetVector("_motorPosWS", directionalMotor.motorPosWS);
        windComputeDirectionalMotor.SetFloat("_motorRadius", directionalMotor.motorRadius);
        windComputeDirectionalMotor.SetFloat("_deltaTime", Time.deltaTime);
        windComputeDirectionalMotor.SetVector("_gridDisplacement", gridDisplacement - playerPositionFloored);

        windComputeDirectionalMotor.Dispatch(0, volumeSizeX / 8, volumeSizeY / 8, volumeSizeZ / 8);
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

        windComputeAddForces.Dispatch(0, volumeSizeX / 8, volumeSizeY / 8, volumeSizeZ / 8);

        SwapTextures(ref velocityX, ref prevVelocityX);

        // Y
        windComputeAddForces.SetTexture(0, "velocityBuffer", velocityY);
        windComputeAddForces.SetTexture(0, "prevVelocityBuffer", prevVelocityY);
        windComputeAddForces.SetTexture(0, "velocitySourcesBuffer", velocitySourceY);

        windComputeAddForces.Dispatch(0, volumeSizeX / 8, volumeSizeY / 8, volumeSizeZ / 8);

        SwapTextures(ref velocityY, ref prevVelocityY);

        // Z
        windComputeAddForces.SetTexture(0, "velocityBuffer", velocityZ);
        windComputeAddForces.SetTexture(0, "prevVelocityBuffer", prevVelocityZ);
        windComputeAddForces.SetTexture(0, "velocitySourcesBuffer", velocitySourceZ);

        windComputeAddForces.Dispatch(0, volumeSizeX / 8, volumeSizeY / 8, volumeSizeZ / 8);

        SwapTextures(ref velocityZ, ref prevVelocityZ);
    }

    void Advection()
    {
        Vector3 domainSize = new Vector3(volumeSizeX, volumeSizeY, volumeSizeZ);

        windComputeAdvection.SetVector("domainSize", domainSize);
        windComputeAdvection.SetFloat("deltaTime", Time.deltaTime);

        windComputeAdvection.SetTexture(0, "velocityFieldX", prevVelocityX);
        windComputeAdvection.SetTexture(0, "velocityFieldY", prevVelocityY);
        windComputeAdvection.SetTexture(0, "velocityFieldZ", prevVelocityZ);

        // X
        //windComputeAdvection.SetTexture(0, "velocityField", prevVelocityX);
        windComputeAdvection.SetTexture(0, "prevQuantity", prevVelocityX);
        windComputeAdvection.SetTexture(0, "newQuantity", velocityX);
        
        windComputeAdvection.Dispatch(0, volumeSizeX / 8, volumeSizeY / 8, volumeSizeZ / 8);

        SwapTextures(ref velocityX, ref prevVelocityX);

        // Y
        //windComputeAdvection.SetTexture(1, "velocityField", prevVelocityY);
        windComputeAdvection.SetTexture(0, "prevQuantity", prevVelocityY);
        windComputeAdvection.SetTexture(0, "newQuantity", velocityY);

        windComputeAdvection.Dispatch(0, volumeSizeX / 8, volumeSizeY / 8, volumeSizeZ / 8);

        SwapTextures(ref velocityY, ref prevVelocityY);

        // Z
        //windComputeAdvection.SetTexture(2, "velocityField", prevVelocityZ);
        windComputeAdvection.SetTexture(0, "prevQuantity", prevVelocityZ);
        windComputeAdvection.SetTexture(0, "newQuantity", velocityZ);

        windComputeAdvection.Dispatch(0, volumeSizeX / 8, volumeSizeY / 8, volumeSizeZ / 8);

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

            windComputePoissonSolver.Dispatch(0, volumeSizeX / 8, volumeSizeY / 8, volumeSizeZ / 8);

            SwapTextures(ref velocityX, ref prevVelocityX);
        }

        // Y velocity diffusion
        windComputePoissonSolver.SetTexture(0, "b", prevVelocityY);

        for (int i = 0; i < JACOBI_ITERATIONS; i++)
        {
            windComputePoissonSolver.SetTexture(0, "x", prevVelocityY);
            windComputePoissonSolver.SetTexture(0, "Result", velocityY);

            windComputePoissonSolver.Dispatch(0, volumeSizeX / 8, volumeSizeY / 8, volumeSizeZ / 8);

            SwapTextures(ref velocityY, ref prevVelocityY);
        }

        // Z velocity diffusion
        windComputePoissonSolver.SetTexture(0, "b", prevVelocityZ);

        for (int i = 0; i < JACOBI_ITERATIONS; i++)
        {
            windComputePoissonSolver.SetTexture(0, "x", prevVelocityZ);
            windComputePoissonSolver.SetTexture(0, "Result", velocityZ);

            windComputePoissonSolver.Dispatch(0, volumeSizeX / 8, volumeSizeY / 8, volumeSizeZ / 8);

            SwapTextures(ref velocityZ, ref prevVelocityZ);
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

        windComputeProject.Dispatch(0, volumeSizeX / 8, volumeSizeY / 8, volumeSizeZ / 8);


        // Solve Poisson equation for pressure
        windComputePoissonSolver.SetFloat("_alpha", -1);
        windComputePoissonSolver.SetFloat("_beta", 6);
        windComputePoissonSolver.SetTexture(0, "b", divergenceField);

        for (int i = 0; i < JACOBI_ITERATIONS; i++)
        {
            windComputePoissonSolver.SetTexture(0, "x", prevPressureTex);
            windComputePoissonSolver.SetTexture(0, "Result", pressureTex);

            windComputePoissonSolver.Dispatch(0, volumeSizeX / 8, volumeSizeY / 8, volumeSizeZ / 8);

            SwapTextures(ref pressureTex, ref prevPressureTex);
        }


        // Calculate gradient of pressure
        windComputeProject.SetTexture(1, "gradientInputField", prevPressureTex);
        windComputeProject.SetTexture(1, "gradientField", pressureTex);

        windComputeProject.Dispatch(1, volumeSizeX / 8, volumeSizeY / 8, volumeSizeZ / 8);
        SwapTextures(ref pressureTex, ref prevPressureTex);


        // Subtract X
        windComputeProject.SetTexture(2, "a", divergenceField);
        windComputeProject.SetTexture(2, "b", pressureTex);
        windComputeProject.SetTexture(2, "result", velocityX);

        windComputeProject.Dispatch(2, volumeSizeX / 8, volumeSizeY / 8, volumeSizeZ / 8);
        SwapTextures(ref velocityX, ref prevVelocityX);

        // Subtract X
        windComputeProject.SetTexture(3, "a", divergenceField);
        windComputeProject.SetTexture(3, "b", pressureTex);
        windComputeProject.SetTexture(3, "result", velocityY);

        windComputeProject.Dispatch(3, volumeSizeX / 8, volumeSizeY / 8, volumeSizeZ / 8);
        SwapTextures(ref velocityY, ref prevVelocityY);

        // Subtract X
        windComputeProject.SetTexture(4, "a", divergenceField);
        windComputeProject.SetTexture(4, "b", pressureTex);
        windComputeProject.SetTexture(4, "result", velocityZ);

        windComputeProject.Dispatch(4, volumeSizeX / 8, volumeSizeY / 8, volumeSizeZ / 8);
        SwapTextures(ref velocityZ, ref prevVelocityZ);
    }

    void Boundary()
    {
        /*windCompute.SetBuffer(boundaryId, "newBoundary", velocityBuffer);
        windCompute.SetBuffer(boundaryId, "prevBoundary", prevVelocityBuffer);
        windCompute.SetInt("boundaryCondition", 1);
        windCompute.Dispatch(boundaryId, 1, 1, 1);

        //windCompute.SetBuffer(boundaryId, "newBoundary", pressureBuffer);
       // windCompute.SetBuffer(boundaryId, "prevBoundary", prevPressureBuffer);
        windCompute.SetInt("boundaryCondition", -1);
        windCompute.Dispatch(boundaryId, 1, 1, 1);*/
    }

    void SwapTextures(ref RenderTexture t1, ref RenderTexture t2)
    {
        // B TO A
        swapTexturesCompute.SetTexture(0, "textureA", auxTexture);
        swapTexturesCompute.SetTexture(0, "textureB", t1);

        swapTexturesCompute.Dispatch(0, volumeSizeX / 8, volumeSizeY / 8, volumeSizeZ / 8);

        swapTexturesCompute.SetTexture(0, "textureA", t1);
        swapTexturesCompute.SetTexture(0, "textureB", t2);

        swapTexturesCompute.Dispatch(0, volumeSizeX / 8, volumeSizeY / 8, volumeSizeZ / 8);

        swapTexturesCompute.SetTexture(0, "textureA", t2);
        swapTexturesCompute.SetTexture(0, "textureB", auxTexture);

        swapTexturesCompute.Dispatch(0, volumeSizeX / 8, volumeSizeY / 8, volumeSizeZ / 8);
    }

    void ClearRenderTexture(ref RenderTexture rt, float value)
    {
        clearRT.SetTexture(0, "Result", rt);

        clearRT.SetFloat("Value", value);

        clearRT.Dispatch(0, volumeSizeX / 8, volumeSizeY / 8, volumeSizeZ / 8);
    }

    void MoveRenderTexture(ref RenderTexture result, ref RenderTexture textureToMove, Vector3 positionDifference, Vector3 gridSize)
    {
        moveTexture.SetTexture(0, "Result", result);
        moveTexture.SetTexture(0, "textureToMove", textureToMove);

        moveTexture.SetVector("gridSize", gridSize);
        moveTexture.SetVector("positionDifference", positionDifference);

        moveTexture.Dispatch(0, volumeSizeX / 8, volumeSizeY / 8, volumeSizeZ / 8);
    }

    private void SetPlayerPosition()
    {
        prevPlayerPosition = playerPositionFloored;
        playerPositionFloored = new Vector3(Mathf.Floor(playerTransform.position.x) + 0.5f,
                                     Mathf.Floor(playerTransform.position.y) + 0.5f,
                                     Mathf.Floor(playerTransform.position.z) + 0.5f);

        Vector3 positionDifference = prevPlayerPosition - playerPositionFloored;

        if (positionDifference != Vector3.zero)
        {
            MoveRenderTexture(ref velocityX, ref prevVelocityX, -positionDifference, gridSize);
            SwapTextures(ref velocityX, ref prevVelocityX);

            MoveRenderTexture(ref velocityY, ref prevVelocityY, -positionDifference, gridSize);
            SwapTextures(ref velocityY, ref prevVelocityY);

            MoveRenderTexture(ref velocityZ, ref prevVelocityZ, -positionDifference, gridSize);
            SwapTextures(ref velocityZ, ref prevVelocityZ);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
        // FLUID SIM
        AddForces();
        Advection();
        Diffusion();
        //Project();
        //Boundary(); 

        SetPlayerPosition();

        ClearRenderTexture(ref velocitySourceX, 0);
        ClearRenderTexture(ref velocitySourceY, 0);
        ClearRenderTexture(ref velocitySourceZ, 0);

    }
}
