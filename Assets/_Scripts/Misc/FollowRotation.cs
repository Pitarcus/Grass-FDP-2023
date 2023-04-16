using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowRotation : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] bool followX;
    [SerializeField] bool followY;
    [SerializeField] bool followZ;

    int x, y, z;

    private float offsetX;
    private float offsetY;
    private float offsetZ;

    Quaternion newRotation;

    private void Start()
    {
        offsetX = transform.rotation.eulerAngles.x;
        offsetY = transform.rotation.eulerAngles.y;
        offsetZ = transform.rotation.eulerAngles.z;
        x = followX ? 1 : 0;
        y = followY ? 1 : 0;
        z = followZ ? 1 : 0;
    }
    // Update is called once per frame
    void Update()
    {
        newRotation = Quaternion.Euler(target.rotation.x * x + offsetX, target.rotation.y * y + offsetY, target.rotation.z * z + offsetZ);
        transform.rotation = newRotation;
    }
}
