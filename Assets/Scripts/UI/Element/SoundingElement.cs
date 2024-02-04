using UnityEngine;
using UnityEngine.EventSystems;

public class SoundingElement : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, ISelectHandler
{
    [Header("Sounds")]
    [SerializeField] private AudioClip _hoverSound;
    [SerializeField] private AudioClip _clickSound;

    [Space(9)]

    [SerializeField] private bool _selectable;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_selectable) return;

        if (_clickSound) SoundSystem.PlayInterfaceSound(new SoundTransporter(_clickSound), volume: 0.55f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_hoverSound) SoundSystem.PlayInterfaceSound(new SoundTransporter(_hoverSound), volume: 0.55f);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!_selectable) return;

        if (_clickSound) SoundSystem.PlayInterfaceSound(new SoundTransporter(_clickSound), volume: 0.55f);
    }
}
