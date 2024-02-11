using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EverywhereUIController : MonoBehaviour
{
    [SerializeField] private GroupsManager _groupsManager;

    public static EverywhereUIController Singleton { get; private set; }

    private void Update()
    {
        PauseMenuCheck();

        if (Input.GetKeyDown(KeyCode.Escape) && _groupsManager.EnabledGroups.Count > 0)
        {
            CanvasGroup target = _groupsManager.EnabledGroups.Last();

            _groupsManager.SetGroup(target, false, true);
            if (_groupsManager.WindowTweens.ContainsKey(target))
            {
                _groupsManager.WindowTweens[target].KillAll();
                _groupsManager.WindowTweens.Remove(target);
            }
        }

        if (Chat.Singleton.Active)
        {
            if (Input.GetKeyDown(KeyCode.T) && !Chat.Singleton.Focused && !PauseMenu.Singleton.PauseMenuOpened)
            {
                Chat.Singleton.SetChat(!Chat.Singleton.Enabled, !EverywhereCanvas.Singleton.IsVotingActive && !ResultsWindow.Singleton.IsEnabled);
            }

            if (!PauseMenu.Singleton.PauseMenuOpened && Chat.Singleton.Enabled && Input.GetMouseButtonDown(0) && !Chat.Singleton.IsPointerOverChat())
            {
                Chat.Singleton.SetChat(false, !EverywhereCanvas.Singleton.IsVotingActive && !ResultsWindow.Singleton.IsEnabled, false);
            }

            if (Input.GetKeyDown(KeyCode.Return) && Chat.Singleton.Enabled && !PauseMenu.Singleton.PauseMenuOpened && string.IsNullOrWhiteSpace(Chat.Singleton.InputField.text))
            {
                Chat.Singleton.InputField.text = "";
                Chat.Singleton.SetChat(false, !EverywhereCanvas.Singleton.IsVotingActive && !ResultsWindow.Singleton.IsEnabled, false);
            }
        }
    }

    private void PauseMenuCheck()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && PauseMenu.Singleton.Active)
        {
            bool WillEnable = !PauseMenu.Singleton.PauseMenuOpened;

            if (!WillEnable && _groupsManager.EnabledGroups.Count > 0)
            {
                if (_groupsManager.EnabledGroups.Last() != PauseMenu.Singleton.PauseMenuGroup) return;
            }

            if (WillEnable && Chat.Singleton.Enabled)
            {
                Chat.Singleton.SetChat(false, !EverywhereCanvas.Singleton.IsVotingActive && !ResultsWindow.Singleton.IsEnabled);
                return;
            }

            PauseMenu.Singleton.Pause(WillEnable, !EverywhereCanvas.Singleton.IsVotingActive && !ResultsWindow.Singleton.IsEnabled && !Chat.Singleton.Enabled);
            _groupsManager.SetGroup(PauseMenu.Singleton.PauseMenuGroup, WillEnable, false, false);
        }
    }

    public void Resume()
    {
        PauseMenu.Singleton.Pause(false, !EverywhereCanvas.Singleton.IsVotingActive && !ResultsWindow.Singleton.IsEnabled && !Chat.Singleton.Enabled);
        _groupsManager.SetGroup(PauseMenu.Singleton.PauseMenuGroup, false, false, false);
    }

    public void ExposeResults()
    {
        ResultsWindow.Singleton.SetWindow(true, !PauseMenu.Singleton.PauseMenuOpened && !EverywhereCanvas.Singleton.IsVotingActive && !Chat.Singleton.Enabled);
        _groupsManager.SetGroup(ResultsWindow.Singleton.Window, true, false, false);
    }

    public void CloseResults()
    {
        ResultsWindow.Singleton.SetWindow(false, !PauseMenu.Singleton.PauseMenuOpened && !EverywhereCanvas.Singleton.IsVotingActive && !Chat.Singleton.Enabled);
        _groupsManager.SetGroup(ResultsWindow.Singleton.Window, false, false, false);
    }

    private void Awake()
    {
        Singleton = this;
        SceneManager.sceneLoaded += (a, b) =>
        {
            if (Chat.Singleton.Active && Chat.Singleton.wasFocused)
            {
                Chat.Singleton.Focus();
            }
        };
    }
}
