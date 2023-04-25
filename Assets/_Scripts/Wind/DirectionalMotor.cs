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

        if(directionalMotor.motorPosWS.x > 0 && directionalMotor.motorPosWS.x < 16)
        {
            Vector3 directionSigned = 
                new Vector3(directionalMotor.motorDirection.normalized.x, 
                            directionalMotor.motorDirection.normalized.y, 
                            directionalMotor.motorDirection.normalized.z);
            Debug.Log(directionSigned);

            // INIT DIRECTIONAL MOTOR
            windMaster.UpdateDirectionalMotor(directionalMotor);
        }
    }
}
