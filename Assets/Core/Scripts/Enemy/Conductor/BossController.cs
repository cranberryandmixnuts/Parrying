using System;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.VFX;

public sealed class BossController : EnemyBase, IParryReactive, IEnemyProjectileOwner
{
    public enum AttackContext
    {
        None,
        Sword,
        Plunge,
        Rush,
        LaserP1,
        LaserP2
    }

    public const string AnimGroundIdle = "Ground Idle";
    public const string AnimAirIdle = "Air Idle";
    public const string AnimGroggy = "Groggy";
    public const string AnimSideSword = "Side Sword";
    public const string AnimPlunge = "Plunge";
    public const string AnimGroundRush = "Ground Rush";
    public const string AnimFire = "Fire";
    public const string AnimCrackLaser = "Crack Laser";
    public const string AnimDeath = "Death";

    [ReadOnly] [SerializeField] private int p1Stacks;
    [ReadOnly] [SerializeField] private int p2Stacks;

    [Header("Settings")]
    [SerializeField] private BossSettings settings;

    [Header("SwordDrop")]
    [SerializeField] private Transform leftTop;
    [SerializeField] private Transform rightTop;

    [Header("PlungeRush")]
    [SerializeField] private Transform ceilingPoint;
    [SerializeField] private Transform rushStopLeft;
    [SerializeField] private Transform rushStopRight;
    [SerializeField] private Collider2D plungeCollider;
    [SerializeField] private Collider2D rushCollider;

    [Header("VolleyLaser")]
    [SerializeField] private Collider2D projectileHitbox;
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private Transform volleyCenter;
    [SerializeField] private Transform volleyHeight;

    [Header("RadialLaser")]
    [SerializeField] private Transform radialLaserPoint;

    [Header("VisualEffect")]


    [Header("Debug Gizmo")]
    [SerializeField] private bool debugDrawSwordGizmo = true;
    [SerializeField] private Color debugSwordColor = new(1f, 0.3f, 0.2f, 0.8f);
    [SerializeField] private float debugArcStepDeg = 5f;
    [SerializeField] private LineRenderer Line;
    [SerializeField] private Color LaserWarningColor = new(1f, 0.9f, 0.4f, 0.9f);
    [SerializeField] private Color LaserFiringColor = new(1f, 0.2f, 0.1f, 0.9f);

    private float gravityOriginal;
    private readonly Collider2D[] overlapResults = new Collider2D[8];
    private BossStateMachine stateMachine;
    private bool lethalActive;
    private AttackContext attackCx;

    protected override string DeathAnimName => AnimDeath;

    protected override void Start()
    {
        base.Start();
        gravityOriginal = Body.gravityScale;
        p1Stacks = settings.p1Stacks;
        p2Stacks = settings.p2Stacks;
        stateMachine = new BossStateMachine();
        stateMachine.Initialize(new BossIdleState(this, stateMachine, true));
    }

    protected override void OnUpdate()
    {
        stateMachine.Update();
    }

    protected override void OnFixedUpdate()
    {
        stateMachine.FixedUpdate();
    }

    public BossStateType CurrentStateType => stateMachine != null ? stateMachine.CurrentStateType : BossStateType.Missing;
    public BossSettings Settings => settings;
    public Transform LeftTop => leftTop;
    public Transform RightTop => rightTop;
    public Transform CeilingPoint => ceilingPoint;
    public Transform RushStopLeft => rushStopLeft;
    public Transform RushStopRight => rushStopRight;
    public Collider2D PlungeCollider => plungeCollider;
    public Collider2D RushCollider => rushCollider;
    public EnemyProjectile MissilePrefab => projectilePrefab;
    public Transform VolleyCenter => volleyCenter;
    public Transform VolleyHeight => volleyHeight;
    public Transform RadialLaserPoint => radialLaserPoint;

    public bool LethalActive => lethalActive;
    public float FacingDir => FacingDirection;
    public float OriginalGravityScale => gravityOriginal;
    public PlayerController PlayerTarget => Player;

    public Transform ProjectileTargetTransform
    {
        get { return transform; }
    }

    public Collider2D ProjectileHitbox
    {
        get { return projectileHitbox; }
    }

    public void OnHitByReflectedProjectile()
    {
        OnMissileReflectedHit();
    }

    public void ChangeToIdle(bool grounded)
    {
        stateMachine.ChangeState(new BossIdleState(this, stateMachine, grounded));
    }

    public void ChangeToGroggy(float duration)
    {
        stateMachine.ChangeState(new BossGroggyState(this, stateMachine, duration));
    }

    public void ChangeToSwordDrop()
    {
        stateMachine.ChangeState(new BossSwordDropState(this, stateMachine));
    }

    public void ChangeToPlungeRush()
    {
        stateMachine.ChangeState(new BossPlungeRushState(this, stateMachine));
    }

    public void ChangeToVolleyLaser()
    {
        stateMachine.ChangeState(new BossVolleyLaserState(this, stateMachine));
    }

    public void ChangeToRadialLaser()
    {
        stateMachine.ChangeState(new BossRadialLaserState(this, stateMachine));
    }

    public void ChangeToDeath()
    {
        stateMachine.ChangeState(new BossDeathState(this, stateMachine));
    }

    public void OnPerfectParry(Vector2 hitPoint)
    {
        if (!lethalActive) return;
        Player.ClearParryCandidate(this);
        SetLethal(AttackContext.None, false);
    }

    public void OnImperfectParry(Vector2 hitPoint)
    {
        if (!lethalActive) return;
        Player.ClearParryCandidate(this);
        SetLethal(AttackContext.None, false);
    }

    public void OnCounterParry(Vector2 hitPoint)
    {
        if (!lethalActive) return;
        Player.ClearParryCandidate(this);
        SetLethal(AttackContext.None, false);
        if (attackCx == AttackContext.LaserP2) { ChangeToDeath(); return; }
        if (HasP1Stacks) { ConsumeP1Stack(); ChangeToGroggy(settings.groggyDuration); return; }
        if (HasP2Stacks) { ConsumeP2Stack(); ChangeToGroggy(settings.groggyDuration); return; }
        ChangeToDeath();
    }

    public void OnMissileReflectedHit()
    {
        if (HasP1Stacks)
        {
            ConsumeP1Stack();
            ChangeToGroggy(settings.groggyDuration);
            return;
        }

        if (HasP2Stacks)
        {
            ConsumeP2Stack();
            ChangeToGroggy(settings.groggyDuration);
            return;
        }

        ChangeToDeath();
    }

    public void DecideP1()
    {
        int r = UnityEngine.Random.Range(0, 3);
        switch (r)
        {
            case 0:
                ChangeToSwordDrop();
                break;
            case 1:
                ChangeToPlungeRush();
                break;
            default:
                ChangeToVolleyLaser();
                break;
        }
    }

    public void ConsumeP1Stack()
    {
        if (p1Stacks > 0) p1Stacks -= 1;
    }

    public bool HasP1Stacks => p1Stacks > 0;

    public void ConsumeP2Stack()
    {
        if (p2Stacks > 0) p2Stacks -= 1;
    }

    public bool HasP2Stacks => p2Stacks > 0;

    public void SetLethal(AttackContext cx, bool on)
    {
        AttackContext prev = attackCx;
        attackCx = cx;
        lethalActive = on;
        Player.ClearParryCandidate(this);
        if (!on && prev == AttackContext.Sword) DebugClearSwingLine();
    }

    public void Play(string anim)
    {
        Anim.Play(anim);
    }

    public float AnimLen(string anim)
    {
        return GetAnimLength(anim);
    }

    public void FaceToPlayer()
    {
        FacePlayer();
    }

    public void FaceTo(int dir)
    {
        ApplyFacing(dir < 0 ? -1 : 1);
    }

    public void Teleport(Vector3 p)
    {
        transform.position = p;
    }

    public void SetGravityScale(float v)
    {
        Body.gravityScale = v;
    }

    public float GetGravityScale()
    {
        return Body.gravityScale;
    }

    public void SetVelocityX(float v)
    {
        Body.linearVelocity = new Vector2(v, Body.linearVelocity.y);
    }

    public void SetVelocityY(float y)
    {
        Body.linearVelocity = new Vector2(Body.linearVelocity.x, y);
    }

    public float GetVelocityY()
    {
        return Body.linearVelocity.y;
    }

    public bool IsTouchingGround()
    {
        return Body.IsTouchingLayers(settings.groundLayer);
    }

    public void StopHorizontal()
    {
        Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
    }

    public int HandleHitbox(Collider2D hitCol, int damage)
    {
        if (!lethalActive) return 0;

        Player.GetParryDetectCircle(out Vector2 pCenter, out float pRadius);
        if (IsColliderWithinCircle(hitCol, pCenter, pRadius)) Player.RegisterParryCandidate(this, hitCol.bounds.center, damage);

        Player.GetDashDetectCircle(out Vector2 dCenter, out float dRadius);
        if (IsColliderWithinCircle(hitCol, dCenter, dRadius)) Player.RegisterDashCandidate(hitCol.bounds.center);

        ContactFilter2D f = new();
        f.SetLayerMask(settings.playerHitMask);
        f.useTriggers = true;

        int hitCount = hitCol.Overlap(f, overlapResults);
        for (int i = 0; i < hitCount; i++)
        {
            PlayerController pc = overlapResults[i].GetComponentInParent<PlayerController>();
            if (pc == Player)
            {
                if (!Player.TryHit(damage, hitCol.bounds.center)) return 0;
                Player.ClearParryCandidate(this);
                if (attackCx != AttackContext.Rush) SetLethal(AttackContext.None, false);
                return 1;
            }
        }
        return 0;
    }

    public LayerMask PlayerHitMask => settings.playerHitMask;

    public void DebugUpdateSwingLine(Vector2 origin, Vector2 dir, float length)
    {
        if (Line == null) throw new InvalidOperationException("ConductorBoss.swingLine is null. Assign a LineRenderer in the inspector.");
        Line.widthMultiplier = settings.swordBladeThickness;
        Line.useWorldSpace = true;
        Line.positionCount = 2;
        Vector3 a = origin;
        Vector3 b = origin + dir.normalized * length;
        Line.SetPosition(0, a);
        Line.SetPosition(1, b);
    }

    public void DebugClearSwingLine()
    {
        if (Line == null) throw new InvalidOperationException("ConductorBoss.swingLine is null. Assign a LineRenderer in the inspector.");
        Line.positionCount = 0;
    }

    private bool IsColliderWithinCircle(Collider2D col, Vector2 center, float radius)
    {
        Vector2 p = col.ClosestPoint(center);
        float dx = p.x - center.x;
        float dy = p.y - center.y;
        float d2 = dx * dx + dy * dy;
        float r2 = radius * radius;
        return d2 <= r2;
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugDrawSwordGizmo) return;

        Gizmos.color = debugSwordColor;

        int face = 1;
        if (transform.lossyScale.x < 0f) face = -1;
        float baseAngle = face > 0 ? 0f : 180f;
        float s = face > 0 ? settings.swordStartAngle : -settings.swordStartAngle;
        float e = face > 0 ? settings.swordEndAngle : -settings.swordEndAngle;
        float a0 = baseAngle + s;
        float a1 = baseAngle + e;

        float step = Mathf.Max(1f, debugArcStepDeg);
        int steps = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(a1 - a0) / step) + 1);

        Vector3 prev = BladeTipAtAngle(a0);
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            float ang = Mathf.Lerp(a0, a1, t);
            Vector3 curr = BladeTipAtAngle(ang);
            Gizmos.DrawLine(prev, curr);
            prev = curr;
        }

        DrawBladeBoxAtAngle(a0);
        DrawBladeBoxAtAngle(a1);
    }

    private Vector3 BladeTipAtAngle(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new(Mathf.Cos(rad), Mathf.Sin(rad));
        return transform.position + (Vector3)(dir * settings.swordBladeLength);
    }

    private void DrawBladeBoxAtAngle(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new(Mathf.Cos(rad), Mathf.Sin(rad));
        Vector3 c = transform.position + (Vector3)(dir * (settings.swordBladeLength * 0.5f));
        float hl = settings.swordBladeLength * 0.5f;
        float ht = settings.swordBladeThickness * 0.5f;

        Vector2 x = new(Mathf.Cos(rad), Mathf.Sin(rad));
        Vector2 y = new(-Mathf.Sin(rad), Mathf.Cos(rad));

        Vector3 p1 = c + (Vector3)(x * hl) + (Vector3)(y * ht);
        Vector3 p2 = c + (Vector3)(x * hl) - (Vector3)(y * ht);
        Vector3 p3 = c - (Vector3)(x * hl) - (Vector3)(y * ht);
        Vector3 p4 = c - (Vector3)(x * hl) + (Vector3)(y * ht);

        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
    }

    public void UpdateVolleyLaserLine(Vector2 origin, Vector2 dir, float length, bool firing)
    {
        if (Line == null) return;

        Line.useWorldSpace = true;
        Line.positionCount = 2;
        Line.widthMultiplier = settings.laserThickness;

        Vector3 a = origin;
        Vector3 b = a + (Vector3)(dir.normalized * length);

        Line.SetPosition(0, a);
        Line.SetPosition(1, b);

        Color c = firing ? LaserFiringColor : LaserWarningColor;
        Line.startColor = c;
        Line.endColor = c;
    }

    public void ClearVolleyLaserLine()
    {
        if (Line == null) return;
        Line.positionCount = 0;
    }

    public void UpdateRadialLaserLines(Vector2 origin, Vector2[] dirs, float length, bool firing)
    {
        if (Line == null) return;

        if (dirs == null || dirs.Length == 0)
        {
            Line.positionCount = 0;
            return;
        }

        Line.useWorldSpace = true;
        Line.widthMultiplier = settings.laserThickness;

        int count = dirs.Length * 2;
        Line.positionCount = count;

        Color c = firing ? LaserFiringColor : LaserWarningColor;
        Line.startColor = c;
        Line.endColor = c;

        Vector3 o = origin;
        int idx = 0;

        for (int i = 0; i < dirs.Length; i++)
        {
            Vector2 d = dirs[i];
            if (d.sqrMagnitude < 0.0001f) d = Vector2.right;
            d.Normalize();

            Vector3 tip = o + (Vector3)(d * length);

            Line.SetPosition(idx++, o);
            Line.SetPosition(idx++, tip);
        }
    }

    public void ClearRadialLaserLines()
    {
        if (Line == null) return;
        Line.positionCount = 0;
    }
}