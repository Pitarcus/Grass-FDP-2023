using DG.Tweening;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using static UIAnimator;

[Serializable]
public class UIAnimatorSettings
{
    public CanvasGroup canvasGroup;
    public RectTransform transformToMove;

    //[Header("Parameters")]
    public UIAnimationType animationType;
    public float transitionTime;
    public DG.Tweening.Ease easingType;
    public float delay;
    public bool animateOnEnable;
    public bool playInverted = false;
    public bool playToggle = false;

    public UnityEvent onUIAnimationFinished;


    
    [SerializeField] public Vector3 toScale;

   
    [SerializeField] public Vector2 toPosition;

    public float shakeStrength;


    public Vector3 _originalScale;
    public Vector2 _originalPosition;
    public bool _playedOnce = false;

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
            transformToMove.DOScale(toScale, transitionTime).SetEase(easingType).SetDelay(delay);
        }
        else
        {
            transformToMove.DOScale(_originalScale, transitionTime).SetEase(easingType).SetDelay(delay);
        }
    }

    #endregion

    #region MoveAnimation
    private void MoveTransform()
    {
        if (!playInverted)
            transformToMove.DOAnchorPos(toPosition, transitionTime).SetEase(easingType).SetDelay(delay).OnComplete(UIAnimationFinishedInvoke);
        else
            transformToMove.DOAnchorPos(_originalPosition, transitionTime).SetEase(easingType).SetDelay(delay).OnComplete(UIAnimationFinishedInvoke);
    }

    #endregion

    #region ScaleAnimation
    private void Shake()
    {
        transformToMove.DOShakeAnchorPos(transitionTime, shakeStrength).SetEase(easingType).SetDelay(delay);
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

[Serializable]
public class UIAnimator : MonoBehaviour
{
    public enum UIAnimationType
    {
        scale,
        move,
        shake,
        fadeIn

    }

    //[Header("References")]
    //[SerializeField] public CanvasGroup canvasGroup;
    //[SerializeField] public RectTransform transformToMove;

    //[Header("Parameters")]
    //[SerializeField] public UIAnimationType animationType;
    //[SerializeField] public float transitionTime;
    //[SerializeField] public DG.Tweening.Ease easingType;
    //[SerializeField] public float delay;
    //[SerializeField] public bool animateOnEnable;
    //[SerializeField] public bool playInverted = false;
    //[SerializeField] public bool playToggle = false;

    //[SerializeField] public UnityEvent onUIAnimationFinished;


    //[Header("Parameters for Scale")]
    //[SerializeField] public Vector3 toScale;

    //[Header("Parameters for Alpha")]

    //[Header("Parameters for Move")]
    //[SerializeField] public Vector2 toPosition;
    

    //[Header("Parameters for Shake")]
    //public float shakeStrength;

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
        if (anim.animateOnEnable)
        {
            anim.AnimateUI();
        }
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