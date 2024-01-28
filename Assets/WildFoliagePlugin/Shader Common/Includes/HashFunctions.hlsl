void hash12_float (float2 p, out float result)
{
	float3 p3  = frac(float3(p.xyx) * .1031);
    p3 += dot(p3, p3.yzx + 33.33);
    result = frac((p3.x + p3.y) * p3.z);
}
void hash12_half (float2 p, out float result)
{
	float3 p3  = frac(float3(p.xyx) * .1031);
    p3 += dot(p3, p3.yzx + 33.33);
    result = frac((p3.x + p3.y) * p3.z);
}