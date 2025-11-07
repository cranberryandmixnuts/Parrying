using UnityEngine;

public sealed class BossSwordDropState : BossState
{
    private readonly Transform leftTop;
    private readonly Transform rightTop;
    private readonly int damage;
    private readonly float startAngle;
    private readonly float endAngle;
    private readonly float bladeLen;
    private readonly float bladeThick;

    private int phase;
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

    public override BossStateType StateType => BossStateType.SwordDrop;

    public BossSwordDropState
    (
        ConductorBoss boss,
        BossStateMachine stateMachine,
        Transform l,
        Transform r,
        int dmg,
        float startDeg,
        float endDeg,
        float bladeLength,
        float bladeThickness
    ) : base(boss, stateMachine)
    {
        leftTop = l;
        rightTop = r;
        damage = dmg;
        startAngle = startDeg;
        endAngle = endDeg;
        bladeLen = bladeLength;
        bladeThick = bladeThickness;
    }

    public override void Enter()
    {
        Transform t = boss.ChooseSideTop();
        boss.Teleport(t.position);
        bool choseLeft = t == leftTop;
        faceDir = choseLeft ? 1 : -1;
        boss.FaceTo(faceDir);
        boss.Play(ConductorBoss.AnimSideSword);

        phase = 1;
        boss.SetLethal(ConductorBoss.AttackContext.Sword, true);

        baseAngle = faceDir > 0 ? 0f : 180f;
        startLocal = faceDir > 0 ? startAngle : -startAngle;
        endLocal = faceDir > 0 ? endAngle : -endAngle;

        elapsed = 0f;
        duration = Mathf.Max(0.01f, boss.AnimLen(ConductorBoss.AnimSideSword));

        curAngle = baseAngle + startLocal;
        prevAngle = curAngle;
        boxSize = new Vector2(bladeLen, bladeThick);
        resolved = false;

        boss.DebugClearSwingLine();
    }

    public override void Update()
    {
        if (phase != 1) return;

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
                    if (!boss.PlayerTarget.Vitals.IsInvincible)
                    {
                        Vector2 hitPos = hits[i].point;
                        boss.PlayerTarget.Hit(damage, hitPos);
                        boss.PlayerTarget.ClearParryCandidate(boss);
                        boss.SetLethal(ConductorBoss.AttackContext.Sword, false);
                        resolved = true;
                    }
                    break;
                }
            }

            PlayerParryDashRegistration(curAngle);
        }

        Vector2 origin = (Vector2)boss.transform.position;
        Vector2 tipDir = TipDir(curAngle);
        boss.DebugUpdateSwingLine(origin, tipDir, bladeLen);

        if (elapsed >= duration)
        {
            boss.SetLethal(ConductorBoss.AttackContext.Sword, false);
            boss.DebugClearSwingLine();
            boss.ChangeToIdle(0.6f);
            phase = 2;
        }
    }

    public override void FixedUpdate()
    {
        boss.StopHorizontal();
    }

    public override void Exit()
    {
        boss.DebugClearSwingLine();
    }

    private Vector2 BladeCenter(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        Vector2 origin = (Vector2)boss.transform.position;
        return origin + dir * (bladeLen * 0.5f);
    }

    private Vector2 TipDir(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    private void PlayerParryDashRegistration(float angleDeg)
    {
        Vector2 rectCenter = BladeCenter(angleDeg);
        Vector2 half = new Vector2(bladeLen * 0.5f, bladeThick * 0.5f);
        Vector2 origin = (Vector2)boss.transform.position;
        Vector2 tip = origin + TipDir(angleDeg) * bladeLen;

        boss.PlayerTarget.GetParryDetectCircle(out Vector2 pc, out float pr);
        boss.PlayerTarget.GetDashDetectCircle(out Vector2 dc, out float dr);

        bool allow = !resolved && boss.LethalActive;

        if (allow && RectCircleIntersects(rectCenter, half, angleDeg, pc, pr)) boss.PlayerTarget.RegisterParryCandidate(boss, tip, damage);
        if (allow && RectCircleIntersects(rectCenter, half, angleDeg, dc, dr)) boss.PlayerTarget.RegisterDashCandidate(tip);
    }

    private bool RectCircleIntersects(Vector2 rectCenter, Vector2 rectHalf, float angleDeg, Vector2 circleCenter, float radius)
    {
        float rad = -angleDeg * Mathf.Deg2Rad;
        Vector2 d = circleCenter - rectCenter;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        Vector2 local = new Vector2(d.x * cos - d.y * sin, d.x * sin + d.y * cos);
        float cx = Mathf.Clamp(local.x, -rectHalf.x, rectHalf.x);
        float cy = Mathf.Clamp(local.y, -rectHalf.y, rectHalf.y);
        float dx = local.x - cx;
        float dy = local.y - cy;
        float r2 = radius * radius;
        return dx * dx + dy * dy <= r2;
    }
}