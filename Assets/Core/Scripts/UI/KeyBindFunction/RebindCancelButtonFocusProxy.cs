using UnityEngine;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;

public sealed class RebindCancelButtonFocusProxy : MonoBehaviour,
    ISelectHandler,
    IDeselectHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler
{
    [SerializeField, Required]
    private RebindOverlayController overlay;

    public void OnSelect(BaseEventData eventData) => overlay.SetCancelButtonSelected(true);

    public void OnDeselect(BaseEventData eventData) => overlay.SetCancelButtonSelected(false);

    public void OnPointerEnter(PointerEventData eventData) => overlay.SetCancelButtonSelected(true);

    public void OnPointerExit(PointerEventData eventData) => overlay.SetCancelButtonSelected(false);

    public void OnPointerDown(PointerEventData eventData) => overlay.SetCancelButtonSelected(true);
}