using UnityEngine;

public sealed class BossIdleState : BossState
{
    private float timer;

    public override BossStateType StateType => BossStateType.Idle;

    public BossIdleState(ConductorBoss boss, BossStateMachine stateMachine, float delay) : base(boss, stateMachine)
    {
        timer = delay;
    }

    public override void Enter()
    {
        boss.Play(ConductorBoss.AnimGroundIdle);
        if (timer <= 0f) timer = 0.4f;
    }

    public override void Update()
    {
        boss.FaceToPlayer();
        timer -= Time.deltaTime;
        if (timer <= 0f) boss.DecideP1();
    }

    public override void FixedUpdate()
    {
        boss.StopHorizontal();
    }
}