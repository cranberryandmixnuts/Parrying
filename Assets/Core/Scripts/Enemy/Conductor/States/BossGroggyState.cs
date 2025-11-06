using UnityEngine;

public sealed class BossGroggyState : BossState
{
    private float timer;

    public BossGroggyState(ConductorBoss boss, BossStateMachine fsm) : base(boss, fsm)
    {
    }

    public void SetDuration(float t)
    {
        timer = t;
    }

    public override void Enter()
    {
        Boss.Play(Boss.GroggyAnim);
    }

    public override void Tick()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        if (Boss.HasP1Stacks) Boss.GoIdle();
        else if (Boss.HasP2Stacks) Boss.GoRadialLaser();
        else Boss.GoDeath();
    }

    public override void FixedTick()
    {
        Boss.StopHorizontal();
    }

    public override void Exit()
    {
    }
}
