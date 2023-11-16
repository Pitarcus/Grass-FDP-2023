using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class StaticWindMaster : MonoBehaviour
{
    [SerializeField][Range(0, 3)] float windStrength = 1;
    [SerializeField] float windSpeed = 1;
    [SerializeField][Range(0, 360)] float windRotation = 0;
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
    }

    private void OnValidate()
    {
        transform.rotation = Quaternion.AngleAxis(windRotation.Remap(0, 360, -90, 270), Vector3.up);
        if(_arrowMeshMaterial != null)
            _arrowMeshMaterial.SetColor(arrowMeshMaterialColorId, windArrowColorGradient.Evaluate(windSpeed * windStrength / 3f));

        UpdateGlobalVariables();
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
