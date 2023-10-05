using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class StaticWindMaster : MonoBehaviour
{
    [SerializeField] float windStrength = 1;
    [SerializeField] float windSpeed = 1;
    [SerializeField][Range(0, 360)] float windRotation = 0;
    [SerializeField] float windNoiseScale = 1;
    [SerializeField] float windDistortion = 0;

    static readonly int
        windStrengthId = Shader.PropertyToID("_WindStrenght"),
        windSpeedId = Shader.PropertyToID("_WindSpeed"),
        windRotationId = Shader.PropertyToID("_WindRotation"),
        windNoiseScaleId = Shader.PropertyToID("_WindNoiseScale"),
        windDistortionId = Shader.PropertyToID("_WindDistortion")
        ;

    // Update is called once per frame
    void Update()
    {
        Shader.SetGlobalFloat(windStrengthId, windStrength);
        Shader.SetGlobalFloat(windSpeedId, windSpeed);
        Shader.SetGlobalFloat(windRotationId, windRotation);
        Shader.SetGlobalFloat(windDistortionId, windDistortion);
        Shader.SetGlobalFloat(windNoiseScaleId, windNoiseScale);
    }
}
