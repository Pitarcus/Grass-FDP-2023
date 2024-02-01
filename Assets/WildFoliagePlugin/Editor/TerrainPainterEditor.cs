using UnityEngine;
using UnityEditor;
using System.Collections;
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
    SerializedProperty alphamapWidth;
    SerializedProperty alphamapHeight;
    SerializedProperty terrainDimensions;
    SerializedProperty isUnityTerrain;
    SerializedProperty maskTexture;
    #endregion

    private bool m_brushGroup = true;

    private Texture2D brushPreviewTexture;
    private TerrainPainterComponent terrainPainter;
    private void OnEnable()
    {
        terrainPainter = (TerrainPainterComponent)target;

        hitMask = serializedObject.FindProperty("hitMask");
        brushTexture = serializedObject.FindProperty("brushTexture");
        brushMode = serializedObject.FindProperty("brushMode");
        brushSize = serializedObject.FindProperty("brushSize");
        brushStrength = serializedObject.FindProperty("brushStrength");

        alphamapWidth = serializedObject.FindProperty("alphamapWidth");
        alphamapHeight = serializedObject.FindProperty("alphamapHeight");
        terrainDimensions = serializedObject.FindProperty("terrainDimensions");
        isUnityTerrain = serializedObject.FindProperty("isUnityTerrain");

        maskTexture = serializedObject.FindProperty("realMaskTexture");

    }

    private void OnDisable()
    {
        DestroyImmediate(brushPreviewTexture);
    }

    public override void OnInspectorGUI()   // Assign and construct the editor window and its appearance. Should be done always
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(maskTexture);

        EditorGUILayout.Space(15);

        // Terrain parameters
        EditorGUILayout.PropertyField(alphamapWidth);
        EditorGUILayout.PropertyField(alphamapHeight);
        EditorGUILayout.PropertyField(terrainDimensions);
        EditorGUILayout.PropertyField(isUnityTerrain);


        EditorGUILayout.Space(15);

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

        if (GUILayout.Button("Reset to saved texture"))
        {
            terrainPainter.ResetMaskTextureToAsset();
        }

        if (GUILayout.Button("Save texture"))
        {
            terrainPainter.SaveTexture();
        }
    }

    private void OnSceneGUI()
    {

        // Draw the brush in the Scene view
        Color discColor = new Color(Color.green.r, Color.green.g, Color.green.b, 0.5f);
        Handles.color = discColor;

        if (terrainPainter != null)
            Handles.DrawSolidDisc
                (terrainPainter.hitPosGizmo, terrainPainter.hitNormal, (float)terrainPainter.brushSize / (float)terrainPainter.alphamapWidth * terrainPainter.terrainDimensions.x / 2);

    }
}

