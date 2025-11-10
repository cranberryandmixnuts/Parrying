public enum BossStateType
{
    Missing,
    Idle,
    Groggy,
    SwordDrop,
    PlungeRush,
    VolleyLaser,
    RadialLaser,
    Death
}


public abstract class BossState
{
    protected readonly BossController boss;
    protected readonly BossStateMachine stateMachine;

    public abstract BossStateType StateType { get; }

    public BossState(BossController boss, BossStateMachine stateMachine)
    {
        this.boss = boss;
        this.stateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
}