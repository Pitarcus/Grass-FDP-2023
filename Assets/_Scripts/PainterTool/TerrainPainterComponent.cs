#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;

public enum BrushMode
{
    Paint,
    Erase
}

[ExecuteInEditMode]
public class TerrainPainterComponent : MonoBehaviour
{
    // brush settings
    public LayerMask hitMask = 1;
    public Texture2D brushTexture;
    public BrushMode brushMode;
    public Vector2Int brushSize = new Vector2Int(50, 50);
    public float brushStrength = 1.0f;
    public Texture2D maskTexture;

    public Terrain terrain; // The terrain is needed to set up the size of the texture correctly.

    public Texture2D heightMap;

    [HideInInspector] public TerrainData terrainData;
    [HideInInspector] public bool isPainting = false;
    [HideInInspector] public Vector2 brushPosition;

    // raycast vars
    [HideInInspector]
    public Vector3 hitPosGizmo;
    Vector3 mousePos;
    public Vector3 hitPoint;
    [HideInInspector]
    public Vector3 hitNormal;

    public UnityEvent onInitFinished;
    public UnityEvent onPaintingMask;

    private void OnValidate()
    {
        if(terrain == null)
            terrain = (Terrain)transform.GetComponentInParent(typeof(Terrain));
        Init();
    }

    // Create the texture and get terrain data
    public void Init()
    {
        if (terrain != null)
        {
            terrainData = terrain.terrainData;

            heightMap = transform.parent.GetComponent<HeightmapHolder>().heighMap;

            int alphamapWidth = terrainData.alphamapWidth;
            int alphamapHeight = terrainData.alphamapHeight;

            if (maskTexture == null)
            {
                maskTexture = new Texture2D(alphamapWidth, alphamapHeight, TextureFormat.RGBA32, false);
                maskTexture.wrapMode = TextureWrapMode.Clamp;
                maskTexture.filterMode = FilterMode.Bilinear;
                ClearMask();
            }

            transform.localPosition = new Vector3(terrainData.size.x / 2, 0, terrainData.size.z / 2);

            onInitFinished.Invoke();
        }
    }
#if UNITY_EDITOR
    void OnEnable()
    {
        // Remove delegate listener if it has previously
        // been assigned.
        if (gameObject == null)
        {
            return;
        }

        // Add (or re-add) the delegate.
        SceneView.duringSceneGui += this.OnScene;

    }

    private void OnDestroy()
    {
        SceneView.duringSceneGui -= this.OnScene;
    }

    public void PaintMask(SceneView scene, Vector3 mousePos)
    {
        if (isPainting)
        {
            // Get the mouse position in the Scene view
            Ray ray = scene.camera.ScreenPointToRay(mousePos);
            float height = transform.position.y;

            // Raycast to the terrain to get the height at the mouse position
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, hitMask.value))
            {
                height = hit.point.y;
                hitNormal = hit.normal;
            }
            
            // Convert the position to the terrain's local coordinates
            Vector3 localPosition = transform.InverseTransformPoint(ray.origin + ray.direction * (height - ray.origin.y) / ray.direction.y);

            // Convert the position to the alphamap coordinates
            Vector2 inTerrainPosition = new Vector2(localPosition.x / terrainData.size.x * terrainData.alphamapWidth, localPosition.z / terrainData.size.z * terrainData.alphamapHeight);

            if (inTerrainPosition.x >= -terrainData.alphamapWidth && inTerrainPosition.x < terrainData.alphamapWidth 
                && inTerrainPosition.y >= -terrainData.alphamapHeight && inTerrainPosition.y < terrainData.alphamapHeight)  // The position is inside the bounds of the terrain
            {
                // Update the brush position
                brushPosition = inTerrainPosition;

                // Paint the mask texture
                int brushSizeX = Mathf.RoundToInt(brushSize.x);
                int brushSizeY = Mathf.RoundToInt(brushSize.y);
                int startX = Mathf.RoundToInt(brushPosition.x - brushSizeX / 2);
                int startY = Mathf.RoundToInt(brushPosition.y - brushSizeY / 2);

                for (int y = startY; y < startY + brushSizeY; y++) // Loop through the size of the brush
                {
                    for (int x = startX; x < startX + brushSizeX; x++)
                    {
                        if (x >= -terrainData.alphamapWidth && x < terrainData.alphamapWidth 
                            && y >= -terrainData.alphamapHeight && y < terrainData.alphamapHeight)
                        {
                            // Mask texture space
                            int u = x + terrainData.alphamapWidth / 2;
                            int v = y + terrainData.alphamapHeight / 2;

                            //float distance = Vector2.Distance(new Vector2(x, y), brushPosition);
                           
                            float maskValue = maskTexture.GetPixel(u, v).a;
                            float brushValue = brushTexture.GetPixelBilinear((x - startX) / (float)brushSizeX, (y - startY) / (float)brushSizeY).a;

                            /* Apply a smoothstep function to the distance to create a feathered edge
                            float falloff = Mathf.SmoothStep(0f, 1f, distance);
                            Debug.Log(falloff);*/

                            // ---- ACTUAL PAINTING ------

                            if (brushMode == BrushMode.Paint)
                            {
                                maskValue = Mathf.Lerp(maskValue, Mathf.Max(brushValue, maskValue), brushStrength);
                            }
                            else
                            {
                                maskValue = Mathf.Lerp(maskValue, maskValue - brushValue, brushStrength);
                            }

                            maskTexture.SetPixel(u, v, new Color(0, 0, 0, maskValue));
                            
                        }
                    }
                }
                maskTexture.Apply();
            }

        }
    }

    private void OnDisable()
    {
        isPainting = false;

        SceneView.duringSceneGui -= this.OnScene;

    }

    private void OnScene(SceneView scene)
    {
        if (!(Selection.Contains(gameObject)))
        { return; }

        // Handle mouse events
        Event currentEvent = Event.current;
        mousePos = currentEvent.mousePosition;
        float ppp = EditorGUIUtility.pixelsPerPoint;
        mousePos.y = scene.camera.pixelHeight - mousePos.y * ppp;
        mousePos.x *= ppp;
        mousePos.z = 0;

        // ray for gizmo(disc)
        Ray rayGizmo = scene.camera.ScreenPointToRay(mousePos);
        RaycastHit hitGizmo;

        if (Physics.Raycast(rayGizmo, out hitGizmo, 200f, hitMask.value))
        {
            hitPosGizmo = hitGizmo.point;
        }

        // Evaluate mouse event
        switch (currentEvent.type)
        {
            case EventType.MouseDrag:
                if (currentEvent.button == 1)
                {
                    isPainting = true;
                    PaintMask(scene, mousePos);
                    Undo.RecordObject(this, "Paint Mask");
                    currentEvent.Use();
                }
                break;
            case EventType.MouseUp:
                if (currentEvent.button == 1)
                {
                    isPainting = false;
                    onPaintingMask.Invoke();
                    currentEvent.Use();
                }
                break;
        }
    }
#endif

    public void ClearMask()
    {
        for (int y = -terrainData.alphamapHeight; y < terrainData.alphamapHeight; y++)
        {
            for (int x = -terrainData.alphamapWidth; x < terrainData.alphamapWidth; x++)
            {
                maskTexture.SetPixel(x, y, new Color(0, 0, 0, 0));
            }
        }
        maskTexture.Apply();
    }

}