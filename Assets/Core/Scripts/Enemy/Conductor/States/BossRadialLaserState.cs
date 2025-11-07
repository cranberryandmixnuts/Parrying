using UnityEngine;

public sealed class BossRadialLaserState : BossState
{
    private float introTimer;
    private float sub;

    public override BossStateType StateType => BossStateType.RadialLaser;

    public BossRadialLaserState(ConductorBoss boss, BossStateMachine stateMachine) : base(boss, stateMachine)
    {
    }

    public override void Enter()
    {
        boss.Play(ConductorBoss.AnimCrackLaser);
        introTimer = boss.AnimLen(ConductorBoss.AnimCrackLaser);
        sub = 0f;
        boss.SetLethal(ConductorBoss.AttackContext.LaserP2, false);
    }

    public override void Update()
    {
        if (introTimer > 0f)
        {
            introTimer -= Time.deltaTime;
            if (introTimer > 0f) return;
            boss.SetLethal(ConductorBoss.AttackContext.LaserP2, true);
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
        boss.SetLethal(ConductorBoss.AttackContext.LaserP2, false);
    }
}