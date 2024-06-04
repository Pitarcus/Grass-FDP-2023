using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class CinemachineMenuManager : MonoBehaviour
{
    // references
    [SerializeField] private CinemachineVirtualCamera[] cameras;

    private int _currentCameraIndex;

    [Range(0, 20)]
    [SerializeField] int _cameraIndex;

    private static CinemachineMenuManager _instance;
    public static CinemachineMenuManager Instance {  get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    private void Start()
    {
        SwitchToCamera(_currentCameraIndex);
    }

    public void SwitchToCamera(int cameraIndex)
    {
        cameras[_currentCameraIndex].Priority = 0;
        cameras[cameraIndex].Priority = 10;
        _currentCameraIndex = cameraIndex;
    }

    private void OnValidate()
    {
        if(cameras!= null)
        {
            if(_cameraIndex < cameras.Length && _cameraIndex >= 0)
            {
                SwitchToCamera(_cameraIndex);
            }
        }
    }

}
