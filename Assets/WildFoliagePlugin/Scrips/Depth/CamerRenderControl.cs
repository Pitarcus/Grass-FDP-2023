using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class CameraRenderControl : MonoBehaviour
{
    public Texture _camDepthTexture;

    [SerializeField] private Material blurMaterial;
    private RenderTexture _temporaryRenderTexture;

    public List<GameObject> thingsToHide = new List<GameObject>();
    private List<GameObject> hiddenThings = new List<GameObject>();

    [Obsolete]
    public void PreRender(ScriptableRenderContext _context, Camera _camera)
    {
        // Do stuff here before the render, i.e. you could hide things specifically from this camera
        foreach (GameObject _thingToHide in thingsToHide)
        {
            _thingToHide.SetActive(false);
            hiddenThings.Add(_thingToHide);
        }

        _temporaryRenderTexture = RenderTexture.GetTemporary(Screen.width, Screen.height);
    }

    public void PostRender(ScriptableRenderContext _context, Camera _camera)
    {
        // Get Camera depth texture (Must be rendering to a used display OR a render texture)
        _camDepthTexture = Shader.GetGlobalTexture("_CameraDepthTexture");

        //Graphics.Blit(_camDepthTexture, _temporaryRenderTexture, blurMaterial);

        Shader.SetGlobalTexture("_WorldRenderTextureDepth", _camDepthTexture);

        RenderTexture.ReleaseTemporary(_temporaryRenderTexture);

        // Reactivate the hidden things after the render
        foreach (GameObject _hiddenThing in hiddenThings)
        {
            _hiddenThing.SetActive(true);
        }
        hiddenThings.Clear();
    }
}
