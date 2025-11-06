using UnityEngine;

public sealed class BossRadialLaserState : BossState
{
    private Collider2D radialCol;
    private int sets;
    private float beat;
    private float activeEach;
    private int damage;

    private float timer;
    private float sub;

    public BossRadialLaserState(ConductorBoss boss, BossStateMachine fsm) : base(boss, fsm)
    {
    }

    public void Configure(Collider2D col, int s, float b, float a, int dmg)
    {
        radialCol = col;
        sets = s;
        beat = b;
        activeEach = a;
        damage = dmg;
    }

    public override void Enter()
    {
        Boss.Play(Boss.P2IntroAnim);
        timer = Boss.AnimLen(Boss.P2IntroAnim);
        sub = 0f;
    }

    public override void Tick()
    {
        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            Boss.Play(Boss.P2RadialAnim);
            Boss.SetLethalLaserP2(true);
            sub = 0f;
        }

        Boss.HandleHitbox(radialCol, damage);
        sub += Time.deltaTime;
        if (sub >= beat) sub = 0f;
    }

    public override void FixedTick()
    {
        Boss.StopHorizontal();
    }

    public override void Exit()
    {
        Boss.SetLethalLaserP2(false);
    }
}
