using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
[ExecuteAlways]
public class LightingManager : MonoBehaviour
{
    [Header("References")]
    public Light directionalLight;
    public LightingConditions preset;

    [Header("Parameters")]
   
    [SerializeField] private float dayPeriod = 24;
    [SerializeField] private bool runCycle = true;
    [SerializeField] private bool rotateAllDirections = false;
    [SerializeField] [Range(-360, 360)] private float yRotation = -100f;
    [SerializeField] [Range(0, 1)] private float timeOfDayNormalized;

    // Private memebers
    
    private float _realTimeOfDay;


    private void Start()
    {
        _realTimeOfDay = timeOfDayNormalized * dayPeriod;
    }
    

    private void Update()
    {
        if (preset == null)
            return;

        
        if(Application.isPlaying)
        {
            if (!runCycle)
                return;

            _realTimeOfDay += Time.deltaTime;
            _realTimeOfDay %= dayPeriod;
            UpdateLighting(_realTimeOfDay / dayPeriod);
        }
        else
        {
            UpdateLighting(_realTimeOfDay / dayPeriod);
        }
    }

    public void StartCycle()
    {
        runCycle = true;
    }

    public void StopCycle()
    {
        runCycle = false;
    }

    /// <summary>
    /// Get the time of day from 0 to 1. 0 when the sun is pointing up, and 1 when the sun is pointing down.
    /// </summary>
    /// <returns></returns>
    public float GetTimeOfDay()
    {
        float time = _realTimeOfDay / dayPeriod;
        return time <= 0.5f ? time * 2 : (1 - time) * 2;
    }

    private void UpdateLighting(float timePercent)
    {
        float halfTime;
        if (!preset.isFullDayGradient) 
        {
            halfTime = timePercent <= 0.5f ? timePercent * 2 : (1 - timePercent) * 2; // Use half of the gradient and invert it halfway
        }
        else
        {
            halfTime = timePercent;
        }
        
        RenderSettings.ambientLight = preset.AmbientColor.Evaluate(halfTime);
        RenderSettings.fogColor = preset.FogColor.Evaluate(halfTime);

        if(directionalLight != null)
        {
            directionalLight.color = preset.DirectionalColor.Evaluate(halfTime);
            if(!rotateAllDirections)
                directionalLight.transform.localRotation = Quaternion.Euler(new Vector3((timePercent * 360f) - 90f, yRotation, 0));
            else
                directionalLight.transform.localRotation = Quaternion.Euler(new Vector3((timePercent * 360f) + 55f, (timePercent * 2 * 360f) - 45f, 0));
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) // Only set the _realTimeOfDay to the set time of day in the editor when not playing
            _realTimeOfDay = timeOfDayNormalized * dayPeriod;

        if (directionalLight != null)
        {
            return;
        }
        if(RenderSettings.sun != null)
        {
            directionalLight = RenderSettings.sun;
        }
        else
        {
            Light[] lights = FindObjectsOfType<Light>();
            foreach(Light light in lights)
            {
                if(light.type == LightType.Directional)
                {
                    directionalLight = light;
                    return;
                }
            }
        }
    }

}
