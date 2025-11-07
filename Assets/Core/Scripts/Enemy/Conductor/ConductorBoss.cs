using System;
using UnityEngine;

public sealed class ConductorBoss : EnemyBase, IParryReactive
{
    public sealed class LaserConfig
    {
        public Collider2D LaserCollider;
        public float Windup;
        public float Active;
        public int Damage;
    }

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
    public const string AnimSwoop = "Swoop";
    public const string AnimGroundRush = "Ground Rush";
    public const string AnimFire = "Fire";
    public const string AnimCrackLaser = "Crack Laser";
    public const string AnimDeath = "Death";

    [Header("Common")]
    [SerializeField] private LayerMask playerHitMask;
    [SerializeField] private int p1Stacks = 3;
    [SerializeField] private int p2Stacks = 1;
    [SerializeField] private float idleDelay = 0.6f;
    [SerializeField] private float groggyDuration = 2.2f;

    [Header("Sword Drop")]
    [SerializeField] private Transform leftTop;
    [SerializeField] private Transform rightTop;
    [SerializeField] private int swordDamage = 16;
    [SerializeField] private float swordStartAngle = 60f;
    [SerializeField] private float swordEndAngle = -80f;
    [SerializeField] private float swordBladeLength = 3.2f;
    [SerializeField] private float swordBladeThickness = 0.3f;

    [Header("Plunge + Rush")]
    [SerializeField] private Collider2D plungeCollider;
    [SerializeField] private Transform ceilingPoint;
    [SerializeField] private float plungeTeleTime = 0.45f;
    [SerializeField] private float plungeActiveTime = 0.28f;
    [SerializeField] private int plungeDamage = 18;
    [SerializeField] private Collider2D rushCollider;
    [SerializeField] private float rushSpeed = 12f;
    [SerializeField] private float rushMaxTime = 2.0f;
    [SerializeField] private float missBehindTime = 0.6f;
    [SerializeField] private int rushDamage = 14;

    [Header("Missile + Laser")]
    [SerializeField] private ConductorMissile missilePrefab;
    [SerializeField] private Transform[] missileMuzzles;
    [SerializeField] private int missileVolleys = 3;
    [SerializeField] private float missileVolleyInterval = 0.5f;
    [SerializeField] private Collider2D chestLaserCollider;
    [SerializeField] private float laserWindupTime = 0.6f;
    [SerializeField] private float laserActiveTime = 1.2f;
    [SerializeField] private int laserDamage = 12;

    [Header("P2 Radial")]
    [SerializeField] private Collider2D radialLaserCollider;
    [SerializeField] private int radialSets = 12;
    [SerializeField] private float radialBeat = 0.25f;
    [SerializeField] private float radialActiveEach = 0.18f;
    [SerializeField] private int radialDamage = 12;

    [Header("Debug Sword Gizmo")]
    [SerializeField] private bool debugDrawSwordGizmo = true;
    [SerializeField] private Color debugSwordColor = new Color(1f, 0.3f, 0.2f, 0.8f);
    [SerializeField] private float debugArcStepDeg = 5f;
    [SerializeField] private LineRenderer swingLine;

    private readonly Collider2D[] overlapResults = new Collider2D[8];
    private BossStateMachine stateMachine;
    private bool lethalActive;
    private AttackContext attackCx;

    protected override string DeathAnimName
    {
        get { return AnimDeath; }
    }

    protected override void Start()
    {
        base.Start();
        stateMachine = new BossStateMachine();
        stateMachine.Initialize(new BossIdleState(this, stateMachine, idleDelay));
    }

    protected override void OnUpdate()
    {
        stateMachine.Update();
    }

    protected override void OnFixedUpdate()
    {
        stateMachine.FixedUpdate();
    }

    public BossStateType CurrentStateType
    {
        get { return stateMachine != null ? stateMachine.CurrentStateType : BossStateType.Missing; }
    }

    public void ChangeToIdle(float delay)
    {
        stateMachine.ChangeState(new BossIdleState(this, stateMachine, delay));
    }

    public void ChangeToGroggy(float duration)
    {
        stateMachine.ChangeState(new BossGroggyState(this, stateMachine, duration));
    }

    public void ChangeToSwordDrop()
    {
        stateMachine.ChangeState
        (
            new BossSwordDropState
            (
                this,
                stateMachine,
                leftTop,
                rightTop,
                swordDamage,
                swordStartAngle,
                swordEndAngle,
                swordBladeLength,
                swordBladeThickness
            )
        );
    }

    public void ChangeToPlungeRush()
    {
        stateMachine.ChangeState(new BossPlungeRushState(this, stateMachine, ceilingPoint, plungeCollider, plungeTeleTime, plungeActiveTime, plungeDamage, rushCollider, rushSpeed, rushMaxTime, missBehindTime, rushDamage));
    }

    public void ChangeToVolleyLaser()
    {
        stateMachine.ChangeState(new BossVolleyLaserState(this, stateMachine, missilePrefab, missileMuzzles, missileVolleys, missileVolleyInterval, new LaserConfig { LaserCollider = chestLaserCollider, Windup = laserWindupTime, Active = laserActiveTime, Damage = laserDamage }));
    }

    public void ChangeToRadialLaser()
    {
        stateMachine.ChangeState(new BossRadialLaserState(this, stateMachine, radialLaserCollider, radialSets, radialBeat, radialActiveEach, radialDamage));
    }

    public void ChangeToDeath()
    {
        stateMachine.ChangeState(new BossDeathState(this, stateMachine));
    }

    public void DecideP1()
    {
        //int r = Random.Range(0, 3);
        //if (r == 0) ChangeToSwordDrop();
        //else if (r == 1) ChangeToPlungeRush();
        //else ChangeToVolleyLaser();
        ChangeToSwordDrop();
    }

    public void SetVelocityX(float v)
    {
        Body.linearVelocity = new Vector2(v, Body.linearVelocity.y);
    }

    public void StopHorizontal()
    {
        Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
    }

    public int HandleHitbox(Collider2D hitCol, int damage)
    {
        Player.GetParryDetectCircle(out Vector2 pCenter, out float pRadius);
        if (IsColliderWithinCircle(hitCol, pCenter, pRadius)) Player.RegisterParryCandidate(this, hitCol.bounds.center, damage);
        Player.GetDashDetectCircle(out Vector2 dCenter, out float dRadius);
        if (IsColliderWithinCircle(hitCol, dCenter, dRadius)) Player.RegisterDashCandidate(hitCol.bounds.center);

        ContactFilter2D f = new ContactFilter2D();
        f.SetLayerMask(playerHitMask);
        f.useTriggers = true;
        int hitCount = hitCol.Overlap(f, overlapResults);
        for (int i = 0; i < hitCount; i++)
        {
            PlayerController pc = overlapResults[i].GetComponentInParent<PlayerController>();
            if (pc == Player)
            {
                Player.Hit(damage, hitCol.bounds.center);
                Player.ClearParryCandidate(this);
                SetLethal(AttackContext.None, false);
                return 1;
            }
        }
        return 0;
    }

    public Transform ChooseSideTop()
    {
        float px = Player.transform.position.x;
        float dl = Mathf.Abs(px - leftTop.position.x);
        float dr = Mathf.Abs(px - rightTop.position.x);
        if (dl < dr) return leftTop; else return rightTop;
    }

    public void OnMissileReflectedHit()
    {
        if (HasP1Stacks)
        {
            ConsumeP1Stack();
            ChangeToGroggy(groggyDuration);
        }
        else
        {
            if (HasP2Stacks)
            {
                ConsumeP2Stack();
                ChangeToGroggy(groggyDuration);
            }
            else ChangeToDeath();
        }
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
        if (attackCx == AttackContext.LaserP2)
        {
            ChangeToDeath();
            return;
        }
        if (HasP1Stacks)
        {
            ConsumeP1Stack();
            ChangeToGroggy(groggyDuration);
            return;
        }
        if (HasP2Stacks)
        {
            ConsumeP2Stack();
            ChangeToGroggy(groggyDuration);
            return;
        }
        ChangeToDeath();
    }

    public bool HasP1Stacks
    {
        get { return p1Stacks > 0; }
    }

    public void ConsumeP1Stack()
    {
        if (p1Stacks > 0) p1Stacks -= 1;
    }

    public bool HasP2Stacks
    {
        get { return p2Stacks > 0; }
    }

    public void ConsumeP2Stack()
    {
        if (p2Stacks > 0) p2Stacks -= 1;
    }

    public bool LethalActive
    {
        get { return lethalActive; }
    }

    public void SetLethal(AttackContext cx, bool on)
    {
        attackCx = cx;
        lethalActive = on;
        if (on) Player.ClearParryCandidate(this);
    }

    public float FacingDir
    {
        get { return FacingDirection; }
    }

    public PlayerController PlayerTarget
    {
        get { return Player; }
    }

    public LayerMask PlayerHitMask
    {
        get { return playerHitMask; }
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
        float s = face > 0 ? swordStartAngle : -swordStartAngle;
        float e = face > 0 ? swordEndAngle : -swordEndAngle;
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
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        return transform.position + (Vector3)(dir * swordBladeLength);
    }

    public void DebugUpdateSwingLine(Vector2 origin, Vector2 dir, float length)
    {
        if (swingLine == null) throw new System.InvalidOperationException("ConductorBoss.swingLine is null. Assign a LineRenderer in the inspector.");
        swingLine.widthMultiplier = swordBladeThickness;
        swingLine.useWorldSpace = true;
        swingLine.positionCount = 2;
        Vector3 a = origin;
        Vector3 b = origin + dir.normalized * length;
        swingLine.SetPosition(0, a);
        swingLine.SetPosition(1, b);
    }

    public void DebugClearSwingLine()
    {
        if (swingLine == null) throw new System.InvalidOperationException("ConductorBoss.swingLine is null. Assign a LineRenderer in the inspector.");
        swingLine.positionCount = 0;
    }

    private void DrawBladeBoxAtAngle(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        Vector3 c = transform.position + (Vector3)(dir * (swordBladeLength * 0.5f));
        float hl = swordBladeLength * 0.5f;
        float ht = swordBladeThickness * 0.5f;
        Vector2 x = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        Vector2 y = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));
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