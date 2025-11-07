using UnityEngine;

public sealed class BossPlungeRushState : BossState
{
    private readonly Transform ceiling;
    private readonly Collider2D plungeCol;
    private readonly float teleTime;
    private readonly float activeTime;
    private readonly int plungeDamage;

    private readonly Collider2D rushCol;
    private readonly float rushSpeed;
    private readonly float rushMaxTime;
    private readonly float missBehindTime;
    private readonly int rushDamage;

    private float timer;
    private int phase;
    private float rushDir;
    private float behindTimer;

    public override BossStateType StateType => BossStateType.PlungeRush;

    public BossPlungeRushState(ConductorBoss boss, BossStateMachine stateMachine, Transform ceil, Collider2D pCol, float pTele, float pActive, int pDmg, Collider2D rCol, float rSpd, float rMax, float missBehind, int rDmg) : base(boss, stateMachine)
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
        boss.Teleport(ceiling.position);
        boss.Play(ConductorBoss.AnimSwoop);
        boss.FaceToPlayer();
        timer = teleTime;
        phase = 0;
        boss.SetLethal(ConductorBoss.AttackContext.Plunge, false);
        boss.SetLethal(ConductorBoss.AttackContext.Rush, false);
        behindTimer = 0f;
    }

    public override void Update()
    {
        if (phase == 0)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                boss.SetLethal(ConductorBoss.AttackContext.Plunge, true);
                timer = activeTime;
                phase = 1;
                rushDir = boss.FacingDir;
            }
        }
        else if (phase == 1)
        {
            boss.HandleHitbox(plungeCol, plungeDamage);
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                boss.SetLethal(ConductorBoss.AttackContext.Plunge, false);
                boss.Play(ConductorBoss.AnimGroundRush);
                boss.SetLethal(ConductorBoss.AttackContext.Rush, true);
                timer = rushMaxTime;
                phase = 2;
            }
        }
        else if (phase == 2)
        {
            Vector3 p = boss.PlayerTarget.transform.position;
            bool playerBehind = rushDir > 0f ? p.x < boss.transform.position.x : p.x > boss.transform.position.x;
            if (playerBehind) behindTimer += Time.deltaTime; else behindTimer = 0f;
            boss.HandleHitbox(rushCol, rushDamage);
            timer -= Time.deltaTime;
            if (behindTimer >= missBehindTime || timer <= 0f)
            {
                boss.SetLethal(ConductorBoss.AttackContext.Rush, false);
                timer = boss.AnimLen(ConductorBoss.AnimGroundRush);
                phase = 3;
            }
        }
        else
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) boss.ChangeToIdle(0.6f);
        }
    }

    public override void FixedUpdate()
    {
        if (phase == 2) boss.SetVelocityX(rushDir * rushSpeed); else boss.StopHorizontal();
    }

    public override void Exit()
    {
        boss.SetLethal(ConductorBoss.AttackContext.Plunge, false);
        boss.SetLethal(ConductorBoss.AttackContext.Rush, false);
        boss.StopHorizontal();
    }
}