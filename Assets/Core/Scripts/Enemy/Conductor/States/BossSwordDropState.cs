using UnityEngine;

public sealed class BossSwordDropState : BossState
{
    private enum Phase
    {
        Tele,
        Swing,
        End
    }

    private Phase phase;
    private int faceDir;
    private float baseAngle;
    private float startLocal;
    private float endLocal;
    private float elapsed;
    private float duration;
    private float prevAngle;
    private float curAngle;
    private Vector2 boxSize;
    private bool resolved;
    private Coroutine teleportRoutine;

    public override BossStateType StateType => BossStateType.SwordDrop;

    public BossSwordDropState(BossController boss, BossStateMachine stateMachine)
        : base(boss, stateMachine) { }

    public override void Enter()
    {
        phase = Phase.Tele;
        resolved = false;
        boss.SetLethal(BossController.AttackContext.Sword, false);
        boss.StopHorizontal();
        boss.DebugClearSwingLine();

        boss.SetGravityScale(0f);
        boss.SetVelocityX(0f);
        boss.SetVelocityY(0f);

        float playerX = boss.PlayerTarget.transform.position.x;
        float leftX = boss.LeftTop.position.x;
        float rightX = boss.RightTop.position.x;

        Transform target = playerX < (leftX + rightX) * 0.5f ? boss.LeftTop : boss.RightTop;
        faceDir = target == boss.LeftTop ? 1 : -1;
        Vector3 point = target.position;
        teleportRoutine = boss.StartCoroutine(boss.TeleportRoutine(point, OnTeleported));
    }

    public override void Update()
    {
        if (phase != Phase.Swing) return;

        float dt = Time.deltaTime;
        elapsed += dt;
        if (elapsed > duration) elapsed = duration;

        float t = elapsed / duration;
        float s = t * t;

        prevAngle = curAngle;
        curAngle = baseAngle + Mathf.Lerp(startLocal, endLocal, s);

        if (!resolved && !boss.LethalActive) resolved = true;

        if (!resolved)
        {
            Vector2 prevCenter = BladeCenter(prevAngle);
            Vector2 currCenter = BladeCenter(curAngle);
            Vector2 castDir = currCenter - prevCenter;
            float castDist = castDir.magnitude;
            Vector2 dir = castDist > 0f ? castDir / castDist : Vector2.right;

            RaycastHit2D[] hits = Physics2D.BoxCastAll(prevCenter, boxSize, curAngle, dir, castDist, boss.PlayerHitMask);
            for (int i = 0; i < hits.Length; i++)
            {
                PlayerController pc = hits[i].collider.GetComponentInParent<PlayerController>();
                if (pc == boss.PlayerTarget)
                {
                    if (boss.PlayerTarget.TryHit(boss.Settings.swordDamage, boss.transform.position))
                    {
                        boss.PlayerTarget.ClearParryCandidate(boss);
                        boss.SetLethal(BossController.AttackContext.Sword, false);
                        resolved = true;
                    }
                    break;
                }
            }

            PlayerParryDashRegistration(curAngle);
        }

        if (boss.LethalActive && !resolved)
        {
            Vector2 origin = (Vector2)boss.transform.position;
            Vector2 tipDir = TipDir(curAngle);
            boss.DebugUpdateSwingLine(origin, tipDir, boss.Settings.swordBladeLength);
        }
        else if (!boss.LethalActive)
        {
            boss.DebugClearSwingLine();
        }

        if (elapsed >= duration)
        {
            boss.SetLethal(BossController.AttackContext.Sword, false);
            AudioManager.Instance.PlayOneShotSFX("쿵 소리", boss.gameObject);
            boss.ChangeToIdle(true);
            phase = Phase.End;
        }
    }

    public override void FixedUpdate()
    {
        if (phase == Phase.Tele)
        {
            boss.SetVelocityX(0f);
            boss.SetVelocityY(0f);
            return;
        }

        boss.StopHorizontal();
    }

    public override void Exit()
    {
        if (teleportRoutine != null) boss.StopCoroutine(teleportRoutine);
        teleportRoutine = null;
        boss.CancelTeleportEffects();
        boss.SetGravityScale(boss.OriginalGravityScale);
        boss.DebugClearSwingLine();
    }

    private void OnTeleported()
    {
        teleportRoutine = null;
        boss.SetGravityScale(boss.OriginalGravityScale);

        boss.FaceTo(faceDir);
        boss.Play(BossController.AnimSideSword);

        phase = Phase.Swing;
        boss.SetLethal(BossController.AttackContext.Sword, true);

        float startAngle = boss.Settings.swordStartAngle;
        float endAngle = boss.Settings.swordEndAngle;
        float bladeLen = boss.Settings.swordBladeLength;
        float bladeThick = boss.Settings.swordBladeThickness;

        baseAngle = faceDir > 0 ? 0f : 180f;
        startLocal = faceDir > 0 ? startAngle : -startAngle;
        endLocal = faceDir > 0 ? endAngle : -endAngle;

        elapsed = 0f;
        duration = boss.AnimLen(BossController.AnimSideSword);

        curAngle = baseAngle + startLocal;
        prevAngle = curAngle;
        boxSize = new Vector2(bladeLen, bladeThick);
        resolved = false;

        boss.DebugClearSwingLine();
    }

    private Vector2 BladeCenter(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new(Mathf.Cos(rad), Mathf.Sin(rad));
        Vector2 origin = (Vector2)boss.transform.position;
        return origin + dir * (boss.Settings.swordBladeLength * 0.5f);
    }

    private Vector2 TipDir(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    private void PlayerParryDashRegistration(float angleDeg)
    {
        if (resolved) return;
        if (!boss.LethalActive) return;

        Vector2 rectCenter = BladeCenter(angleDeg);
        Vector2 half = new(boss.Settings.swordBladeLength * 0.5f, boss.Settings.swordBladeThickness * 0.5f);

        boss.PlayerTarget.GetParryDetectCircle(out Vector2 pc, out float pr);
        boss.PlayerTarget.GetDashDetectCircle(out Vector2 dc, out float dr);

        if (RectCircleIntersects(rectCenter, half, angleDeg, pc, pr)) boss.PlayerTarget.RegisterParryCandidate(boss, boss.transform.position, boss.Settings.swordDamage);
        if (RectCircleIntersects(rectCenter, half, angleDeg, dc, dr)) boss.PlayerTarget.RegisterDashCandidate(boss.transform.position);
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
}