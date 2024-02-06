using UnityEngine;

public class HudHider : MonoBehaviour, IEverywhereCanvas
{
    [SerializeField] private CanvasGroup[] _hudToHide;
    private bool _hidden;

    public bool Active { get; set; }

    public void OnDisconnect()
    {
        _hidden = false;
        SetHUD(1);
    }

    public void ResetCanvas()
    {
        _hidden = false;
        SetHUD(1);
    }

    private void Update()
    {
        if (!Active) return;

        if (Input.GetKeyDown(KeyCode.F1))
        {
            _hidden = !_hidden;
            SetHUD(_hidden ? 0f : 1f);
        }
    }

    private void SetHUD(float alpha)
    {
        foreach (var hud in _hudToHide)
        {
            hud.alpha = alpha;
        }
    }
}
