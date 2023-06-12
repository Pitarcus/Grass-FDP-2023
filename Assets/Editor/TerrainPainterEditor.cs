using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(TerrainPainterComponent))]
public class TerrainPainterEditor : Editor
{
    #region SerializedProperties
    SerializedProperty hitMask;
    SerializedProperty brushTexture;
    SerializedProperty brushMode;
    SerializedProperty brushSize;
    SerializedProperty brushStrength;
    //SerializedProperty maskTexture;
    #endregion

    private bool m_brushGroup = true;

    private Texture2D brushPreviewTexture;
    private TerrainPainterComponent terrainPainter;
    private void OnEnable()
    {
        terrainPainter = (TerrainPainterComponent)target;
        /*TerrainPainterComponent terrainPainter = (TerrainPainterComponent)target;
        brushPreviewTexture = new Texture2D(256, 256);
        brushPreviewTexture.filterMode = FilterMode.Bilinear;
        brushPreviewTexture.wrapMode = TextureWrapMode.Clamp;
        //brushPreviewTexture.SetPixels(terrainPainter.brushTexture.GetPixels());
        brushPreviewTexture.Apply();*/

        hitMask = serializedObject.FindProperty("hitMask");
        brushTexture = serializedObject.FindProperty("brushTexture");
        brushMode = serializedObject.FindProperty("brushMode");
        brushSize = serializedObject.FindProperty("brushSize");
        brushStrength = serializedObject.FindProperty("brushStrength");
        //maskTexture = serializedObject.FindProperty("maskTexture");

    }

    private void OnDisable()
    {
        DestroyImmediate(brushPreviewTexture);
    }

    public override void OnInspectorGUI()   // Assign and construct the editor window and its appearance. Should be done always
    {
        serializedObject.Update();

        EditorGUILayout.BeginFadeGroup(m_brushGroup ? 1 : 0);
        m_brushGroup = EditorGUILayout.BeginFoldoutHeaderGroup(m_brushGroup, "Brush Parameters");
        if (m_brushGroup)
        {
            EditorGUILayout.PropertyField(brushTexture);
            EditorGUILayout.PropertyField(brushMode);
            EditorGUILayout.PropertyField(brushSize);
            EditorGUILayout.PropertyField(brushStrength);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.EndFadeGroup();

        EditorGUILayout.Space(15);

        EditorGUILayout.LabelField("Mask Texture (debug purposes)");
        //EditorGUILayout.PropertyField(maskTexture);

        EditorGUILayout.PropertyField(hitMask);

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(15);

        if (GUILayout.Button("Clear Mask"))
        {
            Undo.RegisterCompleteObjectUndo(terrainPainter.maskTexture, "Clear grass mask");
            PrefabUtility.RecordPrefabInstancePropertyModifications(terrainPainter.maskTexture);
            Undo.FlushUndoRecordObjects();
            terrainPainter.ClearMask();
        }

        /*if (GUILayout.Button("Save texture"))
        {
            terrainPainter.SaveTexture();
        }*/

        //base.OnInspectorGUI();
    }

    private void OnSceneGUI()
    {
 
        // Draw the brush in the Scene view

        Color discColor = new Color(Color.green.r, Color.green.g, Color.green.b, 0.5f);
        Handles.color = discColor;

        Handles.DrawSolidDisc
            (terrainPainter.hitPosGizmo, terrainPainter.hitNormal, (float)terrainPainter.brushSize / (float)terrainPainter.terrainData.alphamapWidth * terrainPainter.terrainData.size.x);

    }
}

