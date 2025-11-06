using UnityEngine;

public sealed class BossIdleState : BossState
{
    private float timer;

    public BossIdleState(ConductorBoss boss, BossStateMachine fsm) : base(boss, fsm)
    {
    }

    public void SetDelay(float t)
    {
        timer = t;
    }

    public override void Enter()
    {
        Boss.Play(Boss.IdleAnim);
        if (timer <= 0f) timer = 0.4f;
    }

    public override void Tick()
    {
        Boss.FaceToPlayer();
        timer -= Time.deltaTime;
        if (timer <= 0f) Boss.ChooseP1();
    }

    public override void FixedTick()
    {
        Boss.StopHorizontal();
    }

    public override void Exit()
    {
    }
}
