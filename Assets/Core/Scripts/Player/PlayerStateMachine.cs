public class PlayerStateMachine
{
    private PlayerState currentState;
    private PlayerState previousState;

    public PlayerStateType CurrentStateType => currentState?.StateType ?? PlayerStateType.Missing;

    public void Initialize(PlayerState startingState)
    {
        currentState = startingState;
        currentState.Enter();
    }

    public void ChangeState(PlayerState newState)
    {
        if (currentState != null && !currentState.CanTransitionTo(newState))
            return;

        currentState?.Exit();
        previousState = currentState;
        currentState = newState;
        //UnityEngine.Debug.Log($"Changing state from {previousState?.StateType} to {currentState.StateType}");
        currentState.Enter();
    }

    public void Update()
    {
        currentState?.Update();
    }

    public void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }
}