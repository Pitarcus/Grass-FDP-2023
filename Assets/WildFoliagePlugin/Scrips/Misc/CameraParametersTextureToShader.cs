using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraParametersTextureToShader : MonoBehaviour
{
    Camera _worldRenderTextureCamera;
    [SerializeField] RenderTexture _colorTexture;
    private void OnEnable()
    {
        _worldRenderTextureCamera = GetComponent<Camera>();
        int width = 1024;
        int height = 1024;

        if(_colorTexture == null)
            _colorTexture = new RenderTexture(width, height, 0, RenderTextureFormat.Default);
      

       _worldRenderTextureCamera.targetTexture = _colorTexture;

        Shader.SetGlobalTexture("_PositionRT", _colorTexture);

        Shader.SetGlobalFloat("_WorldRTCameraSize", _worldRenderTextureCamera.orthographicSize * 2);
  
    }

    private void Update()
    {
        Shader.SetGlobalVector("_WorldRTCameraPos", _worldRenderTextureCamera.transform.position);
    }
}
