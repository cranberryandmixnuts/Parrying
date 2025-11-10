using UnityEngine;

public sealed class BossRadialLaserState : BossState
{
    private float introTimer;
    private float sub;

    public override BossStateType StateType => BossStateType.RadialLaser;

    public BossRadialLaserState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine)
    {
    }

    public override void Enter()
    {
        boss.Play(BossController.AnimCrackLaser);
        introTimer = boss.AnimLen(BossController.AnimCrackLaser);
        sub = 0f;
        boss.SetLethal(BossController.AttackContext.LaserP2, false);
    }

    public override void Update()
    {
        if (introTimer > 0f)
        {
            introTimer -= Time.deltaTime;
            if (introTimer > 0f) return;
            boss.SetLethal(BossController.AttackContext.LaserP2, true);
            sub = 0f;
        }

        boss.HandleHitbox(boss.RadialLaserCollider, boss.Settings.radialDamage);
        sub += Time.deltaTime;
        if (sub >= boss.Settings.radialBeat) sub = 0f;
    }

    public override void FixedUpdate()
    {
        boss.StopHorizontal();
    }

    public override void Exit()
    {
        boss.SetLethal(BossController.AttackContext.LaserP2, false);
    }
}