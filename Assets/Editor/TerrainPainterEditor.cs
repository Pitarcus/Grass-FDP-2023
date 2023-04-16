using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(TerrainPainterComponent))]
public class TerrainPainterEditor : Editor
{
    
    private Texture2D brushPreviewTexture;

    private void OnEnable()
    {
        TerrainPainterComponent terrainPainter = (TerrainPainterComponent)target;
        brushPreviewTexture = new Texture2D(256, 256);
        brushPreviewTexture.filterMode = FilterMode.Bilinear;
        brushPreviewTexture.wrapMode = TextureWrapMode.Clamp;
        //brushPreviewTexture.SetPixels(terrainPainter.brushTexture.GetPixels());
        brushPreviewTexture.Apply();
    }

    private void OnDisable()
    {
        DestroyImmediate(brushPreviewTexture);
    }

    public override void OnInspectorGUI()   // Assign and construct the editor window and its appearance. Should be done always
    {
        TerrainPainterComponent terrainPainter = (TerrainPainterComponent)target;

        LayerMask tempMask = EditorGUILayout.MaskField("Hit Mask", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(terrainPainter.hitMask), InternalEditorUtility.layers);
        terrainPainter.hitMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        terrainPainter.brushTexture = (Texture2D)EditorGUILayout.ObjectField("Brush Texture", terrainPainter.brushTexture, typeof(Texture2D), false);
        if (EditorGUI.EndChangeCheck())
        {
            brushPreviewTexture.SetPixels(terrainPainter.brushTexture.GetPixels());
            brushPreviewTexture.Apply();
        }

        terrainPainter.brushMode = (BrushMode)EditorGUILayout.EnumPopup("Brush Mode", terrainPainter.brushMode);

        terrainPainter.brushSize = EditorGUILayout.Vector2IntField("Brush Size",(Vector2Int) terrainPainter.brushSize);
        terrainPainter.brushStrength = EditorGUILayout.Slider("Brush Strength", terrainPainter.brushStrength, 0, 1);

        EditorGUILayout.Space();

        terrainPainter.minHeight = EditorGUILayout.FloatField("Min Height", terrainPainter.minHeight);
        terrainPainter.maxHeight = EditorGUILayout.FloatField("Max Height", terrainPainter.maxHeight);

        EditorGUILayout.Space();

        terrainPainter.maskTexture = (Texture2D)EditorGUILayout.ObjectField("Mask Texture", terrainPainter.maskTexture, typeof(Texture2D), false);

        EditorGUILayout.Space();

        terrainPainter.terrain = (Terrain)EditorGUILayout.ObjectField("Terrain Object", terrainPainter.terrain, typeof(Terrain), true);

        EditorGUILayout.Space();


        this.serializedObject.Update();
        EditorGUILayout.PropertyField(this.serializedObject.FindProperty("onPaintingMask"), true);
        this.serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();



        EditorGUILayout.Space();

        if (GUILayout.Button("Clear Mask"))
        {
            Undo.RecordObject(terrainPainter, "Clear Mask");
            terrainPainter.ClearMask();
        }

        if (GUILayout.Button("Create Texture"))
        {
            Undo.RecordObject(terrainPainter, "Create Texture");
            terrainPainter.Init();
        }
    }

    private void OnSceneGUI()
    {
        TerrainPainterComponent terrainPainter = (TerrainPainterComponent)target;

        // Draw the brush in the Scene view

        Color discColor = new Color(Color.green.r, Color.green.g, Color.green.b, 0.5f);
        Handles.color = discColor;

        Handles.DrawSolidDisc
            (terrainPainter.hitPosGizmo, terrainPainter.hitNormal, (float)terrainPainter.brushSize.x / (float)terrainPainter.terrainData.alphamapWidth * terrainPainter.terrainData.size.x);

    }
}

