using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.UI;

public class Hud : MonoBehaviour, IEverywhereCanvas
{
    public static Hud Singleton;
    public bool Active { get; set; }

    [SerializeField] private Transform _hudTransform;

    [Header("Health Slider")]
    [SerializeField] private Slider Health;
    [SerializeField] private Image HealthFill;

    [Space(9)]

    [SerializeField] private float _healthBarAnimationSpeed = 0.5f;
    [SerializeField] private Color _healthMinColor;
    [SerializeField] private Color _healthMaxColor;

    [Header("Interaction Slider")]
    [SerializeField] private RectTransform _interactionTransform;
    [SerializeField] private Slider _interaction;
    [SerializeField] private Image _interactionFill;

    [Space(9)]

    [SerializeField] private Color _interactionMinColor;
    [SerializeField] private Color _interactionMaxColor;
    [SerializeField] private AudioClip _interactionShow;
    [SerializeField] private AudioClip _interactionHide;

    [Header("Dashes Count")]
    [SerializeField] private Transform _dotsContainer;
    [SerializeField] private GameObject _dotPrefab;

    private TweenerCore<float, float, FloatOptions> _healthValueTween;
    private TweenerCore<Color, Color, ColorOptions> _healthColorTween;

    private TweenerCore<Vector2, Vector2, VectorOptions> _interactionMoveTween;
    private TweenerCore<float, float, FloatOptions> _interactionValueTween;
    private TweenerCore<Color, Color, ColorOptions> _interactionColorTween;

    private Tweener _hudShakeTween;

    public void OnDisconnect() { }

    public void ResetCanvas()
    {
        Singleton = this;

        _interactionTransform.anchoredPosition = new Vector2(0, -30f);
    }

    public Slider HealthSlider() => Health;

    public void Shake(ShakeEffect shakeEffect)
    {
        _hudShakeTween.Complete();

        _hudShakeTween = _hudTransform.DOShakePosition(shakeEffect.Duration, shakeEffect.Strength);
    }

    public void RemoveLastDashDot()
    {
        Transform lastDot = _dotsContainer.GetChild(_dotsContainer.childCount - 1);
        lastDot.DOScale(0f, 0.25f).SetEase(Ease.InBack).OnComplete(() => Destroy(lastDot.gameObject));
    }

    public void SetDashes(int amount)
    {
        foreach (Transform dot in _dotsContainer)
        {
            Destroy(dot.gameObject);
        }

        for (int i = 0; i < amount; i++)
        {
            GameObject newDot = Instantiate(_dotPrefab, _dotsContainer);
            newDot.transform.localScale = Vector2.zero;
            newDot.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
        }
    }

    public void TweenHealthToAmount(float value)
    {
        _healthValueTween?.Kill();
        _healthColorTween?.Kill();

        _healthValueTween = Health.DOValue(value, _healthBarAnimationSpeed).SetEase(Ease.OutCirc);
        _healthColorTween = HealthFill.DOColor(Color.Lerp(_healthMinColor, _healthMaxColor, value / Health.maxValue), _healthBarAnimationSpeed);
    }

    public void StartInteraction(float duration)
    {
        _interactionValueTween?.Kill();
        _interactionColorTween?.Kill();

        _interactionMoveTween?.Complete();

        SoundSystem.PlayInterfaceSound(new SoundTransporter(_interactionShow), volume: 0.45f);
        _interactionMoveTween = _interactionTransform.DOAnchorPosY(66.8f, 0.1f).SetEase(Ease.OutSine);

        _interactionFill.color = _interactionMinColor;
        _interaction.value = 0f;

        _interactionValueTween = _interaction.DOValue(1f, duration);
        _interactionColorTween = _interactionFill.DOColor(_interactionMaxColor, duration);

        Invoke(nameof(StopInteraction), duration);
    }

    public void StopInteraction()
    {
        CancelInvoke(nameof(StopInteraction));

        _interactionValueTween?.Kill();
        _interactionColorTween?.Kill();

        _interactionMoveTween?.Complete();

        SoundSystem.PlayInterfaceSound(new SoundTransporter(_interactionHide), volume: 0.45f);
        _interactionMoveTween = _interactionTransform.DOAnchorPosY(30f, 0.1f).SetEase(Ease.InSine);
    }
}
