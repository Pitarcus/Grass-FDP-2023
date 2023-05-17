using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetShaderPlayerPosition : MonoBehaviour
{
    [SerializeField] Transform playerTransform;
    Vector3 playerPosition;
   
    // Update is called once per frame
    void Update()
    {
        playerPosition = playerTransform.position;
        Shader.SetGlobalVector("_PlayerPositionFollow", playerPosition);
    }
}
