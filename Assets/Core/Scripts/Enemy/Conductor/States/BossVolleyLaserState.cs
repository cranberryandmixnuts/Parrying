using UnityEngine;

public sealed class BossVolleyLaserState : BossState
{
    private ConductorMissile prefab;
    private Transform[] muzzles;
    private int volleys;
    private float interval;
    private Collider2D laserCol;
    private float windup;
    private float active;
    private int laserDamage;

    private float timer;
    private float sub;
    private int index;
    private int phase;

    public BossVolleyLaserState(ConductorBoss boss, BossStateMachine fsm) : base(boss, fsm)
    {
    }

    public void Configure(ConductorMissile pf, Transform[] mz, int v, float inter, Collider2D lCol, float wind, float act, int dmg)
    {
        prefab = pf;
        muzzles = mz;
        volleys = v;
        interval = inter;
        laserCol = lCol;
        windup = wind;
        active = act;
        laserDamage = dmg;
    }

    public override void Enter()
    {
        Boss.Play(Boss.MissilePreAnim);
        timer = Boss.AnimLen(Boss.MissilePreAnim);
        sub = 0f;
        index = 0;
        phase = 0;
        Boss.SetLethalLaserP1(false);
    }

    public override void Tick()
    {
        if (phase == 0)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                Boss.Play(Boss.MissileAnim);
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
                Boss.FireMissilesOnce();
                index += 1;
            }
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                Boss.Play(Boss.LaserWindupAnim);
                timer = windup;
                phase = 2;
            }
        }
        else if (phase == 2)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                Boss.Play(Boss.LaserAnim);
                Boss.SetLethalLaserP1(true);
                timer = active;
                phase = 3;
            }
        }
        else if (phase == 3)
        {
            Boss.HandleHitbox(laserCol, laserDamage);
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                Boss.SetLethalLaserP1(false);
                Boss.Play(Boss.LaserRecoverAnim);
                timer = Boss.AnimLen(Boss.LaserRecoverAnim);
                phase = 4;
            }
        }
        else
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) Boss.GoIdle();
        }
    }

    public override void FixedTick()
    {
        Boss.StopHorizontal();
    }

    public override void Exit()
    {
        Boss.SetLethalLaserP1(false);
    }
}
