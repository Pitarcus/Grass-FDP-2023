using DG.Tweening;
using UnityEngine;


public class UIAnimatorSequence : MonoBehaviour
{
    [System.Serializable]
   
    private class UIAnimatorSequenceElement
    {
        public UIAnimatorSettings UIAnimatorSettings;
        public float delay;
    }

    [SerializeField] UIAnimatorSequenceElement[] animators;
    [SerializeField] bool playOnEnable = false;
    //[SerializeField] bool playInverted = false;
    [SerializeField] bool loop = false;
    [SerializeField] [Tooltip("Invert each animation when called again")] bool toggleInverted = false;
    [SerializeField][Tooltip("Invert the order of the sequence when called again")] bool invertOrderOnRepeat = false;

    //private bool invertedSequence = false;
    private bool playedOnce = false;
    private bool repeat = false;
    private Sequence sequence;

    private void OnEnable()
    {
        if(playOnEnable)
        {
            PlaySequence();
        }
    }

    public void StopSequence()
    {
        sequence.Kill();
    }
    public void PlaySequence()
    {
        sequence = DOTween.Sequence();

        if (invertOrderOnRepeat && repeat)
        {
            for (int i = animators.Length - 1; i >= 0; i--)
            {
                ManageAnimator(animators[i]);
            }
            repeat = false;
        }
        else 
        {
            for (int i = 0; i < animators.Length; i++)
            {
                ManageAnimator(animators[i]);
            }
            repeat = true;
        }

        if (loop)
            sequence.OnComplete(() => PlaySequence());

        sequence.Play();

        playedOnce = true;
    }

    private void ManageAnimator(UIAnimatorSequenceElement animator)
    {
        sequence.AppendInterval(animator.delay);

        if (toggleInverted && playedOnce)
        {
            // Invert sequence
            animator.UIAnimatorSettings.playInverted = !animator.UIAnimatorSettings.playInverted;
        }

        sequence.AppendCallback(animator.UIAnimatorSettings.AnimateUI);
    }
}
