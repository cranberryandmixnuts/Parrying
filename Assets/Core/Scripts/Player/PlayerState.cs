public enum PlayerStateType
{
    Missing,
    Locomotion,
    Dash,
    Parry,
    CounterParry,
    Heal,
    Hit,
    Death
}

public abstract class PlayerState
{
    protected PlayerController player;
    protected PlayerStateMachine stateMachine;

    public abstract PlayerStateType StateType { get; }

    public PlayerState(PlayerController player, PlayerStateMachine stateMachine)
    {
        this.player = player;
        this.stateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
}