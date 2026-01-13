using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class KeyBindingButton : MonoBehaviour
{
    [BoxGroup("UI"), SerializeField, Required]
    private KeyBindingVisual visual;

    [BoxGroup("Rebind Target"), SerializeField]
    private string mapName = "Player";

    [BoxGroup("Rebind Target"), SerializeField]
    private string actionName = "Jump";

    [BoxGroup("Rebind Target"), SerializeField, Min(0)]
    private int bindingIndex;

    [BoxGroup("Overlay"), SerializeField, Required]
    private RebindOverlayController overlay;

    private void OnEnable()
    {
        RefreshKeyDisplay();
    }

    public void OnClick_StartRebind()
    {
        if (InputManager.Instance.IsRebinding) return;
        if (overlay.IsOpen) return;

        overlay.BeginRebind(this, mapName, actionName, bindingIndex);
    }

    public void RefreshKeyDisplay()
    {
        InputAction action = InputManager.Instance.Actions.FindAction(mapName + "/" + actionName);

        if (action == null)
            return;

        if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            return;

        visual.Apply(action, bindingIndex);
    }
}