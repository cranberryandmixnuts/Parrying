using UnityEngine;

public sealed class BossIdleState : BossState
{
    private readonly bool grounded;
    private float timer;
    private bool zeroed;

    public override BossStateType StateType => BossStateType.Idle;

    public BossIdleState(BossController boss, BossStateMachine stateMachine, bool grounded)
        : base(boss, stateMachine) { this.grounded = grounded; }

    public override void Enter()
    {
        timer = boss.Settings.idleDelay;
        zeroed = false;
        if (grounded)
        {
            boss.SetGravityScale(boss.OriginalGravityScale);
            boss.Play(BossController.AnimGroundIdle);
        }
        else
        {
            boss.SetGravityScale(0f);
            boss.SetVelocityY(0f);
            boss.SetVelocityX(0f);
            boss.Play(BossController.AnimAirIdle);
            zeroed = true;
        }
        boss.StopHorizontal();
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f) boss.DecideP1();
    }

    public override void Exit()
    {
        if (zeroed) boss.SetGravityScale(boss.OriginalGravityScale);
    }
}