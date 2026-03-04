using UnityEngine;

public sealed class BossCurvedSlashState : BossState
{
    private enum Phase
    {
        Tele,
        Down,
        MidPause,
        Up,
        ExternalRush
    }

    private const float MidPauseSec = 0.1f;

    private Phase phase;

    private bool startOnLeft;
    private int faceDir;

    private float baseAngle;
    private float startLocal;
    private float endLocal;

    private float elapsed;
    private float duration;

    private float midPauseTimer;

    private float prevAngle;
    private float curAngle;

    private Vector2 boxSize;
    private bool resolved;

    private Vector3 prevBossPos;

    private Vector3 downP0;
    private Vector3 downP1;
    private Vector3 downP2;

    private Vector3 upP0;
    private Vector3 upP1;
    private Vector3 upP2;

    private Vector2 rushDir;
    private bool rushEnteredScreen;
    private float rushHitDisable;

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

        rushEnteredScreen = false;
        rushHitDisable = 0f;

        projectileHitboxOverridden = false;

        boss.SetLethal(BossController.AttackContext.CurvedSlash, false);
        boss.SetGravityScale(0f);
        boss.SetVelocityX(0f);
        boss.SetVelocityY(0f);
        boss.StopHorizontal();
        boss.DebugClearSwingLine();

        Vector3 player = boss.PlayerTarget.transform.position;
        float dl = Mathf.Abs(player.x - boss.CurvedTopLeft.position.x);
        float dr = Mathf.Abs(player.x - boss.CurvedTopRight.position.x);
        startOnLeft = dl >= dr;

        Transform start = startOnLeft ? boss.CurvedTopLeft : boss.CurvedTopRight;
        Vector3 p = start.position;
        p.z = boss.transform.position.z;

        teleportRoutine = boss.StartCoroutine(boss.TeleportRoutine(p, OnTeleported));
    }

    public override void Update()
    {
        if (phase == Phase.MidPause)
        {
            midPauseTimer -= Time.deltaTime;
            if (midPauseTimer > 0f) return;

            boss.Play(BossController.AnimCurvedSlashUp);
            boss.SetLethal(BossController.AttackContext.CurvedSlash, true);

            phase = Phase.Up;
            duration = boss.AnimLen(BossController.AnimCurvedSlashUp);
            elapsed = 0f;

            curAngle = baseAngle + endLocal;
            prevAngle = curAngle;
            prevBossPos = boss.transform.position;

            return;
        }

        if (phase == Phase.Down || phase == Phase.Up)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            if (elapsed > duration) elapsed = duration;

            float t = duration > 0f ? elapsed / duration : 1f;
            float s = t * t;

            Vector3 prevPos = prevBossPos;
            prevAngle = curAngle;

            if (phase == Phase.Down)
            {
                curAngle = baseAngle + Mathf.Lerp(startLocal, endLocal, s);
                boss.transform.position = WithZ(Bezier2(downP0, downP1, downP2, t));
            }
            else
            {
                curAngle = baseAngle + Mathf.Lerp(endLocal, startLocal, s);
                boss.transform.position = WithZ(Bezier2(upP0, upP1, upP2, t));
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
            {
                boss.DebugClearSwingLine();
            }

            if (elapsed >= duration)
            {
                if (phase == Phase.Down)
                {
                    boss.DebugClearSwingLine();
                    boss.SetLethal(BossController.AttackContext.None, false);

                    phase = Phase.MidPause;
                    midPauseTimer = MidPauseSec;
                    return;
                }

                BeginExternalRush();
            }

            return;
        }

        if (phase == Phase.ExternalRush)
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
                {
                    rushHitDisable = boss.Settings.externalRushHitDisableTime;
                }
            }

            bool inside = IsInsideExternalRushBounds(boss.transform.position);
            if (!rushEnteredScreen && inside) rushEnteredScreen = true;

            if (rushEnteredScreen && !inside)
            {
                WarpToNextExternalRushSpawn();
                rushEnteredScreen = false;
            }
        }
    }

    public override void FixedUpdate()
    {
        if (phase == Phase.ExternalRush)
        {
            boss.SetGravityScale(0f);
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

        float playerX = boss.PlayerTarget.transform.position.x;
        faceDir = playerX >= boss.transform.position.x ? 1 : -1;
        boss.FaceTo(faceDir);

        Vector3 a = (startOnLeft ? boss.CurvedTopLeft : boss.CurvedTopRight).position;
        Vector3 b = boss.CurvedMid.position;
        Vector3 c = (startOnLeft ? boss.CurvedTopRight : boss.CurvedTopLeft).position;

        float z = boss.transform.position.z;

        downP0 = new Vector3(a.x, a.y, z);
        downP2 = new Vector3(b.x, b.y, z);
        downP1 = CurveControlY(downP0, downP2, boss.Settings.curvedSlashDownCurveBulgeY);

        upP0 = downP2;
        upP2 = new Vector3(c.x, c.y, z);
        upP1 = CurveControlY(upP0, upP2, boss.Settings.curvedSlashUpCurveBulgeY);

        boss.Play(BossController.AnimCurvedSlashDown);
        boss.SetLethal(BossController.AttackContext.CurvedSlash, true);

        float startAngle = boss.Settings.curvedSlashStartAngle;
        float endAngle = boss.Settings.curvedSlashEndAngle;
        float bladeLen = boss.Settings.curvedSlashBladeLength;
        float bladeThick = boss.Settings.curvedSlashBladeThickness;

        baseAngle = faceDir > 0 ? 0f : 180f;
        startLocal = faceDir > 0 ? startAngle : -startAngle;
        endLocal = faceDir > 0 ? endAngle : -endAngle;

        elapsed = 0f;
        duration = boss.AnimLen(BossController.AnimCurvedSlashDown);

        curAngle = baseAngle + startLocal;
        prevAngle = curAngle;
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
        phase = Phase.ExternalRush;
    }

    private void WarpToNextExternalRushSpawn()
    {
        Vector3 spawn = ChooseExternalRushSpawn();
        spawn.z = boss.transform.position.z;
        boss.transform.position = spawn;

        Vector2 player = boss.PlayerTarget.transform.position;
        Vector2 p = spawn;
        rushDir = (player - p).sqrMagnitude > 0f ? (player - p).normalized : Vector2.right;

        float zAng = Mathf.Atan2(rushDir.y, rushDir.x) * Mathf.Rad2Deg;
        boss.transform.rotation = Quaternion.Euler(0f, 0f, zAng);

        boss.SetVelocityX(0f);
        boss.SetVelocityY(0f);
    }

    private Vector3 ChooseExternalRushSpawn()
    {
        int r = Random.Range(1, 4);

        if (r == 1)
        {
            Vector3 b = boss.ExternalRushSpawnLeft.position;
            float y = b.y + Random.Range(-boss.Settings.externalRushSideYRandomRange, boss.Settings.externalRushSideYRandomRange);
            return new Vector3(b.x, y, b.z);
        }

        if (r == 2)
        {
            Vector3 b = boss.ExternalRushSpawnTop.position;
            float x = b.x + Random.Range(-boss.Settings.externalRushTopXRandomRange, boss.Settings.externalRushTopXRandomRange);
            return new Vector3(x, b.y, b.z);
        }

        {
            Vector3 b = boss.ExternalRushSpawnRight.position;
            float y = b.y + Random.Range(-boss.Settings.externalRushSideYRandomRange, boss.Settings.externalRushSideYRandomRange);
            return new Vector3(b.x, y, b.z);
        }
    }

    private bool IsInsideExternalRushBounds(Vector3 p)
    {
        float leftX = boss.ExternalRushSpawnLeft.position.x;
        float rightX = boss.ExternalRushSpawnRight.position.x;
        float topY = boss.ExternalRushSpawnTop.position.y;

        return p.x >= leftX && p.x <= rightX && p.y <= topY;
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

    private Vector3 CurveControlY(Vector3 p0, Vector3 p2, float bulgeY)
    {
        Vector3 m = (p0 + p2) * 0.5f;
        return new Vector3(m.x, m.y + bulgeY, m.z);
    }

    private Vector3 Bezier2(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    private Vector3 WithZ(Vector3 p) => new(p.x, p.y, boss.transform.position.z);
}