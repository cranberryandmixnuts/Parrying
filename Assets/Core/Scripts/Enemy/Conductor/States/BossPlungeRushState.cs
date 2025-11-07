using UnityEngine;

public sealed class BossPlungeRushState : BossState
{
    private float timer;
    private int phase;
    private float rushDir;
    private float behindTimer;

    public override BossStateType StateType => BossStateType.PlungeRush;

    public BossPlungeRushState(ConductorBoss boss, BossStateMachine stateMachine) : base(boss, stateMachine)
    {
    }

    public override void Enter()
    {
        boss.Teleport(boss.CeilingPoint.position);
        boss.Play(ConductorBoss.AnimSwoop);
        boss.FaceToPlayer();
        timer = boss.Settings.plungeTeleTime;
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
                timer = boss.Settings.plungeActiveTime;
                phase = 1;
                rushDir = boss.FacingDir;
            }
            return;
        }

        if (phase == 1)
        {
            boss.HandleHitbox(boss.PlungeCollider, boss.Settings.plungeDamage);
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                boss.SetLethal(ConductorBoss.AttackContext.Plunge, false);
                boss.Play(ConductorBoss.AnimGroundRush);
                boss.SetLethal(ConductorBoss.AttackContext.Rush, true);
                timer = boss.Settings.rushMaxTime;
                phase = 2;
            }
            return;
        }

        if (phase == 2)
        {
            Vector3 p = boss.PlayerTarget.transform.position;
            bool playerBehind = rushDir > 0f ? p.x < boss.transform.position.x : p.x > boss.transform.position.x;
            if (playerBehind) behindTimer += Time.deltaTime; else behindTimer = 0f;
            boss.HandleHitbox(boss.RushCollider, boss.Settings.rushDamage);
            timer -= Time.deltaTime;
            if (behindTimer >= boss.Settings.missBehindTime || timer <= 0f)
            {
                boss.SetLethal(ConductorBoss.AttackContext.Rush, false);
                timer = boss.AnimLen(ConductorBoss.AnimGroundRush);
                phase = 3;
            }
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f) boss.ChangeToIdle(boss.Settings.idleDelay);
    }

    public override void FixedUpdate()
    {
        if (phase == 2) boss.SetVelocityX(rushDir * boss.Settings.rushSpeed); else boss.StopHorizontal();
    }

    public override void Exit()
    {
        boss.SetLethal(ConductorBoss.AttackContext.Plunge, false);
        boss.SetLethal(ConductorBoss.AttackContext.Rush, false);
        boss.StopHorizontal();
    }
}