using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

public class Overlays : MonoBehaviour, IEverywhereCanvas
{
    public static Overlays Singleton;
    public bool Active { get; set; }

    [SerializeField] private CanvasGroup _invincibleOverlay;

    private TweenerCore<float, float, FloatOptions> _invincibleOverlayTween;

    public void OnDisconnect() { }

    public void Reset()
    {
        Singleton = this;

        _invincibleOverlay.alpha = 0;
    }

    public void DoInvincibleOverlay(bool show, float fadeTime = 1f)
    {
        _invincibleOverlayTween.Complete();

        float endValue = show ? 1 : 0;
        _invincibleOverlayTween = _invincibleOverlay.DOFade(endValue, fadeTime);
    }
    public void BeginInvincibleOverlay() => DoInvincibleOverlay(true, 0.25f);
    public void EndInvincibleOverlay() => DoInvincibleOverlay(false);
}
