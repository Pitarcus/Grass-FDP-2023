#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;

public enum BrushMode
{
    PaintGrass,
    EraseGrass,
    PaintSmallVegetation
}

[RequireComponent(typeof(HeightmapHolder))]
[ExecuteInEditMode]
public class TerrainPainterComponent : MonoBehaviour
{
    // Initial settings for the size
    [SerializeField] public int alphamapWidth;
    [SerializeField] public int alphamapHeight;
    [SerializeField] public Vector3 terrainDimensions;
    [SerializeField] private bool isUnityTerrain = false;

    // brush settings - for editor
    [SerializeField] private LayerMask hitMask = 1;

    [SerializeField] private Texture2D brushTexture;
    [SerializeField] private BrushMode brushMode;
    [SerializeField] public float brushSize = 50f;
    [SerializeField][Range(0, 1)] private float brushStrength = 1.0f;


    // other properties
    public Texture2D maskTexture;
    [SerializeField] public Texture2D realMaskTexture;
    public Texture2D heightMap;
    //public Terrain terrain { get; private set; }
    //public TerrainData terrainData { get; private set; }

    private bool isPainting = false;
    private Vector2 brushPosition;


    // raycast vars
    public Vector3 hitPosGizmo { get; private set; }
    Vector3 mousePos;
    public Vector3 hitNormal { get; private set; }

    public UnityEvent onInitFinished;   // Used to set up displayer
    public UnityEvent onPaintingMask;

    private Vector2 lastAlphamapSize;

    private void Awake()
    {
        // Load texture in the game
        realMaskTexture = Resources.Load<Texture2D>("GrassPositions/" + transform.parent.name + "_grassPlacementInfo");
    }

    private void OnValidate()
    {
        Init();
    }

    // Create the texture and get terrain data
    public void Init()
    {

        heightMap = GetComponent<HeightmapHolder>().heightMap;

#if UNITY_EDITOR
        if (lastAlphamapSize.x != alphamapWidth || lastAlphamapSize.y != alphamapHeight)
        {
            maskTexture = new Texture2D(alphamapWidth, alphamapHeight, TextureFormat.RGBA32, false);
            maskTexture.wrapMode = TextureWrapMode.Clamp;
            maskTexture.filterMode = FilterMode.Bilinear;
            ClearMask();
            //ResetMaskTextureToAsset();

            lastAlphamapSize.x = alphamapWidth;
            lastAlphamapSize.y = alphamapHeight;
        }
#endif
        // set the texture used in the decal to the saved texture
        if(realMaskTexture != null)
        {
            maskTexture = realMaskTexture;
        }

        if (isUnityTerrain)
            transform.localPosition = new Vector3(terrainDimensions.x / 2, 0, terrainDimensions.z / 2);
        else
            transform.localPosition = new Vector3(0, 0, 0);

        onInitFinished.Invoke();

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

            Vector2 inTerrainPosition;
            if (isUnityTerrain)
            {
                // Convert the position to the alphamap coordinates
                inTerrainPosition = new Vector2(localPosition.x / terrainDimensions.x * alphamapWidth, localPosition.z / terrainDimensions.z * alphamapHeight);
                //inTerrainPosition -= new Vector2(alphamapWidth / 4, alphamapHeight / 4);
            }
            else
            {
                // Convert the position to the alphamap coordinates
                inTerrainPosition = new Vector2(localPosition.x / terrainDimensions.x * alphamapWidth, localPosition.z / terrainDimensions.z * alphamapHeight);
                //inTerrainPosition -= new Vector2(alphamapWidth / 4, alphamapHeight / 4);
            }
           
            if (inTerrainPosition.x >= -alphamapWidth && inTerrainPosition.x < alphamapWidth
                && inTerrainPosition.y >= -alphamapHeight && inTerrainPosition.y < alphamapHeight)  // The position is inside the bounds of the terrain
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
                        if (x >= -alphamapWidth && x < alphamapWidth
                            && y >= -alphamapHeight && y < alphamapHeight)
                        {
                            // Mask texture space
                            int u = x + alphamapWidth / 2;
                            int v = y + alphamapHeight / 2;

                            //float distance = Vector2.Distance(new Vector2(x, y), brushPosition);

                            float maskValue = maskTexture.GetPixel(u, v).a;
                            float brushValue = brushTexture.GetPixelBilinear((x - startX) / (float)brushSizeX, (y - startY) / (float)brushSizeY).a;

                            /* Apply a smoothstep function to the distance to create a feathered edge
                            float falloff = Mathf.SmoothStep(0f, 1f, distance);
                            Debug.Log(falloff);*/

                            // ---- ACTUAL PAINTING ------

                            if (brushMode == BrushMode.PaintGrass)
                            {
                                maskValue = Mathf.Lerp(maskValue, Mathf.Max(brushValue, maskValue), brushStrength);
                            }
                            else if (brushMode == BrushMode.EraseGrass)
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

    private bool AssignRealMaskAsset()
    {
        realMaskTexture = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets" + "/WildFoliagePlugin/Textures/Resources/GrassPositions/" + transform.parent.name + "_grassPlacementInfo.png", typeof(Texture2D));
        return realMaskTexture != null;
    }

    // Copy the asset saved texture into the mask texture used in the component
    public void ResetMaskTextureToAsset()
    {
        if (realMaskTexture != null)
        {
            if(realMaskTexture.width != maskTexture.width)
            {
                alphamapHeight = realMaskTexture.height;
                alphamapWidth = realMaskTexture.width;
                maskTexture = new Texture2D(alphamapWidth, alphamapHeight, TextureFormat.RGBA32, false);
                maskTexture.wrapMode = TextureWrapMode.Clamp;
                maskTexture.filterMode = FilterMode.Bilinear;
            }
            Graphics.CopyTexture(realMaskTexture, maskTexture);
        }
        else if (AssignRealMaskAsset())
        {
            alphamapHeight = realMaskTexture.height;
            alphamapWidth = realMaskTexture.width;
            maskTexture = new Texture2D(alphamapWidth, alphamapHeight, TextureFormat.RGBA32, false);
            maskTexture.wrapMode = TextureWrapMode.Clamp;
            maskTexture.filterMode = FilterMode.Bilinear;
            Graphics.CopyTexture(realMaskTexture, maskTexture);
        }
    }

    /// <summary>
    /// Serialize the current mask texture to be inside the project folder.
    /// This way the texture is always stored and can be modified/saved in different versions.
    /// </summary>
    public void SaveTexture()
    {
        Texture2D auxtex = maskTexture;

        var dirPath = Application.dataPath + "/WildFoliagePlugin/Textures/Resources/GrassPositions/";
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        string grassMaskPath = dirPath + transform.parent.name + "_grassPlacementInfo.png";

        File.WriteAllBytes(grassMaskPath, auxtex.EncodeToPNG());
        AssetDatabase.Refresh();

        AssignRealMaskAsset();

        string assetPath = AssetDatabase.GetAssetPath(realMaskTexture);
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            Debug.Log("Changing importer options");
            TextureImporterPlatformSettings settings = importer.GetDefaultPlatformTextureSettings();
            settings.format = TextureImporterFormat.RGBA32;

            importer.SetPlatformTextureSettings(settings);
            importer.isReadable = true; // Set the "isReadable" property to true
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;


            // Apply the modified import settings
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
        }
    }


#endif
    public void ClearMask()
    {
        for (int y = -alphamapHeight; y < alphamapHeight; y++)
        {
            for (int x = -alphamapWidth; x < alphamapWidth; x++)
            {
                maskTexture.SetPixel(x, y, new Color(0, 0, 0, 0));
            }
        }
        maskTexture.Apply();
    }


    public Texture2D GetMaskTexture()
    {
        return realMaskTexture;
    }

    public Texture2D GetMaskDisplayTexture()
    {
        return maskTexture;
    }

    public Texture2D GetHeightMap()
    {
        return heightMap;
    }
}