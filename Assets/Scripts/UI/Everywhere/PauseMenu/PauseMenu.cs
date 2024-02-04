using System;
using System.Collections;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Mirror;
using UnityEngine;

public class PauseMenu : MonoBehaviour, IEverywhereCanvas
{
    public static PauseMenu Singleton { get; private set; }

    [field: SerializeField] public CanvasGroup PauseMenuGroup { get; private set; }
    [SerializeField] private CanvasGroup _settingsWindow;

    private GroupsManager _groupsManager;

    private TweenerCore<float, float, FloatOptions> _pauseMenuTween;

    [HideInInspector] public bool PauseMenuOpened { get; private set; }

    public bool Active { get; set; }

    public void Reset()
    {
        Singleton = this;

        PauseMenuGroup.alpha = 0;
        PauseMenuOpened = false;
    }

    public void Pause(bool enable, bool modifyCursor = true)
    {
        if (!NetworkManager.singleton.isNetworkActive) return;

        _pauseMenuTween.Kill(true);

        PauseMenuOpened = enable;

        if (enable)
        {
            if (modifyCursor) Cursor.lockState = CursorLockMode.None;

            _pauseMenuTween = PauseMenuGroup.DOFade(1, 0.3f).SetEase(Ease.InOutCubic);
        }
        else
        {
            if (modifyCursor) Cursor.lockState = CursorLockMode.Locked;

            _pauseMenuTween = PauseMenuGroup.DOFade(0, 0.3f).SetEase(Ease.InOutCubic);
        }
    }

    public void OnDisconnect()
    {
        Pause(false, false);
    }
}
