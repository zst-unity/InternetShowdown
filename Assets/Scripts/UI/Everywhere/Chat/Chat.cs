using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private CanvasGroup _messagesChat;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private GameObject _messagePrefab;
    [SerializeField] private Transform _messagesContainer;
    [SerializeField] private Ease _enableEase;
    [SerializeField] private Ease _disableEase;
    private TweenerCore<float, float, FloatOptions> _fadeTween;
    private TweenerCore<float, float, FloatOptions> _messagesFadeTween;

    private List<string> _chatHistory = new();
    private List<GameObject> _chatHistoryObjects = new();

    public string[] ChatHistory => _chatHistory.ToArray();

    public bool wasFocused;

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
        StopCoroutine(nameof(CO_FadeMessages));
        _chatHistory.Clear();
        _chatHistoryObjects.Clear();
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
        _messagesFadeTween = null;
        _expandedChat.alpha = 0;
        _messagesChat.alpha = 0;
    }

    public void AddMessage(string message)
    {
        var newMessage = Instantiate(_messagePrefab, _messagesContainer);
        _chatHistory.Add(message);
        _chatHistoryObjects.Add(newMessage);

        if (_chatHistory.Count >= 100)
        {
            Destroy(_chatHistoryObjects[0]);
            _chatHistory.RemoveAt(0);
            _chatHistoryObjects.RemoveAt(0);
        }

        newMessage.GetComponent<TMP_Text>().text = message;
        _messagesChat.alpha = 1;
        StartCoroutine(nameof(CO_ForceScrollDown));

        StopCoroutine(nameof(CO_FadeMessages));
        StartCoroutine(nameof(CO_FadeMessages));
    }

    private IEnumerator CO_ForceScrollDown()
    {
        yield return new WaitForEndOfFrame();
        _scrollRect.verticalNormalizedPosition = 0f;
    }

    private IEnumerator CO_FadeMessages()
    {
        _messagesFadeTween?.Complete();
        _messagesChat.alpha = 1;

        yield return new WaitForSeconds(8f);
        _messagesFadeTween = _messagesChat.DOFade(0f, 4.25f);
    }

    public void OnEndEdit()
    {
        if (!Input.GetKeyDown(KeyCode.Return) || string.IsNullOrWhiteSpace(InputField.text)) return;

        print($"sending chat message: {InputField.text}");
        var message = $"<b><color={NetworkPlayer.LocalPlayer.ColorHEX}>{NetworkPlayer.LocalPlayer.Nickname}</color>:</b> {InputField.text}";
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
            StopCoroutine(nameof(CO_FadeMessages));
            _messagesFadeTween?.Complete();
            _messagesChat.alpha = 1f;

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

            StopCoroutine(nameof(CO_FadeMessages));
            StartCoroutine(nameof(CO_FadeMessages));
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
