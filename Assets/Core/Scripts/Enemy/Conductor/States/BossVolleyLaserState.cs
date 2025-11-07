using UnityEngine;

public sealed class BossVolleyLaserState : BossState
{
    private readonly ConductorMissile prefab;
    private readonly Transform[] muzzles;
    private readonly int volleys;
    private readonly float interval;
    private readonly ConductorBoss.LaserConfig laser;

    private float timer;
    private float sub;
    private int index;
    private int phase;

    public override BossStateType StateType => BossStateType.VolleyLaser;

    public BossVolleyLaserState(ConductorBoss boss, BossStateMachine stateMachine, ConductorMissile pf, Transform[] mz, int v, float inter, ConductorBoss.LaserConfig lz) : base(boss, stateMachine)
    {
        prefab = pf;
        muzzles = mz;
        volleys = v;
        interval = inter;
        laser = lz;
    }

    public override void Enter()
    {
        boss.Play(ConductorBoss.AnimFire);
        timer = boss.AnimLen(ConductorBoss.AnimFire);
        sub = 0f;
        index = 0;
        phase = 0;
        boss.SetLethal(ConductorBoss.AttackContext.LaserP1, false);
    }

    public override void Update()
    {
        if (phase == 0)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = volleys * interval + 0.01f;
                sub = 0f;
                index = 0;
                phase = 1;
            }
        }
        else if (phase == 1)
        {
            sub += Time.deltaTime;
            if (index < volleys && sub >= index * interval)
            {
                int n = muzzles != null ? muzzles.Length : 0;
                for (int i = 0; i < n; i++)
                {
                    Transform m = muzzles[i];
                    if (m == null) continue;
                    ConductorMissile proj = Object.Instantiate(prefab, m.position, m.rotation);
                    proj.Initialize(boss, boss.PlayerTarget, m.right);
                }
                index += 1;
            }
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = laser.Windup + laser.Active + boss.AnimLen(ConductorBoss.AnimFire) * 0.25f;
                phase = 2;
                boss.SetLethal(ConductorBoss.AttackContext.LaserP1, true);
            }
        }
        else if (phase == 2)
        {
            boss.HandleHitbox(laser.LaserCollider, laser.Damage);
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                boss.SetLethal(ConductorBoss.AttackContext.LaserP1, false);
                boss.ChangeToIdle(0.6f);
            }
        }
    }

    public override void FixedUpdate()
    {
        boss.StopHorizontal();
    }

    public override void Exit()
    {
        boss.SetLethal(ConductorBoss.AttackContext.LaserP1, false);
    }
}