using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPosition : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] bool followX;
    [SerializeField] bool followY;
    [SerializeField] bool followZ;
    [SerializeField] bool followFloored;
    [SerializeField] bool followSmooth;
    [SerializeField] float smoothTime = 0.2f;

    int x, y, z;

    private Vector3 offset;
    private Vector3 currentvelocity;

    private void Start()
    {
        offset = transform.position - target.position;
        x = followX ? 1  :0;
        y = followY ? 1 : 0;
        z = followZ ? 1 : 0;

        currentvelocity = new Vector3();
    }
    // Update is called once per frame
    void Update()
    {
        if(followFloored)
        {
            transform.position = new Vector3(Mathf.Floor(target.position.x * x) + 0.5f,
                                         Mathf.Floor(target.position.y * y) + 0.5f,
                                         Mathf.Floor(target.position.z * z) + 0.5f) + offset;
        }
        else if(followSmooth)
        {
            
            transform.position = Vector3.SmoothDamp(transform.position, new Vector3(target.position.x * x, target.position.y * y, target.position.z * z) + offset, ref currentvelocity, smoothTime);
        }
        else
        {
            transform.position = new Vector3(target.position.x * x, target.position.y * y, target.position.z * z) + offset;
        }
    }
}
