using System;
using System.Collections;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(MaskableGraphic))]
public class TweeningGraphic : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Components")]
    public MaskableGraphic ColorTarget;
    
    public Transform SizeTarget;

    [Header("Tweens")]
    [SerializeField] private GraphicTween _clickTween;
    [SerializeField] private GraphicTween _enterTween;
    [SerializeField] private GraphicTween _exitTween;

    [Header("Other")]
    [SerializeField] private bool _resizeParent = true;
    [SerializeField] private bool _selectable;

    private TweenerCore<Vector3, Vector3, VectorOptions> _sizeTween;
    private TweenerCore<Color, Color, ColorOptions> _colorTween;

    private bool _isTweenActive
    {
        get
        {
            if (_sizeTween == null || _colorTween == null)
                return false;

            return _sizeTween.active && _colorTween.active;
        }
    }

    public bool IsHighlighted { get; private set; }
    public bool IsSelected { get; private set; }

    private void OnValidate()
    {
        _clickTween?.OnValidate();
        _enterTween?.OnValidate();
        _exitTween?.OnValidate();

        TryGetComponent<MaskableGraphic>(out ColorTarget);

        SizeTarget = _resizeParent ? transform.parent : transform;
    }

    private void Start()
    {
        SetStats(_exitTween);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SetStats(_clickTween);

        GraphicTween targetTween = IsHighlighted ? _enterTween : _exitTween;
        DoTween(targetTween);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        IsHighlighted = true;
        DoTween(_enterTween);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        IsHighlighted = false;
        DoTween(_exitTween);
    }

    private void DoTween(GraphicTween tween)
    {
        if (IsSelected || !gameObject.activeInHierarchy) return;

        MakeTween(tween);
    }

    private void MakeTween(GraphicTween tween)
    {
        CompleteTweens();

        _sizeTween = SizeTarget.DOScale(tween.TweenSize, tween.TweenDuration).SetEase(tween.TweenEase);
        _colorTween = ColorTarget.DOColor(tween.TweenColor, tween.TweenDuration).SetEase(tween.TweenEase);
    }

    private void CompleteTweens()
    {
        if (!_isTweenActive)
        {
            _sizeTween.Complete();
            _colorTween.Complete();
        }
    }

    public void SetStats(GraphicTween stats)
    {
        if (IsSelected) return;

        ColorTarget.color = stats.TweenColor;
        SizeTarget.localScale = Vector3.one * stats.TweenSize;
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!_selectable) return;

        DoTween(_enterTween);

        IsSelected = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!_selectable) return;

        IsSelected = false;

        DoTween(_exitTween);
    }
}

[Serializable]
public class GraphicTween
{
    public GraphicTweenPresets TweenPreset;

    public void OnValidate()
    {
        void SetParams(Ease ease, float duration, Color color, float size)
        {
            TweenEase = ease;
            TweenDuration = duration;
            TweenColor = color;
            TweenSize = size;
        }

        switch (TweenPreset)
        {
            case GraphicTweenPresets.HighlightNeutral:
                SetParams(Ease.OutCirc, 0.15f, Color.white, 1.025f);
                break;

            case GraphicTweenPresets.HighlightDanger:
                SetParams(Ease.OutCirc, 0.15f, ColorISH.Red, 1.025f);
                break;

            case GraphicTweenPresets.HighlightPositive:
                SetParams(Ease.OutCirc, 0.15f, ColorISH.Green, 1.025f);
                break;

            case GraphicTweenPresets.NormalDark:
                SetParams(Ease.OutCirc, 0.15f, Color.gray, 1f);
                break;

            case GraphicTweenPresets.NormalBright:
                SetParams(Ease.OutCirc, 0.15f, new Color(0.75f, 0.75f, 0.75f), 1f);
                break;

            case GraphicTweenPresets.Pressed:
                SetParams(Ease.OutCirc, 0f, Color.white, 1.075f);
                break;
        }
    }

    public Ease TweenEase = Ease.OutCirc;

    [Space(9)]

    public float TweenDuration = 0.25f;
    public Color TweenColor = Color.white;
    public float TweenSize = 1f;
}

public enum GraphicTweenPresets
{
    Custom,
    Pressed,
    HighlightDanger,
    HighlightPositive,
    HighlightNeutral,
    NormalDark,
    NormalBright
}
