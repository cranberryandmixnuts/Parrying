using UnityEngine;

public sealed class BossGroggyState : BossState
{
    private float timer;

    public override BossStateType StateType => BossStateType.Groggy;

    public BossGroggyState(ConductorBoss boss, BossStateMachine stateMachine, float duration) : base(boss, stateMachine)
    {
        timer = duration;
    }

    public override void Enter()
    {
        boss.Play(ConductorBoss.AnimGroggy);
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        if (boss.HasP1Stacks) boss.ChangeToIdle(0.5f);
        else if (boss.HasP2Stacks) boss.ChangeToRadialLaser();
        else boss.ChangeToDeath();
    }

    public override void FixedUpdate()
    {
        boss.StopHorizontal();
    }
}