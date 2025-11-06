using UnityEngine;

public sealed class BossSwordDropState : BossState
{
    private Transform leftTop;
    private Transform rightTop;
    private Collider2D col;
    private float teleTime;
    private float activeTime;
    private int damage;

    private float timer;
    private int phase;

    public BossSwordDropState(ConductorBoss boss, BossStateMachine fsm) : base(boss, fsm)
    {
    }

    public void Configure(Transform l, Transform r, Collider2D c, float tele, float active, int dmg)
    {
        leftTop = l;
        rightTop = r;
        col = c;
        teleTime = tele;
        activeTime = active;
        damage = dmg;
    }

    public override void Enter()
    {
        Transform t = Boss.ChooseSideTop();
        Boss.Teleport(t.position);
        Boss.Play(Boss.SwordTeleAnim);
        timer = teleTime;
        phase = 0;
        Boss.SetLethalSword(false);
    }

    public override void Tick()
    {
        if (phase == 0)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                Boss.Play(Boss.SwordAnim);
                timer = activeTime;
                phase = 1;
                Boss.SetLethalSword(true);
            }
        }
        else if (phase == 1)
        {
            Boss.HandleHitbox(col, damage);
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                Boss.SetLethalSword(false);
                Boss.Play(Boss.SwordRecoverAnim);
                timer = Boss.AnimLen(Boss.SwordRecoverAnim);
                phase = 2;
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
        Boss.SetLethalSword(false);
    }
}
