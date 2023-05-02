using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionalMotor : MonoBehaviour
{
    [SerializeField] ComputeShader directionalWindGenerator;

    [SerializeField] WindMaster windMaster;

    [SerializeField] DirectionalMotorStruct directionalMotor;
   

    // Update is called once per frame
    void Update()
    {
        directionalMotor.motorPosWS = transform.position;
        directionalMotor.motorDirection = transform.forward;

        // INIT DIRECTIONAL MOTOR
        windMaster.UpdateDirectionalMotor(directionalMotor);
    }
}
