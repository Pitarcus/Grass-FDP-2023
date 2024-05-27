using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName ="Lighting Preset", menuName ="Scriptables/Lighing Preset", order = 1)]
public class LightingConditions : ScriptableObject
{
    public Gradient AmbientColor;
    public Gradient DirectionalColor;
    public Gradient FogColor;

    [Tooltip("Use the gradients on the whole day/night cycle. Uncheck if you want to set the gradient to night to day cycle")]
    [SerializeField] public bool isFullDayGradient = false;
}
