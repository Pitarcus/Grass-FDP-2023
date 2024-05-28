using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CinemachineMenuManager : MonoBehaviour
{
    // references
    [SerializeField] private CinemachineVirtualCamera[] cameras;

    public int _currentCameraIndex;

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

        SwitchToCamera(_currentCameraIndex);
    }

    public void SwitchToCamera(int cameraIndex)
    {
        cameras[_currentCameraIndex].Priority = 0;
        cameras[cameraIndex].Priority = 10;
        _currentCameraIndex = cameraIndex;
    }

}
