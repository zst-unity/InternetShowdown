using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EverywhereCanvas : MonoBehaviour, IEverywhereCanvas // юи которое будет видно ВЕЗДЕ
{
    public static EverywhereCanvas Singleton { get; private set; }

    public bool Active
    {
        get => _canvas.activeSelf;
        set => _canvas.SetActive(value);
    }

    [Header("Objects")]
    [SerializeField] private GroupsManager _groupsManager;

    [Header("Main")]
    [SerializeField] private GameObject _canvas;
    [SerializeField] private Transition _transition;

    [Space(9)]

    [SerializeField] private CanvasGroup _inLobby;
    [SerializeField] private CanvasGroup _inGame;

    [Header("Other")]
    [SerializeField] private TMP_Text OthersNickname;
    [field: SerializeField] public TMP_Text Timer { get; private set; }

    [SerializeField] private GameObject _playerDebugPanel;
    [SerializeField] private TMP_Text[] _playerDebugStats;

    [SerializeField] private CanvasGroup _killLog;

    [Header("Preparing")]
    [SerializeField] private TMP_Text _TTOText;

    [Space(9)]

    [SerializeField] private AudioClip _matchBegins;

    [Space(9)]

    [SerializeField] private AudioClip _preMatchOne;
    [SerializeField] private AudioClip _preMatchTwo;
    [SerializeField] private AudioClip _preMatchThree;

    [Header("Voting")]
    [SerializeField] private CanvasGroup _mapVoting;
    [SerializeField] private TMP_Text _votingEndText;

    [Space(9)]

    [SerializeField] private List<AudioClip> _keyboardTyping = new List<AudioClip>();

    private float _targetHealth;

    [Header("Death Screen")]
    [SerializeField] private GameObject _deathScreen;
    [SerializeField] private TMP_Text _respawnCountdown;
    [SerializeField] private TMP_Text _deathPhrase;
    [SerializeField] private List<string> _deathPhrases = new List<string>();

    [Space(9)]

    [SerializeField] private CanvasGroup _kafifEasterEgg;

    [HideInInspector] public bool IsVotingActive { get; private set; }

    private bool _isExitingServer;

    private TweenerCore<float, float, FloatOptions> _lobbyGroupTween;
    private TweenerCore<float, float, FloatOptions> _gameGroupTween;

    private TweenerCore<float, float, FloatOptions> _killLogTween;

    private TweenerCore<float, float, FloatOptions> _mapVotingColorTween;

    private TweenerCore<Vector3, Vector3, VectorOptions> _ttoTextTweenY;
    private TweenerCore<Vector3, Vector3, VectorOptions> _ttoTextTweenX;
    private TweenerCore<Color, Color, ColorOptions> _ttoTextColorTween;
    private TweenerCore<Vector3, Vector3, VectorOptions> _mapVotingScaleTween;

    [SerializeField] private UnityEvent _onStart;

    private void Awake()
    {
        if (FindObjectsByType<EverywhereCanvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(transform);

        Button[] buttons = GetComponentsInChildren<Button>(true);

        foreach (var button in buttons)
        {
            button.onClick.AddListener(ClearSelections);
        }
    }

    public void Reset()
    {
        Singleton = this;

        _killLog.alpha = 0;
        _kafifEasterEgg.alpha = 0;
        _mapVoting.alpha = 0;

        _TTOText.color = ColorISH.Invisible;

        HideDeathScreen();

        _onStart.Invoke();
    }

    private void Update()
    {
        _playerDebugPanel.SetActive(false);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        _playerDebugPanel.SetActive(true);
        DebugStats();
#endif
    }

    public void QuitAction()
    {
        _transition.AwakeTransition(TransitionMode.In, Application.Quit);
    }

    public void MenuAction()
    {
        if (_isExitingServer || !NetworkClient.isConnected)
        {
            return;
        }

        StartCoroutine(nameof(OnDisconnectPressed));

        _isExitingServer = true;

        _transition.AwakeTransition(TransitionMode.In, ExitMatch);
    }

    private void ExitMatch()
    {
        NetworkManager.singleton.StopHost();
    }

    public void OnDisconnect() { }

    private IEnumerator OnDisconnectPressed()
    {
        yield return new WaitUntil(() => !NetworkManager.singleton.isNetworkActive);

        _isExitingServer = false;
    }

    public void OnVotingEnd(string message)
    {
        StopCoroutine(nameof(OnVotingEndCoroutine));
        StartCoroutine(nameof(OnVotingEndCoroutine), message);
    }

    public void PreMatchText(int fromCount)
    {
        StartCoroutine(nameof(PreMatchCoroutine), fromCount);
    }

    private IEnumerator PreMatchCoroutine(int fromCount)
    {
        Color targetColor = Color.white;
        AudioClip targetSound = null;
        string targetText = string.Empty;

        void SetTextParams(Color color, AudioClip sound, string text)
        {
            targetColor = color;
            targetSound = sound;
            targetText = text;
        }

        for (int count = fromCount; count >= 0; count--)
        {
            switch (count)
            {
                case 0:
                    SetTextParams(ColorISH.Magenta, _matchBegins, "Let's Go!");
                    break;

                case 1:
                    SetTextParams(ColorISH.Red, _preMatchOne, count.ToString());
                    break;

                case 2:
                    SetTextParams(ColorISH.Yellow, _preMatchTwo, count.ToString());
                    break;

                case 3:
                    SetTextParams(ColorISH.Green, _preMatchThree, count.ToString());
                    break;

                default:
                    SetTextParams(Color.white, null, count.ToString());
                    break;

            }

            BounceTTOText(targetText, targetColor, targetSound);

            yield return new WaitForSeconds(1f);
        }
    }

    public void BounceTTOText(string text, Color color, AudioClip sound)
    {
        _TTOText.text = text;

        _ttoTextTweenY.Kill(true);
        _ttoTextTweenX.Kill(true);
        _ttoTextColorTween.Kill(true);

        _TTOText.transform.localScale = Vector2.one * 1.25f;
        _TTOText.color = color;

        _ttoTextTweenY = _TTOText.transform.DOScaleY(1f, 0.55f).SetEase(Ease.OutElastic);
        _ttoTextTweenX = _TTOText.transform.DOScaleX(1f, 0.75f).SetEase(Ease.OutBack);
        _ttoTextColorTween = _TTOText.DOColor(ColorISH.Invisible * color, 2f);

        if (sound == null) return;

        SoundSystem.PlayInterfaceSound(new SoundTransporter(sound), volume: 0.35f);
    }

    private IEnumerator OnVotingEndCoroutine(string message)
    {
        _votingEndText.text = string.Empty;

        char[] messageChars = message.ToCharArray();

        foreach (var messageChar in messageChars)
        {
            _votingEndText.text += messageChar;

            SoundSystem.PlayInterfaceSound(new SoundTransporter(_keyboardTyping), volume: 0.3f);

            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }
        }

        _votingEndText.text = message;

        yield return new WaitForSeconds(2.5f);

        for (int ch = 0; ch < messageChars.Length; ch++)
        {
            _votingEndText.text = _votingEndText.text.Remove(_votingEndText.text.Length - 1);

            SoundSystem.PlayInterfaceSound(new SoundTransporter(_keyboardTyping), volume: 0.3f);

            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }
        }

        _votingEndText.text = string.Empty;
    }

    public void SetMapVoting(bool enable, bool animation)
    {
        if (IsVotingActive == enable) return;

        IsVotingActive = enable;

        foreach (var votingVariant in _mapVoting.GetComponentsInChildren<MapVoting>())
        {
            votingVariant.SetActive(enable);
        }

        if (animation)
        {
            _mapVotingColorTween.Kill(true);
            _mapVotingScaleTween.Kill(true);
        }

        if (enable)
        {
            ResultsWindow.Singleton.SetWindow(false);
            _groupsManager.SetGroup(ResultsWindow.Singleton.Window, false, false, false);

            if (animation)
            {
                _mapVoting.transform.localScale = Vector3.one;
                _mapVoting.alpha = 0;

                _mapVotingColorTween = _mapVoting.DOFade(1, 0.6f).SetEase(Ease.OutCirc);
            }
            else
            {
                _mapVoting.transform.localScale = Vector3.one;
                _mapVoting.alpha = 1;
            }

            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            if (animation)
            {
                _mapVoting.alpha = 1;
                _mapVoting.transform.localScale = Vector3.one;

                _mapVotingScaleTween = _mapVoting.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack);
                _mapVotingColorTween = _mapVoting.DOFade(0, 0.5f).SetEase(Ease.OutCirc);
            }
            else
            {
                _mapVoting.transform.localScale = Vector3.one;
                _mapVoting.alpha = 0;
            }

            if (PauseMenu.Singleton.PauseMenuOpened || ResultsWindow.Singleton.IsEnabled) return;

            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void SwitchNicknameVisibility(bool show, string target = "")
    {
        OthersNickname.text = target;
        OthersNickname.gameObject.SetActive(show);
    }

    public void FadeUIGameState(CanvasGameState state)
    {
        (float lobbyEnd, float gameEnd) endValues = state == CanvasGameState.Lobby ? (1, 0) : (0, 1);

        KillGameStateTweens();

        _lobbyGroupTween = _inLobby.DOFade(endValues.lobbyEnd, 0.6f);
        _gameGroupTween = _inGame.DOFade(endValues.gameEnd, 0.6f);
    }

    private void KillGameStateTweens()
    {
        _lobbyGroupTween.Kill(true);
        _gameGroupTween.Kill(true);
    }

    public void SwitchUIGameState(CanvasGameState state)
    {
        if (state == CanvasGameState.Lobby)
        {
            _inLobby.alpha = 1;
            _inGame.alpha = 0;
        }
        else if (state == CanvasGameState.Game)
        {
            _inLobby.alpha = 0;
            _inGame.alpha = 1;
        }
    }

    public void LogKill()
    {
        StartCoroutine(LogKillCoroutine(2, 0.65f));
    }

    private IEnumerator LogKillCoroutine(int staticDuration, float fadeDuration)
    {
        _killLog.alpha = 1;

        yield return new WaitForSeconds(staticDuration);

        _killLogTween = _killLog.DOFade(0, fadeDuration);
    }

    public void StartDeathScreen(ref Action onRespawn)
    {
        StopCoroutine(nameof(RespawnScreenCoroutine));
        StartCoroutine(nameof(RespawnScreenCoroutine), onRespawn);
    }

    public void HideDeathScreen()
    {
        StopCoroutine(nameof(RespawnScreenCoroutine));
        _deathScreen.SetActive(false);
    }

    private IEnumerator RespawnScreenCoroutine(Action onRespawn)
    {
        int easterEgg = UnityEngine.Random.Range(1, 100);

        if (easterEgg == 1)
        {
            _kafifEasterEgg.alpha = 0.5f;
            _kafifEasterEgg.DOFade(0, 0.5f);
        }

        _deathScreen.SetActive(true);
        _deathPhrase.text = _deathPhrases[UnityEngine.Random.Range(0, _deathPhrases.Count)];

        for (int i = 5; i > 0; i--)
        {
            _respawnCountdown.text = $"Respawning in {i}";
            yield return new WaitForSeconds(1f);
        }

        if (onRespawn != null)
        {
            onRespawn.Invoke();
        }

        _deathScreen.SetActive(false);
    }

    private class FadeGroupParams
    {
        public bool FadeIn;
        public float FadeDuration;
        public CanvasGroup Target;

        public FadeGroupParams(bool fadeIn, float fadeDuration, CanvasGroup target)
        {
            FadeIn = fadeIn;
            FadeDuration = fadeDuration;
            Target = target;
        }
    }

    private void ClearSelections()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }

    private void DebugStats()
    {
        for (int i = 0; i < _playerDebugStats.Length; i++)
        {
            switch (i)
            {
                case 0:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerMutationStats.Singleton.Speed}";
                    break;

                case 1:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerMutationStats.Singleton.Bounce}";
                    break;

                case 2:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerMutationStats.Singleton.Luck}";
                    break;

                case 3:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerMutationStats.Singleton.Damage}";
                    break;

                case 4:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerCurrentStats.Singleton.Speed}";
                    break;

                case 5:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerCurrentStats.Singleton.Bounce}";
                    break;

                case 6:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerCurrentStats.Singleton.Luck}";
                    break;

                case 7:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerCurrentStats.Singleton.Damage}";
                    break;
            }
        }
    }
}

public enum CanvasGameState
{
    Lobby,
    Game
}
