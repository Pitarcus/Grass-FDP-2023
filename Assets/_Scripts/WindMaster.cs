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
    [SerializeField] ComputeShader windComputeDirectionalMotor;
    public RenderTexture renderTexture;
   

    ComputeBuffer velocityBuffer;
    ComputeBuffer prevVelocityBuffer;   // Read only
    ComputeBuffer velocitySourcesBuffer;

    [Space]

    [Header("Texture buffers")]
    // trying out using textures instead of buffers
    RenderTexture velocityX;
    RenderTexture prevVelocityX;
    public RenderTexture velocitySourceX;
    RenderTexture velocityY;
    RenderTexture prevVelocityY;
    public RenderTexture velocitySourceY;
    RenderTexture velocityZ;
    RenderTexture prevVelocityZ;
    public RenderTexture velocitySourceZ;

    [Header("Directional Motor (testing)")]
    // Test directional motor
    public DirectionalMotor directionalMotor;

    Vector3[] sourceVelocities;
    Vector3[] testvector    ;

    ComputeBuffer testBuffer;

    ComputeBuffer pressureBuffer;
    ComputeBuffer prevPressureBuffer;   // Read only

    ComputeBuffer tmpBuffer;

    [Header("Volume parameters")]
    [SerializeField] int volumeSizeX = 16;
    [SerializeField] int volumeSizeY = 16;
    [SerializeField] int volumeSizeZ = 16;
    [SerializeField] float viscosity = 1;
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

        // X
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

        // Y
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

        // Z
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

    }
    // Start is called before the first frame update
    void Start()
    {
        numberOfVoxels = volumeSizeX * volumeSizeY * volumeSizeZ;
        Debug.Log("Number of voxels: " + numberOfVoxels);
        

        // TEXTURES INSTEAD OF BUFFERS
        InitTextures();


        // INIT DIRECTIONAL MOTOR
        //DirectionalMotor directionalMotor = new DirectionalMotor(100, new Vector3(1, 1, 0).normalized, new Vector3(0, 0, 0), 5);

        windComputeDirectionalMotor.SetTexture(0, "_velocitySourcesX", velocitySourceX);
        windComputeDirectionalMotor.SetTexture(0, "_velocitySourcesY", velocitySourceY);
        windComputeDirectionalMotor.SetTexture(0, "_velocitySourcesZ", velocitySourceZ);
        windComputeDirectionalMotor.SetFloat("_motorStrenght", directionalMotor.motorStrenght);
        windComputeDirectionalMotor.SetVector("_motorDirection", directionalMotor.motorDirection);
        windComputeDirectionalMotor.SetVector("_motorPosWS", directionalMotor.motorPosWS);
        windComputeDirectionalMotor.SetFloat("_motorRadius", directionalMotor.motorRadius);

        windComputeDirectionalMotor.Dispatch(0, 1, 1, 1);

        // starting velocities
        sourceVelocities = new Vector3[numberOfVoxels];
        testvector = new Vector3[numberOfVoxels];

        /*for (int i = 0; i < numberOfVoxels; i++)
        {
            //if (i == 10)
            //{
            //    sourceVelocities[i] = new Vector3(100f, 0.0f, 0.0f);
            //}
            //else
            sourceVelocities[i] = new Vector3(3.0f, 0.0f, 0.0f);
        }

        velocitySourcesBuffer.SetData(sourceVelocities);

        Vector3[] sourcePrevVelocities = new Vector3[numberOfVoxels];

        for (int i = 0; i < numberOfVoxels; i++)
        {
            sourcePrevVelocities[i] = new Vector3(1.0f, 0.0f, 0.0f);
        }

        prevVelocityBuffer.SetData(sourcePrevVelocities);*/

    }

    void AddForces()
    {
        windComputeAddForces.SetInt("_numberOfVoxels", numberOfVoxels);
        windComputeAddForces.SetInt("_sizeX", volumeSizeX - 2);
        windComputeAddForces.SetInt("_sizeY", volumeSizeY - 2);
        windComputeAddForces.SetInt("_sizeZ", volumeSizeZ - 2);
        windComputeAddForces.SetFloat("_gridCellSize", 1);

        windComputeAddForces.SetTexture(0, "velocityBuffer", velocityX);
        windComputeAddForces.SetTexture(0, "prevVelocityBuffer", prevVelocityX);
        windComputeAddForces.SetTexture(0, "velocitySourcesBuffer", velocitySourceX);
        windComputeAddForces.SetTexture(0, "Result", renderTexture);

        windComputeAddForces.Dispatch(0, 1, 1, 1);

        //for (int i = 0; i < numberOfVoxels; i++)
        //{
        //    Debug.Log("Add foces before copy " + testvector[i] + " / Index: " + i);
        //}

        //SwapBuffers(ref prevVelocityBuffer, ref velocityBuffer);

        //prevVelocityBuffer.GetData(testvector);
        //for (int i = 0; i < numberOfVoxels; i++)
        //{
        //    Debug.Log("Add foces after swap: " + testvector[i]);
        //}
    }

    void Advection()
    {
        windCompute.SetBuffer(advectionId, "velocityField", prevVelocityBuffer);
        windCompute.SetBuffer(advectionId, "prevQuantity", prevVelocityBuffer);
        windCompute.SetBuffer(advectionId, "newQuantity", velocityBuffer);
        windCompute.SetFloat("deltaTime", Time.deltaTime);

        windCompute.Dispatch(advectionId, 1, 1, 1);

        SwapBuffers(ref prevVelocityBuffer, ref velocityBuffer);

        //prevVelocityBuffer.GetData(testvector);
        //for (int i = 0; i < numberOfVoxels; i++)
        //{
        //    Debug.Log("Advection, after swapping : " + testvector[i]);
        //}
    }

    void Diffussion()
    {
        windCompute.SetBuffer(poissonSolverId, "bPoisson", prevVelocityBuffer);
        windCompute.SetFloat("_alpha", 1 / (viscosity * Time.deltaTime));
        windCompute.SetFloat("_beta", 1 / (viscosity * Time.deltaTime) + 6);
        windCompute.SetBuffer(poissonSolverId, "_beta", prevVelocityBuffer);

        //for (int i = 0; i < 30; i++)
        //{
        //    windCompute.SetBuffer(poissonSolverId, "x", prevVelocityBuffer);
        //    windCompute.SetBuffer(poissonSolverId, "jacobiResult", velocityBuffer);

        //    windCompute.Dispatch(poissonSolverId, 1, 1, 1);

        //    SwapBuffers(ref prevVelocityBuffer, ref velocityBuffer);
        //}

        prevVelocityBuffer.GetData(testvector);
        for (int i = 0; i < numberOfVoxels; i++)
        {
            Debug.Log("Diffussion after swap: " + testvector[i]);
        }
    }

    void Project()
    {
        // Divergence of velocity
        windCompute.SetBuffer(divergenceId, "field", prevVelocityBuffer);
        windCompute.SetBuffer(divergenceId, "divergenceField", velocityBuffer);

        windCompute.Dispatch(divergenceId, 1, 1, 1);
        SwapBuffers(ref prevVelocityBuffer, ref velocityBuffer);


        // Solve Poisson equation for pressure
        windCompute.SetBuffer(poissonSolverId, "bPoisson", prevVelocityBuffer);

        for (int i = 0; i < 30; i++)
        {
            windCompute.SetBuffer(poissonSolverId, "x", prevPressureBuffer);
            windCompute.SetBuffer(poissonSolverId, "jacobiResult", pressureBuffer);

            windCompute.Dispatch(poissonSolverId, 1 , 1, 1);

            SwapBuffers(ref prevPressureBuffer, ref pressureBuffer);
        }


        // Calculate gradient of pressure
        windCompute.SetBuffer(gradientId, "inputFieldGradient", prevPressureBuffer); 
        windCompute.SetBuffer(gradientId, "gradientField", pressureBuffer); 

        windCompute.Dispatch(gradientId, 1, 1, 1);
        SwapBuffers(ref prevPressureBuffer, ref pressureBuffer);

        // Subtract
        windCompute.SetBuffer(subtractId, "a", prevVelocityBuffer);
        windCompute.SetBuffer(subtractId, "b", pressureBuffer);
        windCompute.SetBuffer(subtractId, "subtractResult", velocityBuffer);

        windCompute.SetTexture(subtractId, "Result", renderTexture);

        windCompute.Dispatch(subtractId, 1, 1, 1);
        SwapBuffers(ref prevVelocityBuffer, ref velocityBuffer);

        prevVelocityBuffer.GetData(testvector);
        for (int i = 0; i < numberOfVoxels; i++)
        {
            Debug.Log("Projection: " + testvector[i]);
        }
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

    void SwapBuffers(ref ComputeBuffer b1, ref ComputeBuffer b2)
    {
        windCompute.SetBuffer(copyId, "_Copy_Source", b1);
        windCompute.SetBuffer(copyId, "_Copy_Target", tmpBuffer);
        windCompute.Dispatch(copyId, 2, 1, 1);

        windCompute.SetBuffer(copyId, "_Copy_Source", velocityBuffer);
        windCompute.SetBuffer(copyId, "_Copy_Target", b1);
        windCompute.Dispatch(copyId, 2, 1, 1);

        windCompute.SetBuffer(copyId, "_Copy_Source", tmpBuffer);
        windCompute.SetBuffer(copyId, "_Copy_Target", b2);
        windCompute.Dispatch(copyId, 2, 1, 1);
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
            //Advection();
            //Diffussion();
            //Project();
            //Boundary(); 


            first = true;
           // prevVelocityBuffer.GetData(testvector);
           // for (int i = 0; i < numberOfVoxels; i++)
           // {
           //     Debug.Log("Second: " + testvector[i]);
           //}
        }

    }
}
