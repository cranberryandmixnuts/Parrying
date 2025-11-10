using UnityEngine;

public sealed class BossVolleyLaserState : BossState
{
    private float timer;
    private float sub;
    private int index;
    private int phase;

    public override BossStateType StateType => BossStateType.VolleyLaser;

    public BossVolleyLaserState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine)
    {
    }

    public override void Enter()
    {
        boss.Play(BossController.AnimFire);
        timer = boss.AnimLen(BossController.AnimFire);
        sub = 0f;
        index = 0;
        phase = 0;
        boss.SetLethal(BossController.AttackContext.LaserP1, false);
    }

    public override void Update()
    {
        if (phase == 0)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = boss.Settings.missileVolleys * boss.Settings.missileVolleyInterval + 0.01f;
                sub = 0f;
                index = 0;
                phase = 1;
            }
            return;
        }

        if (phase == 1)
        {
            sub += Time.deltaTime;
            if (index < boss.Settings.missileVolleys && sub >= index * boss.Settings.missileVolleyInterval)
            {
                int n = boss.MissileMuzzles != null ? boss.MissileMuzzles.Length : 0;
                for (int i = 0; i < n; i++)
                {
                    Transform m = boss.MissileMuzzles[i];
                    if (m == null) continue;
                    ConductorMissile proj = Object.Instantiate(boss.MissilePrefab, m.position, m.rotation);
                    proj.Initialize(boss, boss.PlayerTarget, m.right);
                }
                index += 1;
            }
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = boss.Settings.laserWindupTime + boss.Settings.laserActiveTime + boss.AnimLen(BossController.AnimFire) * 0.25f;
                phase = 2;
                boss.SetLethal(BossController.AttackContext.LaserP1, true);
            }
            return;
        }

        if (phase == 2)
        {
            boss.HandleHitbox(boss.ChestLaserCollider, boss.Settings.laserDamage);
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                boss.SetLethal(BossController.AttackContext.LaserP1, false);
                boss.ChangeToIdle(false);
            }
        }
    }

    public override void FixedUpdate()
    {
        boss.StopHorizontal();
    }

    public override void Exit()
    {
        boss.SetLethal(BossController.AttackContext.LaserP1, false);
    }
}