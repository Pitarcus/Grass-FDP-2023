#ifndef MAINLIGHT_INCLUDED
#define MAINLIGHT_INCLUDED

void GetMainLightData_float(float3 WorldPos, out half3 direction, out half3 color, out half distanceAttenuation, out half shadowAttenuation)
{
#ifdef SHADERGRAPH_PREVIEW
    // In Shader Graph Preview we will assume a default light direction and white color
    direction = half3(-0.3, -0.8, 0.6);
    color = half3(1, 1, 1);
    distanceAttenuation = 1.0;
    shadowAttenuation = 1.0;
#else
    // GetMainLight defined in Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl
    half4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
    Light mainLight = GetMainLight(shadowCoord);
    direction = -mainLight.direction;
    color = mainLight.color;
    distanceAttenuation = mainLight.distanceAttenuation;
    shadowAttenuation = mainLight.shadowAttenuation;
#endif

}

#endif