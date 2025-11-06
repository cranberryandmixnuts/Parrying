using UnityEngine;

public sealed class BossDeathState : BossState
{
    public BossDeathState(ConductorBoss boss, BossStateMachine fsm) : base(boss, fsm)
    {
    }

    public override void Enter()
    {
        Boss.Die();
    }

    public override void Tick()
    {
    }

    public override void FixedTick()
    {
    }

    public override void Exit()
    {
    }
}
