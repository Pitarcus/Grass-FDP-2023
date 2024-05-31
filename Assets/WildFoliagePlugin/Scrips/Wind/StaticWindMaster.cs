using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class should be added to the arrow mesh so that the wind can be properly displayed.
/// It controls the shader global properties in order to controll all shaders in the scene.
/// </summary>
[ExecuteAlways]
public class StaticWindMaster : MonoBehaviour
{
    // Parameters
    [SerializeField][Range(0, 3)] float windStrength = 1;
    public float WindStrength { get { return windStrength; } 
        set { windStrength = value; UpdateGlobalVariables(); UpdateWindArrow(); } }

    [SerializeField] float windSpeed = 1;
    public float WindSpeed { get { return windSpeed; } 
        set { windSpeed = value; UpdateGlobalVariables(); UpdateWindArrow(); } }

    [SerializeField][Range(0, 360)] float windRotation = 0;
    public float WindRotation { get { return windRotation; } 
        set { windRotation = value; UpdateGlobalVariables(); UpdateWindArrow(); } }

    [SerializeField] float windNoiseScale = 1;
    [SerializeField] float windDistortion = 0;
    [SerializeField] [GradientUsage(true)] Gradient windArrowColorGradient;

    private Material _arrowMeshMaterial;

    static readonly int
        arrowMeshMaterialColorId = Shader.PropertyToID("_WindArrowColor"),

        windStrengthId = Shader.PropertyToID("_WindStrength"),
        windSpeedId = Shader.PropertyToID("_WindSpeed"),
        windRotationId = Shader.PropertyToID("_WindRotation"),
        windNoiseScaleId = Shader.PropertyToID("_WindNoiseScale"),
        windDistortionId = Shader.PropertyToID("_WindDistortion")
        ;

    private void OnEnable()
    {
        _arrowMeshMaterial = GetComponent<MeshRenderer>().sharedMaterial;

        UpdateWindArrow();
        UpdateGlobalVariables();
    }

    private void OnValidate()
    {

        UpdateWindArrow();
        UpdateGlobalVariables();
    }

    private void UpdateWindArrow()
    {
        transform.rotation = Quaternion.AngleAxis(windRotation.Remap(0, 360, -90, 270), Vector3.up);
        if (_arrowMeshMaterial != null)
            _arrowMeshMaterial.SetColor(arrowMeshMaterialColorId, windArrowColorGradient.Evaluate(windSpeed * windStrength / 3f));
    }
   
    void UpdateGlobalVariables()
    {
        Shader.SetGlobalFloat(windStrengthId, windStrength);
        Shader.SetGlobalFloat(windSpeedId, windSpeed);
        Shader.SetGlobalFloat(windRotationId, windRotation);
        Shader.SetGlobalFloat(windDistortionId, windDistortion);
        Shader.SetGlobalFloat(windNoiseScaleId, windNoiseScale);
    }
    

}
