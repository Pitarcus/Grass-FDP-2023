#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;

public enum BrushMode
{
    Paint,
    Erase
}

[RequireComponent(typeof(HeightmapHolder))]
[ExecuteInEditMode]
public class TerrainPainterComponent : MonoBehaviour
{
    // brush settings - for editor
    [SerializeField] private LayerMask hitMask = 1;

    [SerializeField] private Texture2D brushTexture;
    [SerializeField] private BrushMode brushMode;
    [SerializeField] public float brushSize = 50f;
    [SerializeField] [Range(0, 1)] private float brushStrength = 1.0f;


    // other properties
    public Texture2D maskTexture;
    public Texture2D heightMap;
    public Terrain terrain { get; private set; } // The terrain is needed to set up the size of the texture correctly.
    public TerrainData terrainData { get; private set; }

    private bool isPainting = false;
    private Vector2 brushPosition;


    // raycast vars
    public Vector3 hitPosGizmo { get; private set; }
    Vector3 mousePos;
    public Vector3 hitNormal { get; private set; }

    public UnityEvent onInitFinished;   // Used to set up displayer
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

            heightMap = GetComponent<HeightmapHolder>().heightMap;

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
        SceneView.beforeSceneGui += this.OnScene;

    }

    private void OnDestroy()
    {
        isPainting = false;
        SceneView.beforeSceneGui -= this.OnScene;
    }
    private void OnDisable()
    {
        isPainting = false;
        SceneView.beforeSceneGui -= this.OnScene;
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
                int brushSizeX = Mathf.RoundToInt(brushSize);
                int brushSizeY = Mathf.RoundToInt(brushSize);
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
            case EventType.MouseDown:
                if (currentEvent.button == 0)
                {
                    // register texture state for the undo system
                    Undo.RegisterCompleteObjectUndo(maskTexture, "Paint grass mask");
                    PrefabUtility.RecordPrefabInstancePropertyModifications(maskTexture);
                    Undo.FlushUndoRecordObjects();

                    isPainting = true;
                    PaintMask(scene, mousePos);
                    Event.current.Use();
                }
                break;
            case EventType.MouseDrag:
                if (currentEvent.button == 0)
                {
                    PaintMask(scene, mousePos);
                    Event.current.Use();
                }
                break;
            case EventType.MouseUp:
                if (currentEvent.button == 0)
                {
                    isPainting = false;
                    onPaintingMask.Invoke();
                    Event.current.Use();
                }
                break;
        }
    }

    /*public void SaveTexture()
    {
        Texture2D auxtex = maskTexture;
        byte[] _bytes = auxtex.EncodeToPNG();

        var dirPath = Application.dataPath + "/Textures/GrassPositions/";
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        grassMaskPath = dirPath + transform.parent.name + "_grassPlacementInfo.png";

        File.WriteAllBytes(grassMaskPath, _bytes);

        grassMaskTextureReal = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Textures/GrassPositions/" + transform.parent.name + "_grassPlacementInfo.png", typeof(Texture2D));

        string assetPath = AssetDatabase.GetAssetPath(grassMaskTextureReal);
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer != null)
        {
            importer.isReadable = true; // Set the "isReadable" property to true

            // Apply the modified import settings
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
        }
    }*/
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

    public Texture2D GetMaskTexture()
    {
        return maskTexture;
    }

    public Texture2D GetHeightMap()
    {
        return heightMap;
    }
}