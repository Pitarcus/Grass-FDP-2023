using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindMaster : MonoBehaviour
{
    [SerializeField] ComputeShader windCompute;
    public RenderTexture renderTexture;
    public float viscosity = 1;

    ComputeBuffer velocityBuffer;
    ComputeBuffer prevVelocityBuffer;   // Read only
    ComputeBuffer velocitySourcesBuffer;
    Vector3[] sourceVelocities;
    Vector3[] testvector;

    ComputeBuffer testBuffer;

    ComputeBuffer pressureBuffer;
    ComputeBuffer prevPressureBuffer;   // Read only

    ComputeBuffer tmpBuffer;

    [SerializeField] int volumeSizeX = 16;
    [SerializeField] int volumeSizeY = 16;
    [SerializeField] int volumeSizeZ = 16;
    int numberOfVoxels;

    private int velocityBufferSize = sizeof(float) * 3;
    private int pressureBufferSize = sizeof(float) * 3;

    int forceFluidId, advectionId, poissonSolverId, divergenceId, gradientId, subtractId, boundaryId, copyId;

    public bool first = false;

    // Start is called before the first frame update
    void Start()
    {
        numberOfVoxels = volumeSizeX * volumeSizeY * volumeSizeZ;
        Debug.Log("Number of voxels: " + numberOfVoxels);
        // Buffer allocation
        velocityBuffer = new ComputeBuffer(numberOfVoxels, velocityBufferSize);
        prevVelocityBuffer = new ComputeBuffer(numberOfVoxels, velocityBufferSize);
        velocitySourcesBuffer = new ComputeBuffer(numberOfVoxels, velocityBufferSize);

        pressureBuffer = new ComputeBuffer(numberOfVoxels, pressureBufferSize);
        prevPressureBuffer = new ComputeBuffer(numberOfVoxels, pressureBufferSize);

        tmpBuffer = new ComputeBuffer(numberOfVoxels, pressureBufferSize);
        //testBuffer = new ComputeBuffer(numberOfVoxels, pressureBufferSize);

        // Kernel IDs
        forceFluidId = windCompute.FindKernel("ForceGPUFluidSim3D");
        advectionId = windCompute.FindKernel("AdvectionGPUFluidSim3D");
        poissonSolverId = windCompute.FindKernel("PoissonSolver3D");
        divergenceId = windCompute.FindKernel("Divergence3D");
        gradientId = windCompute.FindKernel("Gradient3D");
        subtractId = windCompute.FindKernel("Subtract3D");
        boundaryId = windCompute.FindKernel("BoundaryGPUFluidSim3D");
        copyId = windCompute.FindKernel("Copy_StructuredBuffer");

        // Uniforms
        windCompute.SetInt("_numberOfVoxels", numberOfVoxels);
        windCompute.SetInt("_sizeX", volumeSizeX - 2);
        windCompute.SetInt("_sizeY", volumeSizeY - 2);
        windCompute.SetInt("_sizeZ", volumeSizeZ - 2);
        windCompute.SetFloat("_gridCellSize", 1);

        renderTexture = new RenderTexture(16, 16, 16);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        // starting velocities
        sourceVelocities = new Vector3[numberOfVoxels];
        testvector = new Vector3[numberOfVoxels];

        for( int i = 0; i < numberOfVoxels; i++)
        {
            sourceVelocities[i] = new Vector3(100, 100, 100);
        }
        
        velocitySourcesBuffer.SetData(sourceVelocities);
    }

    void AddForces()
    {
        windCompute.SetBuffer(0, "velocityBuffer", velocityBuffer);
        windCompute.SetBuffer(0, "prevVelocityBuffer", prevVelocityBuffer);
        windCompute.SetBuffer(0, "velocitySourcesBuffer", velocitySourcesBuffer);

        windCompute.Dispatch(0, 1, 1, 1);

        velocityBuffer.GetData(testvector);
        for (int i = 0; i < numberOfVoxels; i++)
        {
            Debug.Log("Add foces before copy " + testvector[i] +  " / Index: " + i);
        }

        //SwapBuffers(ref prevVelocityBuffer, ref velocityBuffer);

       /* prevVelocityBuffer.GetData(testvector);
        for (int i = 0; i < numberOfVoxels; i++)
        {
            Debug.Log("Add foces 1: " + testvector[i]);
        }*/
    }

    void Advection()
    {
        windCompute.SetBuffer(advectionId, "velocityField", prevVelocityBuffer);
        windCompute.SetBuffer(advectionId, "prevQuantity", prevVelocityBuffer);
        windCompute.SetBuffer(advectionId, "newQuantity", velocityBuffer);
        windCompute.SetFloat("deltaTime", Time.deltaTime);

        windCompute.Dispatch(advectionId, 1, 1, 1);

        SwapBuffers(ref prevVelocityBuffer, ref velocityBuffer);

        prevVelocityBuffer.GetData(testvector);
        for (int i = 0; i < numberOfVoxels; i++)
        {
            Debug.Log("Advection : " + testvector[i]);
        }
    }

    void Diffussion()
    {
        windCompute.SetBuffer(poissonSolverId, "bPoisson", prevVelocityBuffer);
        windCompute.SetFloat("_alpha", 1 / (viscosity * Time.deltaTime));
        windCompute.SetFloat("_beta", 1 / (viscosity * Time.deltaTime) + 6);
        windCompute.SetBuffer(poissonSolverId, "_beta", prevVelocityBuffer);

        for (int i = 0; i < 30; i++)
        {
            windCompute.SetBuffer(poissonSolverId, "x", prevVelocityBuffer);
            windCompute.SetBuffer(poissonSolverId, "jacobiResult", velocityBuffer);

            windCompute.Dispatch(poissonSolverId, 1, 1, 1);

            SwapBuffers(ref prevVelocityBuffer, ref velocityBuffer);
        }

        prevVelocityBuffer.GetData(testvector);
        for (int i = 0; i < numberOfVoxels; i++)
        {
            Debug.Log("Diffussion: " + testvector[i]);
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
            prevVelocityBuffer.GetData(testvector);
            for (int i = 0; i < numberOfVoxels; i++)
            {
                Debug.Log("Second: " + testvector[i]);
           }
        }

    }
}
