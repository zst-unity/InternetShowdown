using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

public class Markers : MonoBehaviour, IEverywhereCanvas
{
    public static Markers Singleton { get; private set; }

    [SerializeField] private CanvasGroup _hitMarker;
    [SerializeField] private CanvasGroup _damageMarker;

    private TweenerCore<float, float, FloatOptions> _hitMarkerFadeTween;
    private TweenerCore<float, float, FloatOptions> _damageMarkerFadeTween;

    public bool Active { get; set; }

    public void ResetCanvas()
    {
        Singleton = this;

        _hitMarker.alpha = 0;
        _damageMarker.alpha = 0;
    }

    public void DoHitMarker()
    {
        _hitMarkerFadeTween.Complete();

        _hitMarker.alpha = 0.3f;
        _hitMarkerFadeTween = _hitMarker.DOFade(0f, 0.65f);
    }

    public void DoDamageMarker()
    {
        _damageMarkerFadeTween.Complete();

        _damageMarker.alpha = 1f;
        _damageMarkerFadeTween = _damageMarker.DOFade(0f, 1.5f);
    }

    public void OnDisconnect() { }
}
