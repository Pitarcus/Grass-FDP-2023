using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetShaderPlayerPosition : MonoBehaviour
{
    [SerializeField] Transform playerTransform;
    Vector3 playerPosition;
    Vector3 playerPositionFloored;

    // Update is called once per frame
    void Update()
    {
        playerPosition = playerTransform.position;

        playerPositionFloored = new Vector3(Mathf.Floor(playerTransform.position.x) + 0.5f,
                                     Mathf.Floor(playerTransform.position.y) + 0.5f,
                                     Mathf.Floor(playerTransform.position.z) + 0.5f);


        Shader.SetGlobalVector("_PlayerPositionFollow", playerPosition);

        Shader.SetGlobalVector("_PlayerPositionFloored", playerPositionFloored);
    }
}
