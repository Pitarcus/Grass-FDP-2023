void NormalFromTexture_float (Texture2D _texture, SamplerState SS, float2 _UV, float _offset, float strength, out float3 out) 
{
	float sample = tex2D(_texture, SS, _UV).r;
	float sampleX = tex2D(_texture, SS, _UV - float2(_offset, 0)).r;
	float sampleY = tex2D(_texture, SS, _UV - float2(0, _offset)).r;

	sampleX -= sample;
	sampleY -= sample;

	sampleX *= strength;
	sampleY *= strength;

	float3 normalized = normalize(float3(sampleX, sampleY, 1));
	out = normalized;
}