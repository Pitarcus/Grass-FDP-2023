using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionalMotor : MonoBehaviour
{
    [SerializeField] ComputeShader directionalWindGenerator;

    [SerializeField] DynamicWindMaster windMaster;

    [SerializeField] DirectionalMotorStruct directionalMotor;
   

    // Update is called once per frame
    void Update()
    {
        directionalMotor.motorPosWS = transform.position;
        directionalMotor.motorDirection = transform.forward;

        // SHOULD BE CHECKING IF IT'S IN RANGE OD THE VOLUME (not run the shader for every motor in the world)
        // INIT DIRECTIONAL MOTOR
        windMaster.UpdateDirectionalMotor(directionalMotor);
    }
}
