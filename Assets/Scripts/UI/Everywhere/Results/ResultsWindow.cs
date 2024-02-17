using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

public class ResultsWindow : MonoBehaviour, IEverywhereCanvas
{
    public static ResultsWindow Singleton { get; private set; }

    [field: SerializeField] public CanvasGroup Window { get; private set; }
    [SerializeField] private Transform _statsContainer;
    [SerializeField] private GameObject _statPrefab;

    [Header("Sounds")]
    [SerializeField] private AudioClip _statShow;
    [SerializeField] private AudioClip _windowShow;
    [SerializeField] private AudioClip _windowHide;

    private List<ResultsStat> _stats = new List<ResultsStat>();

    public bool IsEnabled { get; private set; }
    public bool Active { get; set; }

    private TweenerCore<float, float, FloatOptions> _fadeTween;
    private TweenerCore<Vector3, Vector3, VectorOptions> _scaleTween;

    private readonly Dictionary<ResultsStat, TweenerCore<float, float, FloatOptions>> _statsFadeTweens = new();

    public void ResetCanvas()
    {
        Singleton = this;

        _fadeTween = null;
        _scaleTween = null;

        IsEnabled = false;

        _stats.Clear();
    }

    public void SetWindow(bool active, bool modifyCursor = true)
    {
        if (IsEnabled == active) return;

        StopCoroutine(nameof(SetWindowCorourine));

        WindowParams windowParams = new WindowParams()
        {
            Active = active,
            ModifyCursor = modifyCursor
        };

        StartCoroutine(nameof(SetWindowCorourine), windowParams);
    }

    private struct WindowParams
    {
        public bool Active;
        public bool ModifyCursor;
    }

    private IEnumerator SetWindowCorourine(WindowParams windowParams)
    {
        IsEnabled = windowParams.Active;

        _fadeTween.Complete();
        _scaleTween.Complete();

        float endValue = windowParams.Active ? 1 : 0;
        Ease ease = windowParams.Active ? Ease.OutBack : Ease.InBack;

        _fadeTween = Window.DOFade(endValue, 0.45f);
        _scaleTween = Window.transform.DOScale(Vector2.one * endValue, 0.5f).SetEase(ease);

        if (windowParams.Active)
        {
            foreach (Transform stat in _statsContainer) { Destroy(stat.gameObject); }

            _stats.Clear();

            int index = 1;
            foreach (var stat in ResultsStatsJobs.StatsToDisplay)
            {
                ResultsStat newStat = Instantiate(_statPrefab, _statsContainer).GetComponent<ResultsStat>();

                newStat.Set($"{stat.Key}:", stat.Value.Value, stat.Value.AskForColor());

                if (index % 2 == 0)
                {
                    newStat.Panel.color = new Color(0, 0, 0, 0.1f);
                }

                _stats.Add(newStat);

                index++;
            }

            SoundSystem.PlayInterfaceSound(new SoundTransporter(_windowShow), volume: 0.6f);

            if (windowParams.ModifyCursor) { Cursor.lockState = CursorLockMode.None; }

            yield return new WaitForSeconds(0.5f);

            float pitch = 0.85f;
            foreach (var stat in _stats)
            {
                _statsFadeTweens.Add(stat, stat.Group.DOFade(1, 0.15f));

                SoundSystem.PlayInterfaceSound(new SoundTransporter(_statShow), pitch, pitch, 0.6f);
                pitch += 0.05f;

                yield return new WaitForSeconds(0.125f);
            }
        }
        else
        {
            SoundSystem.PlayInterfaceSound(new SoundTransporter(_windowHide), volume: 0.6f);

            if (windowParams.ModifyCursor) { Cursor.lockState = CursorLockMode.Locked; }

            foreach (var stat in _stats)
            {
                if (_statsFadeTweens.ContainsKey(stat)) _statsFadeTweens[stat].Complete();
                stat.Group.alpha = 0;
            }
            _statsFadeTweens.Clear();
        }
    }

    public void OnDisconnect()
    {
        SetWindow(false, false);
    }
}
