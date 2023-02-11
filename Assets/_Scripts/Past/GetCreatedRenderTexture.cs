using UnityEngine;
using UnityEngine.UI;

public class GetCreatedRenderTexture : MonoBehaviour
{
    public RayTracingMaster rayTracingMaster;

    private RawImage image;
    private void Update()
    {
        image = GetComponent<RawImage>();
        image.texture = rayTracingMaster._target;
    }
}
