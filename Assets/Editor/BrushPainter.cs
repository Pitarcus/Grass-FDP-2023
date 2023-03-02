using UnityEditor;
using UnityEngine;

public class BrushPainter : EditorWindow
{
    private Texture2D maskTexture;
    private Texture2D brushTexture;
    private Terrain terrain;
    private TerrainData terrainData;
    private int alphamapWidth;
    private int alphamapHeight;
    private bool isPainting = false;
    private Vector2 brushPosition;
    private Vector2 brushSize;
    //private float[,] alphamapData;

    [MenuItem("Window/Mask Painter")]
    private static void Init()
    {
        BrushPainter window = (BrushPainter)EditorWindow.GetWindow(typeof(BrushPainter));
        window.Show();
    }

    private void OnGUI()
    {
        maskTexture = (Texture2D)EditorGUILayout.ObjectField("Mask Texture", maskTexture, typeof(Texture2D), false);
        brushTexture = (Texture2D)EditorGUILayout.ObjectField("Brush Texture", brushTexture, typeof(Texture2D), false);
        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);

        if (GUILayout.Button("Initialize"))
        {
            Initialize();
        }

        if (brushTexture == null || terrain == null)
        {
            EditorGUILayout.HelpBox("Please assign a brush texture, and terrain.", MessageType.Info);
        }
        else if (terrainData == null)
        {
            EditorGUILayout.HelpBox("Please click the Initialize button to load the terrain data.", MessageType.Info);
        }
        else
        {
            DrawBrush();
            PaintMask();
        }
    }

    private void Initialize()
    {
        terrainData = terrain.terrainData;
        alphamapWidth = terrainData.alphamapWidth;
        alphamapHeight = terrainData.alphamapHeight;

        // Create the new texture and set its pixels to transparent
        maskTexture = new Texture2D(alphamapWidth, alphamapHeight, TextureFormat.RGBA32, false);
        maskTexture.wrapMode = TextureWrapMode.Clamp;
        maskTexture.filterMode = FilterMode.Point;
        Color[] colors = new Color[alphamapWidth * alphamapHeight];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.clear;
        }
        maskTexture.SetPixels(colors);
        maskTexture.Apply();
    }

    private void DrawBrush()
    {
        if (brushTexture != null)
        {
            float size = EditorGUIUtility.currentViewWidth / 8;
            Rect rect = new Rect(Event.current.mousePosition.x - size / 2, Event.current.mousePosition.y - size / 2, size, size);
            GUI.DrawTexture(rect, brushTexture);
        }
    }

    private void PaintMask()
    {
        if (maskTexture == null || brushTexture == null || terrain == null || terrainData == null)
        {
            return;
        }

        Vector2 mousePosition = Event.current.mousePosition;
        if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                // Convert the terrain position to a position on the screen
                Vector2 terrainPosition = HandleUtility.WorldToGUIPoint(hit.point);

                // Determine the brush size based on the distance from the camera
                float distance = Vector3.Distance(Camera.current.transform.position, terrain.transform.position);
                brushSize = new Vector2(distance, distance);

                // Get the pixels from the brush texture
                Color[] brushPixels = brushTexture.GetPixels();

                // Update the mask texture
                Color[] colors = maskTexture.GetPixels();

                for (int i = 0; i < brushPixels.Length; i++)
                {
                    // Convert the index to an x, y position
                    int x = i % brushTexture.width;
                    int y = i / brushTexture.width;

                    // Determine the position on the terrain based on the brush size and position
                    int terrainX = Mathf.RoundToInt(terrainPosition.x + x - brushSize.x / 2);
                    int terrainY = Mathf.RoundToInt(terrainPosition.y + y - brushSize.y / 2);

                    if (terrainX >= 0 && terrainX < alphamapWidth && terrainY >= 0 && terrainY < alphamapHeight)
                    {
                        // Determine the strength of the brush
                        float brushStrength = brushPixels[i].a;

                        // Determine the new value of the alphamap
                        //float newValue = colors[x, y] + brushStrength;

                        // Clamp the value between 0 and 1
                        //newValue = Mathf.Clamp01(newValue);

                        // Set the new value of the alphamap
                        //alphamapData[x, y] = newValue;
                        int index = (Mathf.RoundToInt(brushPosition.y) + y) * alphamapWidth + (Mathf.RoundToInt(brushPosition.x) + x);
                        colors[index] = brushTexture.GetPixel(x, y) * new Color(1, 1, 1, 1);
                    }
                }

                maskTexture.SetPixels(colors);
                maskTexture.Apply();
            }

            isPainting = true;
            brushPosition = mousePosition;
        }
        else if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
        {
            isPainting = false;
        }

        if (isPainting)
        {
            // Update the brush position
            brushPosition += Event.current.delta;

            // Repaint the editor window
            Repaint();
        }

        GUILayout.Label("Mask Preview");
        Rect previewRect = GUILayoutUtility.GetRect(alphamapWidth, alphamapHeight);
        GUI.DrawTexture(previewRect, maskTexture, ScaleMode.ScaleToFit);
    }
}
