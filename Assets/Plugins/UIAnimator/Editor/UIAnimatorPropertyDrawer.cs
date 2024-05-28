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

        SerializedProperty animationTypeProperty = property.FindPropertyRelative("animationType");
        UIAnimationType animationType = (UIAnimationType)animationTypeProperty.enumValueIndex;


        float addY = EditorGUIUtility.singleLineHeight;

        Rect classLabelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        Rect canvasGroupRect = new Rect(position.x, position.y + addY , position.width, EditorGUIUtility.singleLineHeight);
        Rect transformToMoveRect = new Rect(position.x, position.y + addY, position.width, EditorGUIUtility.singleLineHeight);
        Rect animationTypeRect = new Rect(position.x, position.y + addY * 3, position.width, EditorGUIUtility.singleLineHeight);
        Rect transitionTimeRect = new Rect(position.x, position.y + addY * 4, position.width, EditorGUIUtility.singleLineHeight);
        Rect insertTypeRect = new Rect(position.x, position.y + addY * 5, position.width, EditorGUIUtility.singleLineHeight);
        Rect easingTypeRect = new Rect(position.x, position.y + addY * 6, position.width, EditorGUIUtility.singleLineHeight);
        Rect delayRect = new Rect(position.x, position.y + addY * 7, position.width, EditorGUIUtility.singleLineHeight);
        Rect animateOnEnableRect = new Rect(position.x, position.y + addY * 8, position.width, EditorGUIUtility.singleLineHeight);
        Rect playInvertedRect = new Rect(position.x, position.y + addY * 9, position.width, EditorGUIUtility.singleLineHeight);
        Rect playToggleRect = new Rect(position.x, position.y + addY * 10, position.width, EditorGUIUtility.singleLineHeight);
        
        Rect toScaleRect = new Rect(position.x, position.y + addY * 12, position.width, EditorGUIUtility.singleLineHeight);
        Rect toPositionRect = new Rect(position.x, position.y + addY * 12, position.width, EditorGUIUtility.singleLineHeight);
        Rect shakeStrengthRect = new Rect(position.x, position.y + addY * 12, position.width, EditorGUIUtility.singleLineHeight);

        int eventPosition = animationType == UIAnimationType.fadeIn ? 13 : 14;

        Rect onUIAnimationFinishedRect = new Rect(position.x, position.y + addY * eventPosition, position.width, position.height);


        // -- DRAW GUI --

        EditorGUI.LabelField(classLabelRect, "UI Animation Settings");

        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 1;

        

        // References
        if (animationType == UIAnimationType.fadeIn)
        {
            SerializedProperty canvasGroup = property.FindPropertyRelative("canvasGroup");
            EditorGUI.PropertyField(canvasGroupRect, canvasGroup);
        }
        else
        {
            SerializedProperty transformToMove = property.FindPropertyRelative("transformToMove");
            EditorGUI.PropertyField(transformToMoveRect, transformToMove);
        }

            
        EditorGUI.PropertyField(animationTypeRect, animationTypeProperty);


        // Parameters
        SerializedProperty transitionTime = property.FindPropertyRelative("transitionTime");
        EditorGUI.PropertyField(transitionTimeRect, transitionTime);

        SerializedProperty easingType = property.FindPropertyRelative("easingType");
        EditorGUI.PropertyField(easingTypeRect, easingType);

      
        SerializedProperty insertType = property.FindPropertyRelative("insertType");
        EditorGUI.PropertyField(insertTypeRect, insertType);


        SerializedProperty delay = property.FindPropertyRelative("delay");
        EditorGUI.PropertyField(delayRect, delay);

        SerializedProperty animateOnEnable = property.FindPropertyRelative("animateOnEnable");
        EditorGUI.PropertyField(animateOnEnableRect, animateOnEnable);

        SerializedProperty playInverted = property.FindPropertyRelative("playInverted");
        EditorGUI.PropertyField(playInvertedRect, playInverted);

        SerializedProperty playToggle = property.FindPropertyRelative("playToggle");
        EditorGUI.PropertyField(playToggleRect, playToggle);

        // Type sensitive parameters
        if (animationType == UIAnimationType.scale)
        {
            SerializedProperty toScale = property.FindPropertyRelative("toScale");
            EditorGUI.PropertyField(toScaleRect, toScale);
        }
        else if (animationType == UIAnimationType.move)
        {
            SerializedProperty toPosition = property.FindPropertyRelative("toPosition");
            EditorGUI.PropertyField(toPositionRect, toPosition);
        }
        else if (animationType == UIAnimationType.shake)
        {
            SerializedProperty shakeStrength = property.FindPropertyRelative("shakeStrength");
            EditorGUI.PropertyField(shakeStrengthRect, shakeStrength);
        }

        // Events
        SerializedProperty onUIAnimationFinished = property.FindPropertyRelative("onUIAnimationFinished");
        EditorGUI.PropertyField(onUIAnimationFinishedRect, onUIAnimationFinished);

        // Set indent back to what it was
        EditorGUI.indentLevel = indent;
        

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 18;  
    }

}
