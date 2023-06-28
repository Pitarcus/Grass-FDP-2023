

void PixelSnapToWorld_float2 (float2 position, float pixelWorldSize, out float2 Out) {
	Out = (floor(position / pixelWorldSize) + 0.5f) * pixelWorldSize;
}
