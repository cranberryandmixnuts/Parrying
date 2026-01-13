using UnityEngine;
using Sirenix.OdinInspector;

public sealed class RebindOverlayController : MonoBehaviour
{
    [SerializeField, Required]
    private GameObject overlayRoot;

    private KeyBindingButton currentOwner;

    public bool IsOpen => overlayRoot.activeSelf;

    private void OnEnable()
    {
        InputManager.Instance.OnRebindCompleted += HandleRebindEnded;
        InputManager.Instance.OnRebindCanceled += HandleRebindEnded;
    }

    private void OnDisable()
    {
        InputManager.Instance.OnRebindCompleted -= HandleRebindEnded;
        InputManager.Instance.OnRebindCanceled -= HandleRebindEnded;
    }

    public void BeginRebind(KeyBindingButton owner, string mapName, string actionName, int bindingIndex)
    {
        if (IsOpen) return;

        currentOwner = owner;
        overlayRoot.SetActive(true);

        InputManager.Instance.StartRebind(mapName, actionName, bindingIndex);
        InputManager.Instance.SetCurrentRebindExcludeMouse(false);
    }

    public void CancelRebindAndClose()
    {
        if (!IsOpen) return;

        InputManager.Instance.CancelCurrentRebind();

        overlayRoot.SetActive(false);
        currentOwner = null;
    }

    public void SetCancelButtonSelected(bool selected)
    {
        if (!IsOpen) return;

        InputManager.Instance.SetCurrentRebindExcludeMouse(selected);
    }

    private void HandleRebindEnded()
    {
        overlayRoot.SetActive(false);

        if (currentOwner != null)
            currentOwner.RefreshKeyDisplay();

        currentOwner = null;
    }
}