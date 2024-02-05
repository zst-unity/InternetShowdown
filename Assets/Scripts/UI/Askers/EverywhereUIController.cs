using System.Linq;
using UnityEngine;

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

        if (Chat.Singleton.Active && Input.GetKeyDown(KeyCode.T) && !Chat.Singleton.Focused && !PauseMenu.Singleton.PauseMenuOpened)
        {
            Chat.Singleton.SetChat(!Chat.Singleton.Enabled, !PauseMenu.Singleton.PauseMenuOpened && !EverywhereCanvas.Singleton.IsVotingActive && !ResultsWindow.Singleton.IsEnabled);
        }

        if (Chat.Singleton.Active && Chat.Singleton.Enabled && Input.GetMouseButtonDown(0) && !Chat.Singleton.IsPointerOverChat())
        {
            Chat.Singleton.SetChat(false, !PauseMenu.Singleton.PauseMenuOpened && !EverywhereCanvas.Singleton.IsVotingActive && !ResultsWindow.Singleton.IsEnabled, false);
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

            PauseMenu.Singleton.Pause(WillEnable, !EverywhereCanvas.Singleton.IsVotingActive && !ResultsWindow.Singleton.IsEnabled);
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
    }
}
