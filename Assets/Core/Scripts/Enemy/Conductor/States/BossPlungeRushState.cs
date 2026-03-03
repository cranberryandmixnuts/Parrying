using UnityEngine;

public sealed class BossPlungeRushState : BossState
{
    private enum Phase
    {
        Tele,
        Fall,
        RushDelay,
        Rush,
        Bounce,
        End
    }

    private Phase phase;
    private float timer;
    private int rushDir;
    private float rushStopX;
    private float rushTime;
    private float rushDisable;
    private bool teleported;
    private Coroutine teleportRoutine;

    public override BossStateType StateType => BossStateType.PlungeRush;

    public BossPlungeRushState(BossController boss, BossStateMachine stateMachine)
        : base(boss, stateMachine) { }

    public override void Enter()
    {
        teleported = false;
        phase = Phase.End;
        timer = 0f;
        rushTime = 0f;
        rushDisable = 0f;

        boss.SetLethal(BossController.AttackContext.None, false);
        boss.SetGravityScale(0f);
        boss.SetVelocityX(0f);
        boss.SetVelocityY(0f);
        boss.StopHorizontal();

        Vector3 player = boss.PlayerTarget.transform.position;
        Vector3 ceiling = boss.CeilingPoint.position;
        Vector3 point = new(player.x, ceiling.y, boss.transform.position.z);

        teleportRoutine = boss.StartCoroutine(boss.TeleportRoutine(point, OnTeleported));
    }

    public override void Update()
    {
        if (!teleported) return;

        if (phase == Phase.Tele)
        {
            TryRegisterCounterParry();
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                boss.SetGravityScale(boss.OriginalGravityScale);
                boss.Play(BossController.AnimPlunge);
                phase = Phase.Fall;
            }
            return;
        }

        if (phase == Phase.Fall)
        {
            int hit = boss.HandleHitbox(boss.PlungeCollider, boss.Settings.plungeDamage);
            if (hit > 0)
            {
                boss.SetLethal(BossController.AttackContext.Plunge, false);
                boss.Play(BossController.AnimAirIdle);
                boss.SetVelocityX(0f);
                boss.SetVelocityY(boss.Settings.plungeBounceUpSpeed);
                phase = Phase.Bounce;
                return;
            }

            if (!boss.LethalActive)
            {
                boss.Play(BossController.AnimAirIdle);
                boss.SetVelocityX(0f);
                boss.SetVelocityY(boss.Settings.plungeBounceUpSpeed);
                phase = Phase.Bounce;
                return;
            }

            if (GroundHit())
            {
                float px = boss.PlayerTarget.transform.position.x;
                rushDir = px >= boss.transform.position.x ? 1 : -1;
                boss.FaceTo(rushDir);
                rushStopX = rushDir > 0 ? boss.RushStopRight.position.x : boss.RushStopLeft.position.x;

                boss.Play(BossController.AnimGroundRush);
                boss.SetVelocityX(0f);
                boss.SetLethal(BossController.AttackContext.Rush, false);

                timer = boss.Settings.rushStartDelay;
                phase = Phase.RushDelay;
                return;
            }
            return;
        }

        if (phase == Phase.RushDelay)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                boss.SetLethal(BossController.AttackContext.Rush, true);
                phase = Phase.Rush;
                rushTime = 0f;
                rushDisable = 0f;
            }
            return;
        }

        if (phase == Phase.Rush)
        {
            if (rushDisable > 0f)
            {
                rushDisable -= Time.deltaTime;
                if (rushDisable <= 0f) boss.SetLethal(BossController.AttackContext.Rush, true);
            }
            else
            {
                int hit = boss.HandleHitbox(boss.RushCollider, boss.Settings.rushDamage);
                if (hit > 0)
                {
                    boss.SetLethal(BossController.AttackContext.Rush, false);
                    rushDisable = 1.0f;
                }
                else if (!boss.LethalActive)
                {
                    rushDisable = 1.0f;
                }
            }

            rushTime += Time.deltaTime;
            if (ReachedRushStop() || rushTime >= boss.Settings.rushMaxTime)
            {
                boss.SetLethal(BossController.AttackContext.Rush, false);
                phase = Phase.End;
                timer = boss.AnimLen(BossController.AnimGroundRush);
            }
            return;
        }

        if (phase == Phase.Bounce)
        {
            if (boss.GetVelocityY() <= 0f)
            {
                boss.ChangeToIdle(false);
                phase = Phase.End;
                timer = 0f;
                return;
            }
            return;
        }

        if (phase == Phase.End)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) boss.ChangeToIdle(true);
        }
    }

    public override void FixedUpdate()
    {
        if (!teleported)
        {
            boss.SetVelocityX(0f);
            boss.SetVelocityY(0f);
            return;
        }

        if (phase == Phase.Tele)
        {
            boss.SetVelocityX(0f);
            boss.SetVelocityY(0f);
        }
        else if (phase == Phase.Fall)
        {
            boss.SetVelocityY(-boss.Settings.plungeFallSpeed);
        }
        else if (phase == Phase.Rush)
        {
            boss.SetVelocityX(rushDir * boss.Settings.rushSpeed);
        }
        else
        {
            boss.StopHorizontal();
        }
    }

    public override void Exit()
    {
        if (teleportRoutine != null) boss.StopCoroutine(teleportRoutine);
        teleportRoutine = null;
        boss.CancelTeleportEffects();

        teleported = false;
        boss.SetLethal(BossController.AttackContext.Plunge, false);
        boss.SetLethal(BossController.AttackContext.Rush, false);
        boss.StopHorizontal();
        boss.SetGravityScale(boss.OriginalGravityScale);
    }

    private void OnTeleported()
    {
        teleportRoutine = null;
        teleported = true;

        float playerX = boss.PlayerTarget.transform.position.x;
        boss.FaceTo(playerX >= boss.transform.position.x ? 1 : -1);

        phase = Phase.Tele;
        timer = boss.Settings.plungeFallDelay;
        boss.SetLethal(BossController.AttackContext.Plunge, true);
        rushTime = 0f;
        rushDisable = 0f;
    }

    private void TryRegisterCounterParry()
    {
        boss.PlayerTarget.GetParryDetectCircle(out Vector2 pc, out float pr);
        Vector2 hp = boss.transform.position;
        Vector2 d = hp - pc;
        if (d.sqrMagnitude <= pr * pr) boss.PlayerTarget.RegisterParryCandidate(boss, hp, 0);
    }

    private bool GroundHit() => boss.IsTouchingGround();

    private bool ReachedRushStop()
    {
        float x = boss.transform.position.x;
        if (rushDir > 0) return x >= rushStopX;
        else return x <= rushStopX;
    }
}