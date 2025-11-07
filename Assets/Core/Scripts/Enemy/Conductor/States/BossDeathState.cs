public sealed class BossDeathState : BossState
{
    public override BossStateType StateType
    {
        get { return BossStateType.Death; }
    }

    public BossDeathState(ConductorBoss boss, BossStateMachine stateMachine) : base(boss, stateMachine)
    {
    }

    public override void Enter()
    {
        boss.Die();
    }
}