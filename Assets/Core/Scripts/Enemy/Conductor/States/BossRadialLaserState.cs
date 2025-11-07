using UnityEngine;

public sealed class BossRadialLaserState : BossState
{
    private readonly Collider2D radialCol;
    private readonly int sets;
    private readonly float beat;
    private readonly float activeEach;
    private readonly int damage;

    private float introTimer;
    private float sub;

    public override BossStateType StateType => BossStateType.RadialLaser;

    public BossRadialLaserState(ConductorBoss boss, BossStateMachine stateMachine, Collider2D col, int s, float b, float a, int dmg) : base(boss, stateMachine)
    {
        radialCol = col;
        sets = s;
        beat = b;
        activeEach = a;
        damage = dmg;
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

        boss.HandleHitbox(radialCol, damage);
        sub += Time.deltaTime;
        if (sub >= beat) sub = 0f;
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