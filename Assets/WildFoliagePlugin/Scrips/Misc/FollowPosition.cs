using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPosition : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] bool followX;
    [SerializeField] bool keepOffsetX;
    [SerializeField] bool followY;
    [SerializeField] bool keepOffsetY;
    [SerializeField] bool followZ;
    [SerializeField] bool keepOffsetZ;
    [SerializeField] bool followRotation = true;
    [SerializeField] bool followFloored;
    [SerializeField] bool followSmooth;
    [SerializeField] float smoothTime = 0.2f;

    int x, y, z, offsetX, offsetY, offsetZ;

    private Vector3 offset;
    private Vector3 currentvelocity;
    private Quaternion originalRotation;

    private void Start()
    {
        offset = transform.position - target.position;
        x = followX ? 1 : 0;
        y = followY ? 1 : 0;
        z = followZ ? 1 : 0;
        offsetX = keepOffsetX ? 1 : 0;
        offsetY = keepOffsetY ? 1 : 0;
        offsetZ = keepOffsetZ ? 1 : 0;

        currentvelocity = new Vector3();

        originalRotation = transform.rotation;
    }
    // Update is called once per frame
    void Update()
    {
        if (!followRotation)
        {
            transform.rotation = originalRotation;
        }
        if(followFloored)
        {
            transform.position = new Vector3(Mathf.Floor(target.position.x * x + offset.x * offsetX) + 0.5f,
                                         Mathf.Floor(target.position.y * y + offset.y * offsetY) + 0.5f,
                                         Mathf.Floor(target.position.z * z + offset.z * offsetZ) + 0.5f);
        }
        else if(followSmooth)
        {
            
            transform.position = Vector3.SmoothDamp(transform.position, 
                new Vector3(target.position.x * x + offset.x * offsetX, target.position.y * y + offset.y * offsetY, target.position.z * z + offset.z * offsetZ),
                ref currentvelocity, smoothTime);
        }
        else
        {
            transform.position = new Vector3(target.position.x * x + offset.x * offsetX, target.position.y * y + offset.y * offsetY, target.position.z * z + offset.z * offsetZ);
        }
    }
}
