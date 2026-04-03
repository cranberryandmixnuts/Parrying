using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public sealed class BossCurvedSlashState : BossState
{
    private enum Phase
    {
        Tele,
        Down,
        Land,
        Up,
        ExternalRush
    }

    private Phase phase;
    private int faceDir;

    private float elapsed;
    private float duration;
    private float timer;

    private float prevAngle;
    private float curAngle;

    private float downSwingFromAngle;
    private float downSwingDelta;

    private float upSwingFromAngle;
    private float upSwingDelta;

    private Vector2 boxSize;
    private bool resolved;

    private Vector3 prevBossPos;

    private SplineContainer downPath;
    private SplineContainer upPath;

    private Vector2 rushDir;
    private float rushHitDisable;

    private bool rushActive;
    private bool rushEnteredBounds;
    private float rushBetweenTimer;

    private bool projectileHitboxPrevEnabled;
    private bool projectileHitboxOverridden;

    private Coroutine teleportRoutine;

    public override BossStateType StateType => BossStateType.CurvedSlash;

    public BossCurvedSlashState(BossController boss, BossStateMachine stateMachine)
        : base(boss, stateMachine) { }

    public override void Enter()
    {
        phase = Phase.Tele;
        resolved = false;
        timer = 0f;

        rushHitDisable = 0f;
        rushActive = false;
        rushEnteredBounds = false;
        rushBetweenTimer = 0f;

        projectileHitboxOverridden = false;

        boss.SetLethal(BossController.AttackContext.CurvedSlash, false);
        boss.SetGravityScale(0f);
        boss.SetVelocityX(0f);
        boss.SetVelocityY(0f);
        boss.StopHorizontal();
        boss.DebugClearSwingLine();

        float playerX = boss.PlayerTarget.transform.position.x;

        float3 leftStart = boss.LeftStandardDownPath.EvaluatePosition(0f);
        float3 rightStart = boss.RightStandardDownPath.EvaluatePosition(0f);
        float midX = (leftStart.x + rightStart.x) * 0.5f;

        bool useRightPath = playerX < midX;
        downPath = useRightPath ? boss.RightStandardDownPath : boss.LeftStandardDownPath;
        upPath = useRightPath ? boss.RightStandardUpPath : boss.LeftStandardUpPath;
        faceDir = useRightPath ? -1 : 1;

        float3 start = downPath.EvaluatePosition(0f);
        Vector3 point = new(start.x, start.y, boss.transform.position.z);

        teleportRoutine = boss.StartCoroutine(boss.TeleportRoutine(point, OnTeleported));
    }

    public override void Update()
    {
        if (phase == Phase.Down || phase == Phase.Up)
        {
            float dt = Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

            Vector3 prevPos = prevBossPos;
            prevAngle = curAngle;

            if (phase == Phase.Down)
            {
                curAngle = downSwingFromAngle + downSwingDelta * t;
                boss.transform.position = WithZ(EvaluatePathPosition(downPath, t));
            }
            else
            {
                curAngle = upSwingFromAngle + upSwingDelta * t;
                boss.transform.position = WithZ(EvaluatePathPosition(upPath, t));
            }

            Vector3 currPos = boss.transform.position;
            prevBossPos = currPos;

            if (!resolved && !boss.LethalActive) resolved = true;

            if (!resolved) TryBladeHitAndRegistration(prevPos, prevAngle, currPos, curAngle);

            if (boss.LethalActive && !resolved)
            {
                Vector2 origin = currPos;
                Vector2 tipDir = TipDir(curAngle);
                boss.DebugUpdateSwingLine(origin, tipDir, boss.Settings.curvedSlashBladeLength, boss.Settings.curvedSlashBladeThickness);
            }
            else if (!boss.LethalActive)
                boss.DebugClearSwingLine();

            elapsed += dt;
            if (elapsed < duration) return;

            if (phase == Phase.Down)
            {
                Vector3 junction = WithZ(EvaluatePathPosition(upPath, 0f));
                boss.transform.position = junction;
                prevBossPos = junction;

                boss.Play(BossController.AnimCurvedSlashLand);
                boss.SetLethal(BossController.AttackContext.CurvedSlash, false);
                boss.DebugClearSwingLine();

                phase = Phase.Land;
                timer = boss.AnimLen(BossController.AnimCurvedSlashLand);
                return;
            }

            BeginExternalRush();
            return;
        }

        if (phase == Phase.Land)
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;

            boss.Play(BossController.AnimCurvedSlashUp);
            boss.SetLethal(BossController.AttackContext.CurvedSlash, true);

            phase = Phase.Up;
            duration = boss.AnimLen(BossController.AnimCurvedSlashUp);
            elapsed = 0f;
            resolved = false;

            curAngle = upSwingFromAngle;
            prevAngle = curAngle;
            prevBossPos = boss.transform.position;
            return;
        }

        if (phase == Phase.ExternalRush)
        {
            if (rushActive)
            {
                if (rushHitDisable > 0f)
                {
                    rushHitDisable -= Time.deltaTime;
                    if (rushHitDisable <= 0f) boss.SetLethal(BossController.AttackContext.Rush, true);
                }
                else
                {
                    int hit = boss.HandleHitbox(boss.RushCollider, boss.Settings.curvedSlashDamage);
                    if (hit > 0)
                    {
                        boss.SetLethal(BossController.AttackContext.Rush, false);
                        rushHitDisable = boss.Settings.externalRushHitDisableTime;
                    }
                    else if (!boss.LethalActive)
                        rushHitDisable = boss.Settings.externalRushHitDisableTime;
                }

                bool inside = boss.ExternalRushBounds.OverlapPoint(boss.transform.position);
                if (!rushEnteredBounds && inside) rushEnteredBounds = true;

                if (rushEnteredBounds && !inside)
                {
                    Vector3 clamped = boss.ExternalRushBounds.ClosestPoint(boss.transform.position);
                    clamped.z = boss.transform.position.z;
                    boss.transform.position = clamped;

                    boss.SetVelocityX(0f);
                    boss.SetVelocityY(0f);
                    boss.SetLethal(BossController.AttackContext.Rush, false);

                    rushActive = false;
                    rushEnteredBounds = false;
                    rushBetweenTimer = boss.Settings.externalRushInterval;
                }

                return;
            }

            rushBetweenTimer -= Time.deltaTime;
            if (rushBetweenTimer > 0f) return;

            WarpToNextExternalRushSpawn();
            boss.SetLethal(BossController.AttackContext.Rush, true);
            rushHitDisable = 0f;
            rushActive = true;
        }
    }

    public override void FixedUpdate()
    {
        if (phase == Phase.ExternalRush)
        {
            boss.SetGravityScale(0f);

            if (!rushActive)
            {
                boss.SetVelocityX(0f);
                boss.SetVelocityY(0f);
                return;
            }

            float spd = boss.Settings.externalRushSpeed;
            boss.SetVelocityX(rushDir.x * spd);
            boss.SetVelocityY(rushDir.y * spd);
            return;
        }

        boss.SetVelocityX(0f);
        boss.SetVelocityY(0f);
    }

    public override void Exit()
    {
        if (teleportRoutine != null) boss.StopCoroutine(teleportRoutine);
        teleportRoutine = null;
        boss.CancelTeleportEffects();

        if (projectileHitboxOverridden)
        {
            boss.ProjectileHitbox.enabled = projectileHitboxPrevEnabled;
            projectileHitboxOverridden = false;
        }

        boss.SetLethal(BossController.AttackContext.None, false);
        boss.SetVelocityX(0f);
        boss.SetVelocityY(0f);
        boss.SetGravityScale(boss.OriginalGravityScale);
        boss.DebugClearSwingLine();
        boss.ResetRotationToFacing();
    }

    private void OnTeleported()
    {
        teleportRoutine = null;

        boss.FaceTo(faceDir);

        boss.Play(BossController.AnimCurvedSlashDown);
        boss.SetLethal(BossController.AttackContext.CurvedSlash, true);

        float startAngle = boss.Settings.curvedSlashStartAngle;
        float endAngle = boss.Settings.curvedSlashEndAngle;

        float downStartAngle;
        float downEndAngle;
        float upStartAngle;
        float upEndAngle;

        if (faceDir > 0)
        {
            downStartAngle = startAngle;
            downEndAngle = endAngle;
            upStartAngle = endAngle;
            upEndAngle = startAngle;
        }
        else
        {
            downStartAngle = 180f - startAngle;
            downEndAngle = 180f - endAngle;
            upStartAngle = 180f - endAngle;
            upEndAngle = 180f - startAngle;
        }

        downSwingFromAngle = downStartAngle;
        downSwingDelta = Mathf.DeltaAngle(downStartAngle, downEndAngle);

        upSwingFromAngle = upStartAngle;
        upSwingDelta = Mathf.DeltaAngle(upStartAngle, upEndAngle);

        elapsed = 0f;
        duration = boss.AnimLen(BossController.AnimCurvedSlashDown);

        curAngle = downSwingFromAngle;
        prevAngle = curAngle;

        float bladeLen = boss.Settings.curvedSlashBladeLength;
        float bladeThick = boss.Settings.curvedSlashBladeThickness;
        boxSize = new Vector2(bladeLen, bladeThick);

        resolved = false;
        prevBossPos = boss.transform.position;
        phase = Phase.Down;
    }

    private void BeginExternalRush()
    {
        boss.DebugClearSwingLine();
        boss.SetLethal(BossController.AttackContext.Rush, true);
        boss.Play(BossController.AnimExternalRush);
        boss.SetGravityScale(0f);

        projectileHitboxPrevEnabled = boss.ProjectileHitbox.enabled;
        boss.ProjectileHitbox.enabled = false;
        projectileHitboxOverridden = true;

        WarpToNextExternalRushSpawn();
        rushHitDisable = 0f;
        rushActive = true;
        rushEnteredBounds = false;
        rushBetweenTimer = 0f;
        phase = Phase.ExternalRush;
    }

    private void WarpToNextExternalRushSpawn()
    {
        Vector3 spawn = ChooseExternalRushSpawn();
        spawn.z = boss.transform.position.z;
        boss.transform.position = spawn;

        Vector2 aim = boss.PlayerTarget.transform.position;
        Vector2 p = spawn;
        rushDir = (aim - p).sqrMagnitude > 0f ? (aim - p).normalized : Vector2.right;

        float zAng = Mathf.Atan2(rushDir.y, rushDir.x) * Mathf.Rad2Deg - 90f;
        boss.transform.rotation = Quaternion.Euler(0f, 0f, zAng);

        boss.SetVelocityX(0f);
        boss.SetVelocityY(0f);
    }

    private Vector3 ChooseExternalRushSpawn()
    {
        int r = UnityEngine.Random.Range(1, 4);

        if (r == 1)
        {
            Vector3 b = boss.ExternalRushSpawnLeft.position;
            float y = b.y + UnityEngine.Random.Range(-boss.Settings.externalRushSideYRandomRange, boss.Settings.externalRushSideYRandomRange);
            return new Vector3(b.x, y, b.z);
        }

        if (r == 2)
        {
            Vector3 b = boss.ExternalRushSpawnTop.position;
            float x = b.x + UnityEngine.Random.Range(-boss.Settings.externalRushTopXRandomRange, boss.Settings.externalRushTopXRandomRange);
            return new Vector3(x, b.y, b.z);
        }

        {
            Vector3 b = boss.ExternalRushSpawnRight.position;
            float y = b.y + UnityEngine.Random.Range(-boss.Settings.externalRushSideYRandomRange, boss.Settings.externalRushSideYRandomRange);
            return new Vector3(b.x, y, b.z);
        }
    }

    private void TryBladeHitAndRegistration(Vector3 prevPos, float prevAng, Vector3 currPos, float currAng)
    {
        Vector2 prevCenter = BladeCenter(prevPos, prevAng);
        Vector2 currCenter = BladeCenter(currPos, currAng);
        Vector2 castDir = currCenter - prevCenter;
        float castDist = castDir.magnitude;
        Vector2 dir = castDist > 0f ? castDir / castDist : Vector2.right;

        RaycastHit2D[] hits = Physics2D.BoxCastAll(prevCenter, boxSize, currAng, dir, castDist, boss.PlayerHitMask);
        for (int i = 0; i < hits.Length; i++)
        {
            PlayerController pc = hits[i].collider.GetComponentInParent<PlayerController>();
            if (pc == boss.PlayerTarget)
            {
                if (boss.PlayerTarget.TryHit(boss.Settings.curvedSlashDamage, boss.transform.position))
                {
                    boss.PlayerTarget.ClearParryCandidate(boss);
                    boss.SetLethal(BossController.AttackContext.CurvedSlash, false);
                    resolved = true;
                }
                break;
            }
        }

        PlayerParryDashRegistration(currPos, currAng);
    }

    private void PlayerParryDashRegistration(Vector3 origin, float angleDeg)
    {
        if (resolved) return;
        if (!boss.LethalActive) return;

        Vector2 rectCenter = BladeCenter(origin, angleDeg);
        Vector2 half = new(boss.Settings.curvedSlashBladeLength * 0.5f, boss.Settings.curvedSlashBladeThickness * 0.5f);

        boss.PlayerTarget.GetParryDetectCircle(out Vector2 pc, out float pr);
        boss.PlayerTarget.GetDashDetectCircle(out Vector2 dc, out float dr);

        if (RectCircleIntersects(rectCenter, half, angleDeg, pc, pr)) boss.PlayerTarget.RegisterParryCandidate(boss, boss.transform.position, boss.Settings.curvedSlashDamage);
        if (RectCircleIntersects(rectCenter, half, angleDeg, dc, dr)) boss.PlayerTarget.RegisterDashCandidate(boss.transform.position);
    }

    private Vector2 BladeCenter(Vector3 origin, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new(Mathf.Cos(rad), Mathf.Sin(rad));
        Vector2 o = origin;
        return o + dir * (boss.Settings.curvedSlashBladeLength * 0.5f);
    }

    private Vector2 TipDir(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    private bool RectCircleIntersects(Vector2 rectCenter, Vector2 rectHalf, float angleDeg, Vector2 circleCenter, float radius)
    {
        float rad = -angleDeg * Mathf.Deg2Rad;
        Vector2 d = circleCenter - rectCenter;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        Vector2 local = new(d.x * cos - d.y * sin, d.x * sin + d.y * cos);
        float cx = Mathf.Clamp(local.x, -rectHalf.x, rectHalf.x);
        float cy = Mathf.Clamp(local.y, -rectHalf.y, rectHalf.y);
        float dx = local.x - cx;
        float dy = local.y - cy;
        float r2 = radius * radius;
        return dx * dx + dy * dy <= r2;
    }

    private Vector3 EvaluatePathPosition(SplineContainer path, float t01)
    {
        float3 p = path.EvaluatePosition(Mathf.Clamp01(t01));
        return new Vector3(p.x, p.y, p.z);
    }

    private Vector3 WithZ(Vector3 p) => new(p.x, p.y, boss.transform.position.z);
}