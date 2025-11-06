using UnityEngine;

public abstract class BossState
{
    protected readonly ConductorBoss Boss;
    protected readonly BossStateMachine Fsm;

    protected BossState(ConductorBoss boss, BossStateMachine fsm)
    {
        Boss = boss;
        Fsm = fsm;
    }

    public abstract void Enter();
    public abstract void Tick();
    public abstract void FixedTick();
    public abstract void Exit();
}
