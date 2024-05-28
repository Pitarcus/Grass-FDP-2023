using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;


public enum UIAnimatorSequenceInsertType
{
    Append,
    Join
}

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
    [SerializeField] bool loop = false;
    [SerializeField] [Tooltip("Invert each animation when called again")] bool toggleInverted = false;
    [SerializeField][Tooltip("Invert the order of the sequence when called again")] bool reverseOnRepeat = false;

    [Space(3)]
    [SerializeField] public UnityEvent onSequenceComplete;
    [SerializeField] public UnityEvent onSequenceCompleteReverse;

    // Memebers
    private bool playedOnce = false;
    private bool reverse = false;
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

        // Manage sequence order
        if (reverseOnRepeat && !reverse && playedOnce)
        {
            // Backwards
            for (int i = animators.Length - 1; i >= 0; i--)
            {
                // Not adding the interval of the first tween for correct usage
                if (i != animators.Length - 1)
                    sequence.AppendInterval(animators[i+1].delay);

                ManageAnimator(animators[i]);

                // Append duration of the last tween for correct callback of the onSequenceComplete event
                if (i == 0)
                {
                    sequence.AppendInterval(animators[i].UIAnimatorSettings.transitionTime);
                }
            }
            reverse = true;
        }
        else 
        {
            // Forward
            for (int i = 0; i < animators.Length; i++)
            {
                sequence.AppendInterval(animators[i].delay);
                ManageAnimator(animators[i]);

                // Append duration of the last tween for correct callback of the onSequenceComplete event
                if (i == animators.Length - 1)   
                {
                    sequence.AppendInterval(animators[i].UIAnimatorSettings.transitionTime);
                }
            }
            reverse = false;
        }

        // Loop
        if (loop)
            sequence.OnComplete(() => PlaySequence());
        else
        {
            if (!reverse)
                sequence.OnComplete(() => onSequenceComplete.Invoke());
            else
                sequence.OnComplete(() => onSequenceCompleteReverse.Invoke());
        }

        // Play
        sequence.Play();

        playedOnce = true;
    }

    private void ManageAnimator(UIAnimatorSequenceElement animator)
    {
        if (toggleInverted && playedOnce)
        {
            // Invert sequence
            animator.UIAnimatorSettings.playInverted = !animator.UIAnimatorSettings.playInverted;
        }

        // Add the following tween
        if (animator.UIAnimatorSettings.insertType == UIAnimatorSequenceInsertType.Append)
        {

            sequence.AppendCallback(animator.UIAnimatorSettings.AnimateUI);
        }
        else if(animator.UIAnimatorSettings.insertType == UIAnimatorSequenceInsertType.Join)
        {
            sequence.JoinCallback(animator.UIAnimatorSettings.AnimateUI);
        }
    }
}
