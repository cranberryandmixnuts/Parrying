using Sirenix.OdinInspector;
using UnityEngine;

public sealed class KeyBindingButton : MonoBehaviour
{
    [BoxGroup("UI"), SerializeField, Required]
    private KeyBindingVisual visual;

    [BoxGroup("Rebind Target"), SerializeField]
    private string mapName;

    [BoxGroup("Rebind Target"), SerializeField]
    private string actionName;

    [BoxGroup("Rebind Target"), SerializeField, Min(0)]
    private int bindingIndex = 0;

    [BoxGroup("Overlay"), SerializeField, Required]
    private RebindOverlayController overlay;

    private void OnEnable()
    {
        InputManager.Instance.OnRebindCompleted += RefreshKeyDisplay;
        RefreshKeyDisplay();
    }

    private void OnDisable() => InputManager.Instance.OnRebindCompleted -= RefreshKeyDisplay;

    public void OnClick_StartRebind()
    {
        if (InputManager.Instance.IsRebinding) return;
        if (overlay.IsOpen) return;

        overlay.BeginRebind(this, mapName, actionName, bindingIndex);
    }

    public void RefreshKeyDisplay() => visual.Apply(InputManager.Instance.Actions.FindAction(mapName + "/" + actionName), bindingIndex);
}