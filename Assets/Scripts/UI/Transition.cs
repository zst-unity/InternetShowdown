using System;
using System.Collections;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Graphic))]
public class Transition : MonoBehaviour
{
    [SerializeField] private RectTransform _transform;
    [SerializeField] private Graphic _graphic;

    [field: SerializeField] public float FullDurationIn { get; private set; } = 1f;
    [field: SerializeField] public float FullDurationOut { get; private set; } = 0.6f;
    [field: SerializeField] public Ease FullEase { get; private set; } = Ease.OutSine;

    [HideInInspector] public bool Active;

    private TweenerCore<Vector3, Vector3, VectorOptions> _transitionTween;

    // CALLBACKS

    public Action<TransitionMode> OnTransitionEnd;
    public Action<TransitionMode> OnTransitionStart;

    private void OnValidate()
    {
        if (TryGetComponent<RectTransform>(out _transform))
        {
            (_transform.anchorMin, _transform.anchorMax) = (new Vector2(0, 0), new Vector2(1, 1));
            (_transform.offsetMin, _transform.offsetMax) = (new Vector2(0, 0), new Vector2(0, 0));
        }

        TryGetComponent<Graphic>(out _graphic);
    }

    private void Awake()
    {
        AwakeTransition(TransitionMode.Out);
    }

    public void AwakeTransition(TransitionMode mode, Action onEnd = null)
    {
        StopCoroutine(nameof(TransitionCoroutine));

        _transitionTween.Complete();

        StartCoroutine(nameof(TransitionCoroutine), new TransitionTransporter(mode, onEnd));
    }

    private class TransitionTransporter
    {
        public TransitionMode Mode;
        public Action OnEnd;

        public TransitionTransporter(TransitionMode mode, Action onEnd = null)
        {
            Mode = mode;
            OnEnd = onEnd;
        }
    }

    private IEnumerator TransitionCoroutine(TransitionTransporter transporter)
    {
        OnTransitionStart?.Invoke(transporter.Mode);

        Active = true;
        _graphic.raycastTarget = true;

        float endValue = ((float)transporter.Mode);
        float duration = transporter.Mode == TransitionMode.In ? FullDurationIn : FullDurationOut;

        _transform.localScale = new Vector2(1 - endValue, 1);
        _transitionTween = _transform.DOScaleX(endValue, duration).SetEase(FullEase);

        yield return new WaitForSeconds(duration);

        OnTransitionMasterEnd(transporter.Mode);

        OnTransitionEnd?.Invoke(transporter.Mode);
        transporter.OnEnd?.Invoke();
    }

    private void OnTransitionMasterEnd(TransitionMode mode)
    {
        Active = false;

        bool raycastTarget = (mode == TransitionMode.In);
        _graphic.raycastTarget = raycastTarget;
    }

    public static Transition Singleton()
    {
        return FindFirstObjectByType<Transition>();
    }
}

public enum TransitionMode
{
    Out,
    In
}
