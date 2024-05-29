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
    [SerializeField] [Tooltip("Invert the order of the sequence when called again")] bool reverseOnRepeat = false;

    [Space(3)]
    [SerializeField] public UnityEvent onSequenceComplete;
    [SerializeField] public UnityEvent onSequenceCompleteReverse;

    // Memebers
    private bool _playedOnce = false;
    private bool _alternator = false;
    private bool _reverse = false;
    private Sequence _sequence;

    private void OnEnable()
    {
        if(playOnEnable)
        {
            PlaySequence();
        }
    }

    public void StopSequence()
    {
        _sequence.Kill();
    }

    public void PlaysequenceReverse()
    {
        _reverse = !_reverse;
        PlaySequence();
        _reverse = !_reverse;
    }

    public void PlaySequence()
    {
        _sequence = DOTween.Sequence();

        // Manage sequence order
        if (reverseOnRepeat && _alternator)
        {
            // Backwards
            for (int i = animators.Length - 1; i >= 0; i--)
            {
                // Not adding the interval of the first tween for correct usage
                if (i != animators.Length - 1)
                    _sequence.AppendInterval(animators[i+1].delay);

                ManageAnimator(animators[i]);

                // Append duration of the last tween for correct callback of the onSequenceComplete event
                if (i == 0)
                {
                    _sequence.AppendInterval(animators[i].UIAnimatorSettings.transitionTime);
                }
            }
            _alternator = !_alternator;
            _reverse = true;
        }
        else 
        {
            // Forward
            for (int i = 0; i < animators.Length; i++)
            {
                _sequence.AppendInterval(animators[i].delay);
                ManageAnimator(animators[i]);

                // Append duration of the last tween for correct callback of the onSequenceComplete event
                if (i == animators.Length - 1)   
                {
                    _sequence.AppendInterval(animators[i].UIAnimatorSettings.transitionTime);
                }
            }
            _alternator = !_alternator;
            _reverse = false;
        }

        // Loop
        if (loop)
            _sequence.OnComplete(() => PlaySequence());
        else
        {
            if (!_reverse)
                _sequence.OnComplete(() => onSequenceComplete.Invoke());
            else
                _sequence.OnComplete(() => onSequenceCompleteReverse.Invoke());
        }

        // Play
        _sequence.Play();

        _playedOnce = true;
    }

    private void ManageAnimator(UIAnimatorSequenceElement animator)
    {
        if (toggleInverted && _playedOnce)
        {
            // Invert sequence
            animator.UIAnimatorSettings.playInverted = !animator.UIAnimatorSettings.playInverted;
        }

        // Add the following tween
        if (animator.UIAnimatorSettings.insertType == UIAnimatorSequenceInsertType.Append)
        {

            _sequence.AppendCallback(animator.UIAnimatorSettings.AnimateUI);
        }
        else if(animator.UIAnimatorSettings.insertType == UIAnimatorSequenceInsertType.Join)
        {
            _sequence.JoinCallback(animator.UIAnimatorSettings.AnimateUI);
        }
    }
}
