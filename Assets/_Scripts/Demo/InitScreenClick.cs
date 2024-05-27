using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class InitScreenClick : MonoBehaviour
{
    public CanvasGroup clickCanvas;
    public AnimationCurve flashAnimCurve;

    public void FlashCanvas()
    {
        clickCanvas.DOFade(0, 1.5f).SetEase(flashAnimCurve)
            .OnComplete
            (() =>
            CinemachineMenuManager.Instance.SwitchToCamera(1)
            ) ;
    }
}
