using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

public class AnimateCanvasGroupAlpha : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Parameters")]
    [SerializeField] private float transitionTime;

    public void ShowGroup(bool show)
    {
        if (show)
        {
            TweenAlpha(0, 1, transitionTime);
        }
        else
        {
            TweenAlpha(1, 0, transitionTime);
        }
    }

    public void HideGroup()
    {
        TweenAlpha(1, 0, transitionTime);
    }


    void TweenAlpha(float init, float end, float time)
    {
        DOVirtual.Float(init, end, transitionTime, SetAlpha);
    }

    void SetAlpha(float value)
    {
        canvasGroup.alpha = value;
    }
}
