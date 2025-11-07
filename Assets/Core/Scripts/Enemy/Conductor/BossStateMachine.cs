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
        if (newState == null) throw new System.ArgumentNullException(nameof(newState));
        if (currentState != null) currentState.Exit();
        currentState = newState;
        CurrentStateType = newState.StateType;
        currentState.Enter();
    }

    public void Update()
    {
        if (currentState != null) currentState.Update();
    }

    public void FixedUpdate()
    {
        if (currentState != null) currentState.FixedUpdate();
    }
}