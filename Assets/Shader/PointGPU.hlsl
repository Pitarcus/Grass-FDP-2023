#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)

	struct GrassData
	{
		float3 position;
		float3 scale;
	};
	StructuredBuffer<GrassData> _GrassData;
#endif

void ConfigureProcedural () {
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		float3 position = _GrassData[unity_InstanceID].position;
		float3 scale = _GrassData[unity_InstanceID].scale;

		unity_ObjectToWorld = 0.0;
		unity_ObjectToWorld._m03_m13_m23_m33 = float4(position, 1.0);
		unity_ObjectToWorld._m00_m11_m22 = scale;
	#endif
}

void ShaderGraphFunction_float (float3 In, out float3 Out) {
	Out = In;
}

void ShaderGraphFunction_half (half3 In, out half3 Out) {
	Out = In;
}