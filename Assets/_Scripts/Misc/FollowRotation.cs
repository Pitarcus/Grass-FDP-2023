using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowRotation : MonoBehaviour
{
    public Transform target;
    public bool followX, followY, followZ;
    //public Vector3 initialOffsetRotation;

    private Quaternion initialRotation;

    void Start()
    {
        // Save the initial rotation offset
        initialRotation = transform.rotation;
    }

    void Update()
    {
        // Get the target's rotation
        Quaternion targetRotation = target.rotation;

        // Apply the initial rotation offset
        targetRotation *= initialRotation;

        // Only follow the target's rotation in the specified axes
        if (!followX) targetRotation.x = transform.rotation.x;
        if (!followY) targetRotation.y = transform.rotation.y;
        if (!followZ) targetRotation.z = transform.rotation.z;

        // Set the rotation of this object to match the target's rotation
        transform.rotation = targetRotation;
    }
}
