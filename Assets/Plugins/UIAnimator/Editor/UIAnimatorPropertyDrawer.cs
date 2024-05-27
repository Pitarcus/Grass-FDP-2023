using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(UIAnimatorSettings))]
public class UIAnimatorPropertyDrawer : PropertyDrawer
{

    public bool showAnimatorSettings = true;
    public string status = "Animator Settings";
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        showAnimatorSettings = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), showAnimatorSettings, status);

        float addY = EditorGUIUtility.singleLineHeight;

        Rect canvasGroupRect = new Rect(position.x, position.y + addY * 1, position.width, EditorGUIUtility.singleLineHeight);
        Rect transformToMoveRect = new Rect(position.x, position.y + addY * 2, position.width, EditorGUIUtility.singleLineHeight);
        Rect animationTypeRect = new Rect(position.x, position.y + addY * 4, position.width, EditorGUIUtility.singleLineHeight);
        Rect transitionTimeRect = new Rect(position.x, position.y + addY * 5, position.width, EditorGUIUtility.singleLineHeight);
        Rect easingTypeRect = new Rect(position.x, position.y + addY * 6, position.width, EditorGUIUtility.singleLineHeight);
        Rect delayRect = new Rect(position.x, position.y + addY * 7, position.width, EditorGUIUtility.singleLineHeight);
        Rect animateOnEnableRect = new Rect(position.x, position.y + addY * 8, position.width, EditorGUIUtility.singleLineHeight);
        Rect playInvertedRect = new Rect(position.x, position.y + addY * 9, position.width, EditorGUIUtility.singleLineHeight);
        Rect playToggleRect = new Rect(position.x, position.y + addY * 10, position.width, EditorGUIUtility.singleLineHeight);
        
        Rect toScaleRect = new Rect(position.x, position.y + addY * 12, position.width, EditorGUIUtility.singleLineHeight);
        Rect toPositionRect = new Rect(position.x, position.y + addY * 13, position.width, EditorGUIUtility.singleLineHeight);
        Rect shakeStrengthRect = new Rect(position.x, position.y + addY * 14, position.width, EditorGUIUtility.singleLineHeight);

        Rect onUIAnimationFinishedRect = new Rect(position.x, position.y + addY * 15, position.width, position.height);

        if (showAnimatorSettings)
        {
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 1;

            SerializedProperty canvasGroup = property.FindPropertyRelative("canvasGroup");
            EditorGUI.PropertyField(canvasGroupRect, canvasGroup);

            SerializedProperty transformToMove = property.FindPropertyRelative("transformToMove");
            EditorGUI.PropertyField(transformToMoveRect, transformToMove);

            SerializedProperty animationType = property.FindPropertyRelative("animationType");
            EditorGUI.PropertyField(animationTypeRect, animationType);

            SerializedProperty transitionTime = property.FindPropertyRelative("transitionTime");
            EditorGUI.PropertyField(transitionTimeRect, transitionTime);

            SerializedProperty easingType = property.FindPropertyRelative("easingType");
            EditorGUI.PropertyField(easingTypeRect, easingType);

            SerializedProperty delay = property.FindPropertyRelative("delay");
            EditorGUI.PropertyField(delayRect, delay);

            SerializedProperty animateOnEnable = property.FindPropertyRelative("animateOnEnable");
            EditorGUI.PropertyField(animateOnEnableRect, animateOnEnable);

            SerializedProperty playInverted = property.FindPropertyRelative("playInverted");
            EditorGUI.PropertyField(playInvertedRect, playInverted);

            SerializedProperty playToggle = property.FindPropertyRelative("playToggle");
            EditorGUI.PropertyField(playToggleRect, playToggle);

            SerializedProperty onUIAnimationFinished = property.FindPropertyRelative("onUIAnimationFinished");
            EditorGUI.PropertyField(onUIAnimationFinishedRect, onUIAnimationFinished);

            SerializedProperty toScale = property.FindPropertyRelative("toScale");
            EditorGUI.PropertyField(toScaleRect, toScale);

            SerializedProperty toPosition = property.FindPropertyRelative("toPosition");
            EditorGUI.PropertyField(toPositionRect, toPosition);

            SerializedProperty shakeStrength = property.FindPropertyRelative("shakeStrength");
            EditorGUI.PropertyField(shakeStrengthRect, shakeStrength);

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (showAnimatorSettings)
        {
            return EditorGUIUtility.singleLineHeight * 20;
        }
        else
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }

}
