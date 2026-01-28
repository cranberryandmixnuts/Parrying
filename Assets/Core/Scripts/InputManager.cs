using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

public enum InputMode
{
    Manual,
    Auto
}

public enum ActionKey
{
    Move,
    Jump,
    Dash,
    Parry,
    Heal,
    Escape
}

public sealed class InputManager : Singleton<InputManager, GlobalScope>
{
    private struct AutoButtonState
    {
        public bool Held;
        public bool PreviousHeld;
        public bool PulseDown;
    }

    [TabGroup("Input Service", "Setup"), SerializeField, Required]
    private InputActionAsset actions;

    [TabGroup("Input Service", "Setup"), SerializeField]
    private string playerMapName = "Player";

    [TabGroup("Input Service", "Setup"), SerializeField]
    private string UIMapName = "UI";

    [TabGroup("Input Service", "Action Names"), BoxGroup("Input Service/Action Names/Player"), SerializeField]
    private string moveActionName = "Move";

    [TabGroup("Input Service", "Action Names"), BoxGroup("Input Service/Action Names/Player"), SerializeField]
    private string jumpActionName = "Jump";

    [TabGroup("Input Service", "Action Names"), BoxGroup("Input Service/Action Names/Player"), SerializeField]
    private string dashActionName = "Dash";

    [TabGroup("Input Service", "Action Names"), BoxGroup("Input Service/Action Names/Player"), SerializeField]
    private string parryActionName = "Parry";

    [TabGroup("Input Service", "Action Names"), BoxGroup("Input Service/Action Names/Player"), SerializeField]
    private string healActionName = "Heal";

    [TabGroup("Input Service", "Action Names"), BoxGroup("Input Service/Action Names/UI"), SerializeField]
    private string escapeActionName = "Escape";

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction parryAction;
    private InputAction healAction;
    private InputAction escapeAction;

    private InputActionRebindingExtensions.RebindingOperation currentRebind;

    private InputAction currentRebindAction;
    private int currentRebindBindingIndex;
    private bool currentRebindExcludeMouse;

    private InputMode moveMode = InputMode.Manual;
    private InputMode jumpMode = InputMode.Manual;
    private InputMode dashMode = InputMode.Manual;
    private InputMode parryMode = InputMode.Manual;
    private InputMode healMode = InputMode.Manual;
    private InputMode escapeMode = InputMode.Manual;

    private float autoMoveAxis;

    private AutoButtonState autoJump;
    private AutoButtonState autoDash;
    private AutoButtonState autoParry;
    private AutoButtonState autoHeal;
    private AutoButtonState autoEscape;

    private const string RebindsKey = "InputService_Rebinds";

    public float MoveAxis { get; private set; }

    public bool JumpDown { get; private set; }
    public bool JumpUp { get; private set; }
    public bool JumpHeld { get; private set; }

    public bool DashDown { get; private set; }

    public bool ParryDown { get; private set; }
    public bool ParryHeld { get; private set; }

    public bool HealHeld { get; private set; }

    public bool EscapeDown { get; private set; }

    public bool IsRebinding { get; private set; }

    public InputActionAsset Actions { get { return actions; } }

    public event Action OnRebindStarted;
    public event Action OnRebindCompleted;
    public event Action OnRebindCanceled;

    protected override void SingletonAwake()
    {
        InitializeActions();
        LoadBindingOverrides();
    }

    private void OnEnable()
    {
        if (Instance != this) return;
        EnableActions(true);
    }

    private void OnDisable()
    {
        if (Instance != this) return;
        EnableActions(false);
    }

    private void Update()
    {
        if (IsRebinding)
        {
            MoveAxis = 0f;

            JumpDown = false;
            JumpUp = false;
            JumpHeld = false;

            DashDown = false;

            ParryDown = false;
            ParryHeld = false;

            HealHeld = false;

            EscapeDown = false;

            StabilizeAutoDuringRebind();
            return;
        }

        if (moveMode == InputMode.Manual)
        {
            Vector2 move = moveAction.ReadValue<Vector2>();
            MoveAxis = Mathf.Clamp(move.x, -1f, 1f);
        }
        else
        {
            MoveAxis = Mathf.Clamp(autoMoveAxis, -1f, 1f);
        }

        if (jumpMode == InputMode.Manual)
        {
            JumpDown = jumpAction.WasPressedThisFrame();
            JumpUp = jumpAction.WasReleasedThisFrame();
            JumpHeld = jumpAction.IsPressed();
        }
        else
        {
            EvaluateAutoButton(ref autoJump, out bool down, out bool up, out bool held);
            JumpDown = down;
            JumpUp = up;
            JumpHeld = held;
        }

        if (dashMode == InputMode.Manual)
        {
            DashDown = dashAction.WasPressedThisFrame();
        }
        else
        {
            EvaluateAutoButton(ref autoDash, out bool down, out _, out _);
            DashDown = down;
        }

        if (parryMode == InputMode.Manual)
        {
            ParryDown = parryAction.WasPressedThisFrame();
            ParryHeld = parryAction.IsPressed();
        }
        else
        {
            EvaluateAutoButton(ref autoParry, out bool down, out _, out bool held);
            ParryDown = down;
            ParryHeld = held;
        }

        if (healMode == InputMode.Manual)
        {
            HealHeld = healAction.IsPressed();
        }
        else
        {
            EvaluateAutoButton(ref autoHeal, out _, out _, out bool held);
            HealHeld = held;
        }

        if (escapeMode == InputMode.Manual)
        {
            EscapeDown = escapeAction.WasPressedThisFrame();
        }
        else
        {
            EvaluateAutoButton(ref autoEscape, out bool down, out _, out _);
            EscapeDown = down;
        }
    }

    public InputMode GetMode(ActionKey key)
    {
        return key switch
        {
            ActionKey.Move => moveMode,
            ActionKey.Jump => jumpMode,
            ActionKey.Dash => dashMode,
            ActionKey.Parry => parryMode,
            ActionKey.Heal => healMode,
            ActionKey.Escape => escapeMode,
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }

    public void SetMode(ActionKey key, InputMode mode)
    {
        switch (key)
        {
            case ActionKey.Move:
                moveMode = mode;
                autoMoveAxis = 0f;
                break;
            case ActionKey.Jump:
                jumpMode = mode;
                ResetAutoButton(ref autoJump);
                break;
            case ActionKey.Dash:
                dashMode = mode;
                ResetAutoButton(ref autoDash);
                break;
            case ActionKey.Parry:
                parryMode = mode;
                ResetAutoButton(ref autoParry);
                break;
            case ActionKey.Heal:
                healMode = mode;
                ResetAutoButton(ref autoHeal);
                break;
            case ActionKey.Escape:
                escapeMode = mode;
                ResetAutoButton(ref autoEscape);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(key), key, null);
        }
    }

    public void SetAllModes(InputMode mode)
    {
        SetMode(ActionKey.Move, mode);
        SetMode(ActionKey.Jump, mode);
        SetMode(ActionKey.Dash, mode);
        SetMode(ActionKey.Parry, mode);
        SetMode(ActionKey.Heal, mode);
        SetMode(ActionKey.Escape, mode);
    }

    public void SetAutoMoveAxis(float axis)
    {
        autoMoveAxis = Mathf.Clamp(axis, -1f, 1f);
    }

    public void SetAutoHeld(ActionKey key, bool held)
    {
        switch (key)
        {
            case ActionKey.Jump:
                autoJump.Held = held;
                break;
            case ActionKey.Dash:
                autoDash.Held = held;
                break;
            case ActionKey.Parry:
                autoParry.Held = held;
                break;
            case ActionKey.Heal:
                autoHeal.Held = held;
                break;
            case ActionKey.Escape:
                autoEscape.Held = held;
                break;
            case ActionKey.Move:
            default:
                throw new ArgumentOutOfRangeException(nameof(key), key, null);
        }
    }

    public void TriggerAutoDown(ActionKey key)
    {
        switch (key)
        {
            case ActionKey.Jump:
                autoJump.PulseDown = true;
                break;
            case ActionKey.Dash:
                autoDash.PulseDown = true;
                break;
            case ActionKey.Parry:
                autoParry.PulseDown = true;
                break;
            case ActionKey.Escape:
                autoEscape.PulseDown = true;
                break;
            case ActionKey.Heal:
            case ActionKey.Move:
            default:
                throw new ArgumentOutOfRangeException(nameof(key), key, null);
        }
    }

    private void StabilizeAutoDuringRebind()
    {
        StabilizeAutoButtonDuringRebind(ref autoJump);
        StabilizeAutoButtonDuringRebind(ref autoDash);
        StabilizeAutoButtonDuringRebind(ref autoParry);
        StabilizeAutoButtonDuringRebind(ref autoHeal);
        StabilizeAutoButtonDuringRebind(ref autoEscape);
    }

    private static void ResetAutoButton(ref AutoButtonState state)
    {
        state.Held = false;
        state.PreviousHeld = false;
        state.PulseDown = false;
    }

    private static void StabilizeAutoButtonDuringRebind(ref AutoButtonState state)
    {
        state.PulseDown = false;
        state.PreviousHeld = state.Held;
    }

    private static void EvaluateAutoButton(ref AutoButtonState state, out bool down, out bool up, out bool held)
    {
        held = state.Held;
        down = state.PulseDown || (!state.PreviousHeld && held);
        up = state.PreviousHeld && !held;
        state.PreviousHeld = held;
        state.PulseDown = false;
    }

    private void InitializeActions()
    {
        moveAction = FindAction(playerMapName, moveActionName);
        jumpAction = FindAction(playerMapName, jumpActionName);
        dashAction = FindAction(playerMapName, dashActionName);
        parryAction = FindAction(playerMapName, parryActionName);
        healAction = FindAction(playerMapName, healActionName);

        escapeAction = FindAction(UIMapName, escapeActionName);
    }

    private InputAction FindAction(string mapName, string actionName) => actions.FindAction(mapName + "/" + actionName);

    private void EnableActions(bool enable)
    {
        if (enable) actions.Enable();
        else actions.Disable();
    }

    public void SaveBindingOverrides()
    {
        string json = actions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(RebindsKey, json);
        PlayerPrefs.Save();
    }

    public void LoadBindingOverrides()
    {
        Debug.Log("Loading input binding overrides.");
        if (!PlayerPrefs.HasKey(RebindsKey))
            return;

        string json = PlayerPrefs.GetString(RebindsKey);
        actions.LoadBindingOverridesFromJson(json);
    }

    public void ClearBindingOverrides()
    {
        actions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(RebindsKey);
    }

    public void CancelCurrentRebind()
    {
        if (currentRebind == null)
            return;

        currentRebind.Cancel();
    }

    public void StartRebind(string mapName, string actionName, int bindingIndex)
    {
        InputAction action = FindAction(mapName, actionName);

        if (action == null)
            return;

        if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            return;

        currentRebind?.Cancel();

        IsRebinding = true;

        currentRebindAction = action;
        currentRebindBindingIndex = bindingIndex;
        currentRebindExcludeMouse = false;

        action.Disable();

        OnRebindStarted?.Invoke();

        currentRebind = BuildRebindOperation(action, bindingIndex, currentRebindExcludeMouse);
        currentRebind.Start();
    }

    public void SetCurrentRebindExcludeMouse(bool excludeMouse)
    {
        if (!IsRebinding)
            return;

        if (currentRebind == null)
            return;

        if (currentRebindAction == null)
            return;

        if (currentRebindExcludeMouse == excludeMouse)
            return;

        currentRebindExcludeMouse = excludeMouse;

        currentRebind.Dispose();
        currentRebind = null;

        currentRebind = BuildRebindOperation(currentRebindAction, currentRebindBindingIndex, currentRebindExcludeMouse);
        currentRebind.Start();
    }

    private InputActionRebindingExtensions.RebindingOperation BuildRebindOperation(InputAction action, int bindingIndex, bool excludeMouse)
    {
        var operation = action.PerformInteractiveRebinding(bindingIndex);

        if (excludeMouse) operation.WithControlsExcluding("Mouse");

        operation.OnMatchWaitForAnother(0.1f);

        return operation
            .OnComplete(o => FinishRebind(action))
            .OnCancel(o => CancelRebind(action));
    }

    private void FinishRebind(InputAction action)
    {
        action.Enable();

        currentRebind.Dispose();
        currentRebind = null;

        currentRebindAction = null;

        IsRebinding = false;

        SaveBindingOverrides();
        OnRebindCompleted?.Invoke();
    }

    private void CancelRebind(InputAction action)
    {
        action.Enable();

        currentRebind.Dispose();
        currentRebind = null;

        currentRebindAction = null;

        IsRebinding = false;

        OnRebindCanceled?.Invoke();
    }
}