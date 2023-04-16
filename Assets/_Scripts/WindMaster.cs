using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindMaster : MonoBehaviour
{
    [SerializeField] ComputeShader windCompute;
    ComputeBuffer velocityBuffer;
    ComputeBuffer prevVelocityBuffer;
    ComputeBuffer velocitySourcesBuffer;

    [SerializeField] int volumeSizeX;
    [SerializeField] int volumeSizeY;
    [SerializeField] int volumeSizeZ;
    int numberOfVoxels;

    private int velocityBufferSize = sizeof(float) * 3;

    // Start is called before the first frame update
    void Start()
    {
        numberOfVoxels = volumeSizeX * volumeSizeY * volumeSizeZ;
        velocityBuffer = new ComputeBuffer(numberOfVoxels, velocityBufferSize);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
