public sealed class BossDeathState : BossState
{
    public override BossStateType StateType => BossStateType.Death;

    public BossDeathState(BossController boss, BossStateMachine stateMachine)
        : base(boss, stateMachine) { }

    public override void Enter()
    {
        boss.CancelTeleportEffects();
        boss.Die();
    }
}