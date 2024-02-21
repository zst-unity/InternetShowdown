using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Chat : MonoBehaviour, IEverywhereCanvas
{
    public static Chat Singleton { get; private set; }
    public bool Active { get; set; }
    public bool Enabled { get; private set; }
    public bool Focused => InputField.isFocused;

    [field: SerializeField] public TMP_InputField InputField { get; private set; }
    [SerializeField] private CanvasGroup _expandedChat;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private GameObject _messagePrefab;
    [SerializeField] private Transform _messagesContainer;
    [SerializeField] private Ease _enableEase;
    [SerializeField] private Ease _disableEase;
    private TweenerCore<float, float, FloatOptions> _fadeTween;

    private readonly Dictionary<TMP_Text, MessageData> _messages = new();
    private List<string> _chatHistory = new();

    public string[] ChatHistory => _chatHistory.ToArray();

    public bool wasFocused;

    private class MessageData
    {
        public TweenerCore<Color, Color, ColorOptions> tween = null;
        public Coroutine coroutine = null;
        public bool isNew = true;
    }

    public void Focus()
    {
        wasFocused = true;
        InputField.ActivateInputField();
    }

    public void Unfocus()
    {
        wasFocused = false;
        InputField.OnDeselect(new BaseEventData(EventSystem.current));
    }

    public void OnDisconnect()
    {
        SetChat(false, false, false);
        _chatHistory.Clear();

        foreach (var data in _messages.Values)
        {
            data.tween?.Complete();
            StopCoroutine(data.coroutine);
        }
        _messages.Clear();

        foreach (Transform message in _messagesContainer)
        {
            Destroy(message.gameObject);
        }
    }

    public void ResetCanvas()
    {
        Singleton = this;
        Enabled = false;
        _fadeTween = null;
        _expandedChat.alpha = 0;
    }

    public void AddMessage(string text)
    {
        var messageObject = Instantiate(_messagePrefab, _messagesContainer);
        var message = messageObject.GetComponent<TMP_Text>();

        _chatHistory.Add(text);
        _messages.Add(message, new());

        if (_chatHistory.Count >= 100)
        {
            Destroy(_messages.First().Key.gameObject);

            _chatHistory.RemoveAt(0);
            _messages.Remove(_messages.First().Key);
        }

        message.text = text;
        StartCoroutine(nameof(CO_ForceScrollDown));

        if (!Enabled)
        {
            _messages[message].coroutine = StartCoroutine(nameof(CO_FadeMessage), message);
        }
    }

    private IEnumerator CO_ForceScrollDown()
    {
        yield return new WaitForEndOfFrame();
        _scrollRect.verticalNormalizedPosition = 0f;
    }

    private IEnumerator CO_FadeMessage(TMP_Text message)
    {
        _messages[message].tween?.Complete();
        message.color = Color.white;

        yield return new WaitForSeconds(8f);
        _messages[message].tween = message.DOColor(new(1, 1, 1, 0), 4.25f).OnComplete(() => _messages[message].isNew = false);
    }

    public void OnEndEdit()
    {
        if (!Input.GetKeyDown(KeyCode.Return) || string.IsNullOrWhiteSpace(InputField.text)) return;

        print($"sending chat message: {InputField.text}");
        var color = NetworkPlayer.LocalPlayer.ColorHEX;
        var nickname = NetworkPlayer.LocalPlayer.Nickname;
        var message = NetworkPlayer.LocalPlayer.IsDead ? $"<color=#8F8F8F><i>(dead)</i></color>   <b><color={color}>{nickname}</color>:</b> {InputField.text}" : $"<b><color={color}>{nickname}</color>:</b> {InputField.text}";
        SceneGameManager.Singleton.CmdSendChatMessage(message);

        Unfocus();
        InputField.text = "";
        SetChat(false, !PauseMenu.Singleton.PauseMenuOpened && !EverywhereCanvas.Singleton.IsVotingActive && !ResultsWindow.Singleton.IsEnabled, false);
    }

    public void SetChat(bool enable, bool modifyCursor = true, bool fade = true)
    {
        if (enable == Enabled) return;
        Enabled = enable;
        _fadeTween?.Complete();

        if (enable)
        {
            foreach (var message in _messages)
            {
                StopCoroutine(message.Value.coroutine);
                message.Value.tween?.Kill();
                message.Key.color = Color.white;
            }

            if (fade) _fadeTween = _expandedChat.DOFade(1f, 0.35f).SetEase(_enableEase);
            else _expandedChat.alpha = 1f;

            _expandedChat.interactable = true;
            if (modifyCursor)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            Focus();
        }
        else
        {
            if (fade) _fadeTween = _expandedChat.DOFade(0f, 0.35f).SetEase(_enableEase);
            else _expandedChat.alpha = 0f;

            _expandedChat.interactable = false;
            if (modifyCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            foreach (var message in _messages)
            {
                if (message.Value.isNew) message.Value.coroutine = StartCoroutine(nameof(CO_FadeMessage), message.Key);
                else message.Key.color = new(1, 1, 1, 0);
            }
        }
    }

    public bool IsPointerOverChat()
    {
        return IsPointerOverUIElement(GetEventSystemRaycastResults());
    }

    private bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaycastResults)
    {
        for (int index = 0; index < eventSystemRaycastResults.Count; index++)
        {
            RaycastResult curRaycastResult = eventSystemRaycastResults[index];
            if (curRaycastResult.gameObject.layer == 14)
                return true;
        }
        return false;
    }

    static List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);
        return raycastResults;
    }
}
