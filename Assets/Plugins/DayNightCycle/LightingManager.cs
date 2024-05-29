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
   
    [SerializeField] public float dayPeriod = 24;
    [SerializeField] public bool runCycle = true;
    [SerializeField] private bool invertCycle = true;
    [SerializeField] private bool rotateAllDirections = false;
    [SerializeField] [Range(-360, 360)] private float yRotation = -100f;
    [SerializeField] [Range(0, 1)] public float timeOfDayNormalized;

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


        if (Application.isPlaying)
        {
            if (!runCycle)
            {
                //UpdateLighting(timeOfDayNormalized);
            }
            else 
            { 
                _realTimeOfDay += Time.deltaTime;
                _realTimeOfDay %= dayPeriod;
                timeOfDayNormalized = _realTimeOfDay / dayPeriod;
                UpdateLighting(_realTimeOfDay / dayPeriod);
            }
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
    /// Get the time of day from 0 to 1. Depending on wether the gradient is using the full spectrum or not, the value returned is different.
    /// </summary>
    /// <returns>If full gradient, 0 and 1 is middle night. Else, 0 is middle of the night and 1 is middle of the day ERROR!!!</returns>
    public float GetTimeOfDay(float time)
    {
        float timeFormatted;
        if (!preset.isFullDayGradient)
        {
            timeFormatted = time <= 0.5f ? time * 2 : (1 - time) * 2; // Use half of the gradient and invert it halfway
        }
        else
        {
            timeFormatted = time;
        }
        return timeFormatted;
        // THIS IS WRONG
    }

    public void SetTimeOfDay(float time)
    {
        if (runCycle)
            return;

        timeOfDayNormalized = time;

        _realTimeOfDay = timeOfDayNormalized * dayPeriod;

        UpdateLighting(timeOfDayNormalized);
    }

    public void ChangePeriod(float newPeriod)
    {
        float positionInDay = dayPeriod / _realTimeOfDay;

        dayPeriod = newPeriod;

        _realTimeOfDay = newPeriod / positionInDay;
        timeOfDayNormalized = _realTimeOfDay / newPeriod;

        dayPeriod = newPeriod;

        UpdateLighting(timeOfDayNormalized);
    }

    /// <summary>
    /// Update all necessary components to update the lighting of the day / night cycle.
    /// </summary>
    /// <param name="timePercent"> 0 and 1 are both middle of the night. 0.5 is middle of the day</param>
    private void UpdateLighting(float timePercent)
    {
        float timeFormatted = GetTimeOfDay(timePercent);
        
        RenderSettings.ambientLight = preset.AmbientColor.Evaluate(timeFormatted);
        RenderSettings.fogColor = preset.FogColor.Evaluate(timeFormatted);

        if(directionalLight != null)
        {
            directionalLight.color = preset.DirectionalColor.Evaluate(timeFormatted);
            if (!rotateAllDirections)
            {
                timePercent = invertCycle? -timePercent : timePercent;
                directionalLight.transform.localRotation = Quaternion.Euler(timePercent * 360f -90f, yRotation, 0);
            }
            else
                directionalLight.transform.localRotation = Quaternion.Euler(timePercent * 360f+ 55f, (timePercent * 2 * 360f) - 45f, 0);
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) // Only set the _realTimeOfDay to the set time of day in the editor when not playing
            _realTimeOfDay = timeOfDayNormalized * dayPeriod;

        if (directionalLight != null)
        {
            UpdateLighting(timeOfDayNormalized);
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
