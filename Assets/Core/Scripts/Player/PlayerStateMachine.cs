using UnityEngine;

public sealed class PlayerStateMachine
{
    private PlayerState currentState;
    public PlayerStateType CurrentStateType { get; private set; } = PlayerStateType.Missing;

    public void Initialize(PlayerState startState)
    {
        currentState = startState;
        CurrentStateType = startState.StateType;
        currentState.Enter();
    }

    public void ChangeState(PlayerState newState)
    {
        if (newState == null)
            throw new System.ArgumentNullException(nameof(newState));

        if (currentState != null)
            currentState.Exit();

        currentState = newState;
        CurrentStateType = newState.StateType;
        currentState.Enter();
    }

    public void Update()
    {
        if (currentState != null)
            currentState.Update();
    }

    public void FixedUpdate()
    {
        if (currentState != null)
            currentState.FixedUpdate();
    }
}
