using DG.Tweening;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public enum UIAnimationType
{
    scale,
    move,
    shake,
    fadeIn
}

[Serializable]
public class UIAnimatorSettings
{
    // References
    public CanvasGroup canvasGroup;
    public RectTransform transformToMove;

    // Animation parameters
    public UIAnimationType animationType;
    public float transitionTime;
    public UIAnimatorSequenceInsertType insertType;
    public DG.Tweening.Ease easingType;
    public float delay;
    public bool playInverted = false;
    [Tooltip("Invert the tween each time the animator is called")]public bool playToggle = false;

    // Event
    public UnityEvent onUIAnimationFinished;

    // Type sensitive parameters
    [SerializeField] public Vector3 toScale;
    [SerializeField] public Vector2 toPosition;
    public float shakeStrength;

    //Memebers
    public Vector3 _originalScale; // Only set when played the for the first time in not reverse
    public Vector2 _originalPosition; // Only set when played the for the first time in not reverse
    private bool _playedOnce = false;

    public void AnimateUIInverted()
    {
        playInverted = !playInverted;
        AnimateUI();
        playInverted = !playInverted;
    }
    public void AnimateUI()
    {
        if (playToggle && _playedOnce)
            playInverted = !playInverted;

        switch (animationType)
        {
            case UIAnimationType.scale:
                Scale();
                break;

            case UIAnimationType.move:
                MoveTransform();
                break;

            case UIAnimationType.shake:
                Shake();
                break;

            case UIAnimationType.fadeIn:
                AnimateGroupAlpha();
                break;
        }
        _playedOnce = true;
    }

    private void UIAnimationFinishedInvoke()
    {
        onUIAnimationFinished?.Invoke();
    }

    #region ScaleAnimation
    private void Scale()
    {
        if (!playInverted)
        {
            if (!_playedOnce)
            {
                _originalScale = transformToMove.localScale;
            }
            transformToMove.DOScale(toScale, transitionTime).SetEase(easingType).SetDelay(delay).OnComplete(UIAnimationFinishedInvoke);
        }
        else
        {
            transformToMove.DOScale(_originalScale, transitionTime).SetEase(easingType).SetDelay(delay).OnComplete(UIAnimationFinishedInvoke);
        }
    }

    #endregion

    #region MoveAnimation
    private void MoveTransform()
    {
        if (!playInverted) 
        {
            if (!_playedOnce)
            {
                _originalPosition = transformToMove.anchoredPosition;
            }
            transformToMove.DOAnchorPos(toPosition, transitionTime).SetEase(easingType).SetDelay(delay).OnComplete(UIAnimationFinishedInvoke);
        }
        else
        {
            transformToMove.DOAnchorPos(_originalPosition, transitionTime).SetEase(easingType).SetDelay(delay).OnComplete(UIAnimationFinishedInvoke);
        }
    }

    #endregion

    #region ShakeAnimation
    private void Shake()
    {
        transformToMove.DOShakeAnchorPos(transitionTime, shakeStrength).SetEase(easingType).SetDelay(delay).OnComplete(UIAnimationFinishedInvoke);
    }
    #endregion

    #region AlphaAnimation
    private void AnimateGroupAlpha()
    {
        if (!playInverted)
        {
            TweenAlpha(0, 1, transitionTime);
        }
        else
        {
            TweenAlpha(1, 0, transitionTime);
        }
    }

    void TweenAlpha(float init, float end, float time)
    {
        DOVirtual.Float(init, end, transitionTime, SetAlpha).SetEase(easingType).SetDelay(delay).OnComplete(UIAnimationFinishedInvoke);
    }

    void SetAlpha(float value)
    {
        canvasGroup.alpha = value;
    }
    #endregion

}


public class UIAnimator : MonoBehaviour
{

    public UIAnimatorSettings anim;
    public bool animateOnEnable = false;


    private void OnEnable()
    {
        if(anim.transformToMove == null)
        {
            anim.transformToMove = GetComponent<RectTransform>();
        }
        if (anim.animationType == UIAnimationType.move)
        {
            anim._originalPosition = anim.transformToMove.anchoredPosition;
        }
        if (anim.animationType == UIAnimationType.scale)
        {
            anim._originalScale = anim.transformToMove.localScale;
        }
        if (animateOnEnable)
        {
            anim.AnimateUI();
        }
    }

    public void AnimateUI()
    {
        anim.AnimateUI();
    }

    public void AnimateUIInverted()
    {
        anim.AnimateUIInverted();
    }

}


//#if UNITY_EDITOR
//// -------------------- CUSTOM EDITOR FOR THE SCRIPT ---------------------
//[CustomEditor(typeof(UIAnimator))]
//public class UIAnimatorEditor : Editor
//{
//    // references
//    SerializedProperty canvasGroup;
//    SerializedProperty transformToMove;

//    // properties
//    SerializedProperty animationType;
//    SerializedProperty easingType;
//    SerializedProperty transitionTime;
//    SerializedProperty delay;
//    SerializedProperty animateOnEnable;
//    SerializedProperty playInverted;
//    SerializedProperty playToggle;


//    // move specific properties
//    SerializedProperty toPosition;
//    SerializedProperty shakeStrength;
//    SerializedProperty toScale;

//    // events
//    SerializedProperty onUIAnimationFinished;

//    UIAnimator myScript;

//    private void OnEnable()
//    {
//        myScript = target as UIAnimator;

//        canvasGroup = serializedObject.FindProperty("canvasGroup");
//        transformToMove = serializedObject.FindProperty("transformToMove");

//        animationType = serializedObject.FindProperty("animationType");
//        easingType = serializedObject.FindProperty("easingType");
//        transitionTime = serializedObject.FindProperty("transitionTime");
//        delay = serializedObject.FindProperty("delay");
//        animateOnEnable = serializedObject.FindProperty("animateOnEnable");
//        shakeStrength = serializedObject.FindProperty("shakeStrength");


//        toPosition = serializedObject.FindProperty("toPosition");
//        playInverted = serializedObject.FindProperty("playInverted");
//        playToggle = serializedObject.FindProperty("playToggle");
//        toScale = serializedObject.FindProperty("toScale");

//        onUIAnimationFinished = serializedObject.FindProperty("onUIAnimationFinished");
//    }

//    public override void OnInspectorGUI()
//    {
//        serializedObject.Update();

//        // References
//        if (myScript.animationType == UIAnimator.UIAnimationType.fadeIn)
//        {
//            EditorGUILayout.PropertyField(canvasGroup);

//        }
//        else if (myScript.animationType == UIAnimator.UIAnimationType.move ||
//            myScript.animationType == UIAnimator.UIAnimationType.shake ||
//            myScript.animationType == UIAnimator.UIAnimationType.scale)
//        {
//            EditorGUILayout.PropertyField(transformToMove);
//        }

//        EditorGUILayout.Space(3);

//        // Parameters
//        EditorGUILayout.PropertyField(animationType);
//        EditorGUILayout.PropertyField(transitionTime);
//        EditorGUILayout.PropertyField(easingType); 
//        EditorGUILayout.PropertyField(delay);
//        EditorGUILayout.PropertyField(animateOnEnable);
//        EditorGUILayout.PropertyField(playInverted);
//        EditorGUILayout.PropertyField(playToggle);
        

//        EditorGUILayout.Space(3);

//        // Type sensitive parameters
//        if (myScript.animationType == UIAnimator.UIAnimationType.move)
//        {
//            EditorGUILayout.PropertyField(toPosition);
//        }
//        else if (myScript.animationType == UIAnimator.UIAnimationType.shake)
//        {
//            EditorGUILayout.PropertyField(shakeStrength);
//        }
//        else if (myScript.animationType == UIAnimator.UIAnimationType.scale)
//        {
//            EditorGUILayout.PropertyField(toScale);
//        }

//        EditorGUILayout.Space(3);

//        // Events
//        EditorGUILayout.PropertyField(onUIAnimationFinished);

//        serializedObject.ApplyModifiedProperties();
//    }
//}
//#endif