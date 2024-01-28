
using UnityEngine;
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/GrassMaterialParameters", order = 1)]
public class GrassMaterialParameters_SO : ScriptableObject
{
    // Grass Material parameters
    [Header("Grass Material parameters")]
    [SerializeField] public Color bottomColor;
    [SerializeField] public Color topColor;
    [SerializeField] public Color tipColor;
    [SerializeField] public Color SSSColor;
    [SerializeField] public float smoothness = 0.65f;
    [SerializeField] public float ao = 0.7f;

    [SerializeField] public float worldUVTiling = 1;
    [Header("Scale")]
    [SerializeField] public float scaleY = 1;
    [SerializeField] public float randomYScaleNoise = 1;
    [SerializeField] public float minRandomY = 0;
    [SerializeField] public float maxRandomY = 1;
    [Header("Rotation")]
    [SerializeField][Range(0, 360)] public float maxYRotation = 0;
    [SerializeField] public float randomYRotationNoise = 1;
    [SerializeField][Range(0, 90)] public float maxBend = 20;
    [SerializeField] public bool randomBend = false;
    [SerializeField][Range(0, 90)] public float maxAdditionalBend = 20;
    [SerializeField] public float bendRandomnessScale = 1;
    [Header("Static Wind")]
    [SerializeField] public float baseWindDisplacement = 0f;
    [SerializeField] public float baseWindYDisplacement = 0f;
    [SerializeField] public float staticWindYMultiplier = -0.2f;
    [SerializeField] public float staticWindXZMultiplier = 1f;
    [SerializeField] public float staticWindBladeHashIntensity = 1.5f;

    [Header("Dynamic Wind")]
    [SerializeField] public float dynamicWindStrength = 1.2f;//
    [SerializeField] public float dynamicWindNoiseStrength = 1f;//

    [Header("Player interaction")]
    [SerializeField] public float playerPositionModifierX = 1f;
    [SerializeField] public float playerPositionModifierY = 1f;
    [SerializeField] public float playerPositionModifierZ = 1f;
}


