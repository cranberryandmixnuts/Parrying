using System;
using UnityEngine;
using UnityEngine.Splines;
using Sirenix.OdinInspector;
using System.Collections;

public sealed class BossController : EnemyBase, IParryReactive, IEnemyProjectileOwner
{
    public enum AttackContext
    {
        None,
        Sword,
        CurvedSlash,
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
    public const string AnimLanding = "Land";
    public const string AnimGroundRush = "Ground Rush";
    public const string AnimFire = "Fire";
    public const string AnimCrackLaser = "Crack Laser";
    public const string AnimDeath = "Death";
    public const string AnimCurvedSlashDown = "Curved Slash Down";
    public const string AnimCurvedSlashUp = "Curved Slash Up";
    public const string AnimExternalRush = "External Rush";

    [TabGroup("Boss Controller", "Runtime"), BoxGroup("Boss Controller/Runtime/Stacks"), ReadOnly, SerializeField]
    private int p1Stacks;

    [TabGroup("Boss Controller", "Runtime"), BoxGroup("Boss Controller/Runtime/Phase 1"), ReadOnly, SerializeField]
    private int p1CountersTowardCurvedSlash;

    [TabGroup("Boss Controller", "Runtime"), BoxGroup("Boss Controller/Runtime/Phase 1"), ReadOnly, SerializeField]
    private int p1CurvedSlashRemaining;

    [TabGroup("Boss Controller", "Runtime"), BoxGroup("Boss Controller/Runtime/Phase 1"), ReadOnly, SerializeField]
    private bool pendingCurvedSlash;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/GlobalRefs"), SerializeField, Required]
    private BossSettings settings;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/GlobalRefs"), SerializeField, Required]
    private BossTeleportManager teleportManager;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/GlobalRefs"), SerializeField, Required]
    private Collider2D externalRushBounds;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/GlobalRefs"), SerializeField, Required]
    private Collider2D projectileHitbox;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/GlobalRefs"), SerializeField, Required]
    private Transform arenaCenter;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/SwordDrop"), SerializeField, Required]
    private Transform leftTop;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/SwordDrop"), SerializeField, Required]
    private Transform rightTop;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/SwordDrop"), SerializeField, Required]
    private Transform swordVisual;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/CurvedSlash Path"), SerializeField, Required]
    private SplineContainer rightStandardDownPath;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/CurvedSlash Path"), SerializeField, Required]
    private SplineContainer rightStandardUpPath;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/CurvedSlash Path"), SerializeField, Required]
    private SplineContainer leftStandardDownPath;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/CurvedSlash Path"), SerializeField, Required]
    private SplineContainer leftStandardUpPath;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/PlungeRush"), SerializeField, Required]
    private Transform ceilingPoint;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/PlungeRush"), SerializeField, Required]
    private Transform rushStopLeft;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/PlungeRush"), SerializeField, Required]
    private Transform rushStopRight;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/PlungeRush"), SerializeField, Required]
    private Collider2D plungeCollider;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/PlungeRush"), SerializeField, Required]
    private Collider2D rushCollider;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/ExternalRush"), SerializeField, Required]
    private Transform externalRushSpawnLeft;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/ExternalRush"), SerializeField, Required]
    private Transform externalRushSpawnTop;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/ExternalRush"), SerializeField, Required]
    private Transform externalRushSpawnRight;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/VolleyLaser"), SerializeField]
    private EnemyProjectile projectilePrefab;

    [TabGroup("Boss Controller", "Setup"), BoxGroup("Boss Controller/Setup/Volley&RadialLaser"), SerializeField, Required]
    private Transform laserHeightPoint;

    [Header("Debug")]
    [SerializeField] private Color debugSwordColor = new(1f, 0.3f, 0.2f, 0.8f);
    [SerializeField] private float debugArcStepDeg = 5f;
    [SerializeField] private LineRenderer Line;

    private float gravityOriginal;
    private readonly Collider2D[] overlapResults = new Collider2D[8];
    private BossStateMachine stateMachine;
    private bool lethalActive;
    private AttackContext attackCx;
    private int lastPatternP1 = -1;
    private int lastPatternP1Streak = 0;

    protected override string DeathAnimName => AnimDeath;

    protected override void Start()
    {
        base.Start();

        gravityOriginal = Body.gravityScale;

        p1Stacks = settings.P1TotalStacks;
        p1CountersTowardCurvedSlash = 0;
        p1CurvedSlashRemaining = settings.p1PatternBundleRepeatCount;
        pendingCurvedSlash = false;

        ResetSwordVisual();

        stateMachine = new BossStateMachine();
        stateMachine.Initialize(new BossIdleState(this, stateMachine, true));
        FacePlayer();
    }

    protected override void OnUpdate() => stateMachine.Update();

    protected override void OnFixedUpdate() => stateMachine.FixedUpdate();

    #region Properties
    public BossStateType CurrentStateType => stateMachine.CurrentStateType;
    public BossSettings Settings => settings;
    public Transform LeftTop => leftTop;
    public Transform RightTop => rightTop;
    public SplineContainer RightStandardDownPath => rightStandardDownPath;
    public SplineContainer RightStandardUpPath => rightStandardUpPath;
    public SplineContainer LeftStandardDownPath => leftStandardDownPath;
    public SplineContainer LeftStandardUpPath => leftStandardUpPath;
    public Transform CeilingPoint => ceilingPoint;
    public Transform RushStopLeft => rushStopLeft;
    public Transform RushStopRight => rushStopRight;
    public Collider2D PlungeCollider => plungeCollider;
    public Collider2D RushCollider => rushCollider;
    public Transform ExternalRushSpawnLeft => externalRushSpawnLeft;
    public Transform ExternalRushSpawnTop => externalRushSpawnTop;
    public Transform ExternalRushSpawnRight => externalRushSpawnRight;
    public Collider2D ExternalRushBounds => externalRushBounds;
    public EnemyProjectile MissilePrefab => projectilePrefab;
    public Transform VolleyCenter => arenaCenter;
    public Transform VolleyHeight => laserHeightPoint;
    public Transform RadialLaserPoint => laserHeightPoint;
    public bool LethalActive => lethalActive;
    public float FacingDir => FacingDirection;
    public float OriginalGravityScale => gravityOriginal;
    public PlayerController PlayerTarget => Player;
    public Transform ProjectileTargetTransform => transform;
    public Collider2D ProjectileHitbox => projectileHitbox;
    #endregion

    #region ChangeStateMethods
    public void ChangeToIdle(bool grounded) => stateMachine.ChangeState(new BossIdleState(this, stateMachine, grounded));
    public void ChangeToGroggy(float duration) => stateMachine.ChangeState(new BossGroggyState(this, stateMachine, duration));
    public void ChangeToSwordDrop() => stateMachine.ChangeState(new BossSwordDropState(this, stateMachine));
    public void ChangeToPlungeRush() => stateMachine.ChangeState(new BossPlungeRushState(this, stateMachine));
    public void ChangeToVolleyLaser() => stateMachine.ChangeState(new BossVolleyLaserState(this, stateMachine));
    public void ChangeToCurvedSlash() => stateMachine.ChangeState(new BossCurvedSlashState(this, stateMachine));
    public void ChangeToRadialLaser() => stateMachine.ChangeState(new BossRadialLaserState(this, stateMachine));
    public void ChangeToDeath() => stateMachine.ChangeState(new BossDeathState(this, stateMachine));
    #endregion

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

        if (attackCx == AttackContext.LaserP2)
        {
            ChangeToDeath();
            return;
        }

        if (HasP1Stacks)
        {
            bool fromCurvedSlash = stateMachine.CurrentStateType == BossStateType.CurvedSlash;
            ConsumeP1Stack(!fromCurvedSlash);
            ChangeToGroggy(settings.groggyDuration);
            return;
        }

        ChangeToDeath();
    }

    public void OnHitByReflectedProjectile()
    {
        if (HasP1Stacks)
        {
            ConsumeP1Stack(true);
            ChangeToGroggy(settings.groggyDuration);
            return;
        }

        ChangeToDeath();
    }

    public void DecideP1()
    {
        if (!HasP1Stacks)
        {
            ChangeToRadialLaser();
            return;
        }

        if (pendingCurvedSlash)
        {
            pendingCurvedSlash = false;
            if (p1CurvedSlashRemaining > 0) p1CurvedSlashRemaining -= 1;
            ChangeToCurvedSlash();
            return;
        }

        int pattern = GetNextP1Pattern();
        pattern = 2; // TEMP

        switch (pattern)
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

    private int GetNextP1Pattern()
    {
        if (lastPatternP1 < 0)
            return ChooseInitialP1Pattern();

        if (lastPatternP1Streak >= 3)
            return ChooseDifferentPattern();

        float[] weights = new float[3];

        for (int i = 0; i < 3; i++)
            weights[i] = 1f;

        float penaltyFactor = Mathf.Pow(settings.repeatPenalty, lastPatternP1Streak);
        weights[lastPatternP1] *= penaltyFactor;

        int pattern = GetWeightedRandomIndex(weights);

        if (pattern == lastPatternP1)
            lastPatternP1Streak++;
        else
        {
            lastPatternP1 = pattern;
            lastPatternP1Streak = 1;
        }

        return pattern;
    }

    private int ChooseInitialP1Pattern()
    {
        int pattern = UnityEngine.Random.Range(0, 3);
        lastPatternP1 = pattern;
        lastPatternP1Streak = 1;
        return pattern;
    }

    private int ChooseDifferentPattern()
    {
        int pattern = lastPatternP1;

        while (pattern == lastPatternP1)
            pattern = UnityEngine.Random.Range(0, 3);

        lastPatternP1 = pattern;
        lastPatternP1Streak = 1;
        return pattern;
    }

    private int GetWeightedRandomIndex(float[] weights)
    {
        float total = 0f;

        for (int i = 0; i < weights.Length; i++)
            total += weights[i];

        float r = UnityEngine.Random.Range(0f, total);
        float cumulative = 0f;

        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (r <= cumulative)
                return i;
        }

        return weights.Length - 1;
    }

    private void ConsumeP1Stack(bool countTowardCurvedSlash)
    {
        if (p1Stacks > 0) p1Stacks -= 1;

        if (!countTowardCurvedSlash) return;
        if (pendingCurvedSlash) return;
        if (p1CurvedSlashRemaining <= 0) return;

        p1CountersTowardCurvedSlash += 1;
        if (p1CountersTowardCurvedSlash < settings.p1CountersToTriggerCurvedSlash) return;

        pendingCurvedSlash = true;
        p1CountersTowardCurvedSlash = 0;
    }

    public bool HasP1Stacks => p1Stacks > 0;

    public void SetLethal(AttackContext cx, bool on)
    {
        AttackContext prev = attackCx;
        attackCx = cx;
        lethalActive = on;
        Player.ClearParryCandidate(this);

        if (!on && (prev == AttackContext.Sword || prev == AttackContext.CurvedSlash))
            DebugClearSwingLine();
    }

    public IEnumerator TeleportRoutine(Vector3 p, Action onTeleported = null) => teleportManager.PlayTeleportSequence(p, onTeleported);

    public void CancelTeleportEffects() => teleportManager.ForceReset();

    public void Play(string anim) => Anim.Play(anim);

    public float AnimLen(string anim) => GetAnimLength(anim);

    public void FaceToPlayer() => FacePlayer();

    public void FaceTo(int dir) => ApplyFacing(dir < 0 ? -1 : 1);

    public void ResetRotationToFacing() => transform.rotation = Quaternion.Euler(0f, FacingDirection == -1 ? 180f : 0f, 0f);

    public void SetGravityScale(float v) => Body.gravityScale = v;

    public float GetGravityScale() => Body.gravityScale;

    public void SetVelocityX(float v) => Body.linearVelocity = new Vector2(v, Body.linearVelocity.y);

    public void SetVelocityY(float y) => Body.linearVelocity = new Vector2(Body.linearVelocity.x, y);

    public float GetVelocityY() => Body.linearVelocity.y;

    public bool IsTouchingGround() => Body.IsTouchingLayers(settings.groundLayer);

    public void StopHorizontal() => Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);

    public void SetSwordVisualActive(bool active) => swordVisual.gameObject.SetActive(active);

    public void SetSwordVisualLocalAngle(float angle) => swordVisual.localRotation = Quaternion.Euler(0f, 0f, angle);

    public void ResetSwordVisual()
    {
        SetSwordVisualLocalAngle(0f);
        SetSwordVisualActive(false);
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
        DebugUpdateSwingLine(origin, dir, length, settings.swordBladeThickness);
    }

    public void DebugUpdateSwingLine(Vector2 origin, Vector2 dir, float length, float thickness)
    {
        if (Line == null) throw new InvalidOperationException("ConductorBoss.swingLine is null. Assign a LineRenderer in the inspector.");
        Line.widthMultiplier = thickness;
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
}