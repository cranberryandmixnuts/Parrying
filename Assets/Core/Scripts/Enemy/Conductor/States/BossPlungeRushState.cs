using UnityEngine;

public sealed class BossPlungeRushState : BossState
{
    private Transform ceiling;
    private Collider2D plungeCol;
    private float teleTime;
    private float activeTime;
    private int plungeDamage;

    private Collider2D rushCol;
    private float rushSpeed;
    private float rushMaxTime;
    private float missBehindTime;
    private int rushDamage;

    private float timer;
    private int phase;
    private float rushDir;
    private float behindTimer;

    public BossPlungeRushState(ConductorBoss boss, BossStateMachine fsm) : base(boss, fsm)
    {
    }

    public void Configure(Transform ceil, Collider2D pCol, float pTele, float pActive, int pDmg, Collider2D rCol, float rSpd, float rMax, float missBehind, int rDmg)
    {
        ceiling = ceil;
        plungeCol = pCol;
        teleTime = pTele;
        activeTime = pActive;
        plungeDamage = pDmg;
        rushCol = rCol;
        rushSpeed = rSpd;
        rushMaxTime = rMax;
        missBehindTime = missBehind;
        rushDamage = rDmg;
    }

    public override void Enter()
    {
        Boss.Teleport(ceiling.position);
        Boss.Play(Boss.PlungeTeleAnim);
        Boss.FaceToPlayer();
        timer = teleTime;
        phase = 0;
        Boss.SetLethalPlunge(false);
        Boss.SetLethalRush(false);
        behindTimer = 0f;
    }

    public override void Tick()
    {
        if (phase == 0)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                Boss.Play(Boss.PlungeAnim);
                Boss.SetLethalPlunge(true);
                timer = activeTime;
                phase = 1;
                rushDir = Boss.FacingDir;
            }
        }
        else if (phase == 1)
        {
            Boss.HandleHitbox(plungeCol, plungeDamage);
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                Boss.SetLethalPlunge(false);
                Boss.Play(Boss.RushAnim);
                Boss.SetLethalRush(true);
                timer = rushMaxTime;
                phase = 2;
            }
        }
        else if (phase == 2)
        {
            Vector3 p = Boss.PlayerTarget.transform.position;
            bool playerBehind = rushDir > 0f ? p.x < Boss.transform.position.x : p.x > Boss.transform.position.x;
            if (playerBehind) behindTimer += Time.deltaTime;
            else behindTimer = 0f;
            Boss.HandleHitbox(rushCol, rushDamage);
            timer -= Time.deltaTime;
            if (behindTimer >= missBehindTime || timer <= 0f)
            {
                Boss.SetLethalRush(false);
                Boss.Play(Boss.RushRecoverAnim);
                timer = Boss.AnimLen(Boss.RushRecoverAnim);
                phase = 3;
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
        if (phase == 2) Boss.SetVelocityX(rushDir * rushSpeed);
        else Boss.StopHorizontal();
    }

    public override void Exit()
    {
        Boss.SetLethalPlunge(false);
        Boss.SetLethalRush(false);
        Boss.StopHorizontal();
    }
}