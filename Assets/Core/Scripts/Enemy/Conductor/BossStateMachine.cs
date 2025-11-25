using UnityEngine;

public sealed class BossStateMachine
{
    private BossState currentState;
    public BossStateType CurrentStateType { get; private set; } = BossStateType.Missing;

    public void Initialize(BossState startState)
    {
        currentState = startState;
        CurrentStateType = startState.StateType;
        currentState.Enter();
    }

    public void ChangeState(BossState newState)
    {
        currentState.Exit();
        currentState = newState;
        CurrentStateType = newState.StateType;
        currentState.Enter();
    }

    public void Update()
    {
        currentState.Update();
    }

    public void FixedUpdate()
    {
        currentState.FixedUpdate();
    }
}