void NormalFromTexture_float (UnityTexture2D myTexture, SamplerState SS, float2 UV, float _offset, float strength, out float3 normal) 
{
	float _sample = SAMPLE_TEXTURE2D_LOD(myTexture, SS, UV, 0).r;
	float _sampleX = SAMPLE_TEXTURE2D_LOD(myTexture, SS, UV - float2(_offset, 0), 0).r;
	float _sampleY = SAMPLE_TEXTURE2D_LOD(myTexture, SS, UV - float2(0, _offset), 0).r; 

	//float _sampleA = SAMPLE_TEXTURE2D_LOD(myTexture, SS, UV, 0).a;

	_sampleX -= _sample;
	_sampleY -= _sample;

	_sampleX = -(_sampleX);
	_sampleY = -(_sampleY);

	_sampleX *= strength;
	_sampleY *= strength;

	float3 normalized = normalize(float3(_sampleX, _sampleY, 1));
	normal = normalized;
}


