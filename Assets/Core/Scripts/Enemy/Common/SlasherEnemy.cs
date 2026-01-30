using UnityEngine;
using Sirenix.OdinInspector;

public sealed class SlasherEnemy : EnemyBase, IParryReactive
{
    private enum State
    {
        Idle,
        Walk,
        Chase,
        BackWalk,
        Attack,
        Hit
    }

    private const string AnimIdle = "Idle";
    private const string AnimWalk = "Walk";
    private const string AnimChase = "Chase";
    private const string AnimBackWalk = "BackWalk";
    private const string AnimAttack = "Attack";
    private const string AnimHit = "Hit";
    private const string AnimDeath = "Death";

    [TabGroup("Slasher Enemy", "Setup"), BoxGroup("Slasher Enemy/Setup/Ranges"), SerializeField, Required]
    private Collider2D walkRange;

    [TabGroup("Slasher Enemy", "Setup"), BoxGroup("Slasher Enemy/Setup/Ranges"), SerializeField, Required]
    private Collider2D backOffRange;

    [TabGroup("Slasher Enemy", "Setup"), BoxGroup("Slasher Enemy/Setup/Attack"), SerializeField, Required]
    private Transform attackOrigin;

    [TabGroup("Slasher Enemy", "Setup"), BoxGroup("Slasher Enemy/Setup/Attack"), SerializeField]
    private LayerMask playerHitMask;

    [TabGroup("Slasher Enemy", "Tuning"), BoxGroup("Slasher Enemy/Tuning/Movement"), SerializeField, MinValue(0f), SuffixLabel("u/s", true)]
    private float walkSpeed = 1.5f;

    [TabGroup("Slasher Enemy", "Tuning"), BoxGroup("Slasher Enemy/Tuning/Movement"), SerializeField, MinValue(0f), SuffixLabel("u/s", true)]
    private float chaseSpeed = 4f;

    [TabGroup("Slasher Enemy", "Tuning"), BoxGroup("Slasher Enemy/Tuning/Attack Geometry"), SerializeField, SuffixLabel("deg", true)]
    private float swingStartAngleDeg = 75f;

    [TabGroup("Slasher Enemy", "Tuning"), BoxGroup("Slasher Enemy/Tuning/Attack Geometry"), SerializeField, SuffixLabel("deg", true)]
    private float swingEndAngleDeg = -30f;

    [TabGroup("Slasher Enemy", "Tuning"), BoxGroup("Slasher Enemy/Tuning/Attack Geometry"), SerializeField, MinValue(0f), SuffixLabel("u", true)]
    private float swingLength = 2f;

    [TabGroup("Slasher Enemy", "Tuning"), BoxGroup("Slasher Enemy/Tuning/Attack Timing"), SerializeField, PropertyRange(0f, 1f), SuffixLabel("%", true)]
    private float attackPrepPercent = 0.2f;

    [TabGroup("Slasher Enemy", "Tuning"), BoxGroup("Slasher Enemy/Tuning/Attack Timing"), SerializeField, PropertyRange(0f, 1f), SuffixLabel("%", true)]
    private float attackEndPercent = 0.9f;

    [TabGroup("Slasher Enemy", "Tuning"), BoxGroup("Slasher Enemy/Tuning/Damage"), SerializeField, MinValue(0), SuffixLabel("HP", true)]
    private int attackDamage = 10;

    [Header("Debug")]
    [SerializeField] private LineRenderer swingLine;
    [SerializeField] private float swingLineWidth = 0.05f;

    private State state;
    private float stateTimer;
    private float attackCooldownTimer;
    private int attackPhase;
    private float attackPhaseTimer;
    private bool attackResolved;

    private float attackWindupRuntime;
    private float swingDurationRuntime;
    private float attackRecoverRuntime;

    protected override string DeathAnimName => AnimDeath;

    protected override void Awake()
    {
        base.Awake();

        DeathDespawnDelay = -1f;

        state = State.Chase;
        stateTimer = 0f;
        attackCooldownTimer = 0f;
        attackPhase = 0;
        attackPhaseTimer = 0f;
        attackResolved = false;

        Anim.Play(AnimChase);

        swingLine.useWorldSpace = true;
        swingLine.positionCount = 0;
        swingLine.startWidth = swingLineWidth;
        swingLine.endWidth = swingLineWidth;
    }

    public override void Die()
    {
        if (IsDead()) return;
        ClearSwingLine();
        base.Die();
    }

    protected override void OnUpdate()
    {
        if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;

        if (state != State.Attack && state != State.Hit) FacePlayer();

        switch (state)
        {
            case State.Idle:
                UpdateIdle();
                break;
            case State.Walk:
                UpdateWalk();
                break;
            case State.Chase:
                UpdateChase();
                break;
            case State.BackWalk:
                UpdateBackWalk();
                break;
            case State.Attack:
                UpdateAttack();
                break;
            case State.Hit:
                UpdateHit();
                break;
        }
    }

    protected override void OnFixedUpdate()
    {
        switch (state)
        {
            case State.Walk:
                MoveTowardsPlayer(walkSpeed);
                break;
            case State.Chase:
                MoveTowardsPlayer(chaseSpeed);
                break;
            case State.BackWalk:
                MoveAwayFromPlayer(walkSpeed);
                break;
            default:
                StopHorizontal();
                break;
        }
    }

    private void UpdateIdle()
    {
        ChooseMovementState();
    }

    private void UpdateWalk()
    {
        ChooseMovementState();
    }

    private void UpdateChase()
    {
        ChooseMovementState();
    }

    private void UpdateBackWalk()
    {
        if (attackCooldownTimer <= 0f && IsPlayerInSwingCone())
        {
            EnterState(State.Attack);
            return;
        }

        if (!InBackOffRange())
        {
            ChooseMovementState();
            return;
        }

        if (!IsPlayerInSwingCone())
        {
            ChooseMovementState();
            return;
        }
    }

    private void UpdateAttack()
    {
        if (attackPhase == 0)
        {
            ClearSwingLine();
            RegisterDashAssistRay();
            attackPhaseTimer -= Time.deltaTime;
            if (attackPhaseTimer <= 0f)
            {
                attackPhase = 1;
                attackPhaseTimer = swingDurationRuntime;
                attackResolved = false;
            }
            return;
        }

        if (attackPhase == 1)
        {
            PerformSwingStep();
            attackPhaseTimer -= Time.deltaTime;
            if (attackPhaseTimer <= 0f)
            {
                attackPhase = 2;
                attackPhaseTimer = attackRecoverRuntime;
                StartAttackCooldown();
                Player.ClearParryCandidate(this);
                ClearSwingLine();
            }
            return;
        }

        if (attackPhase == 2)
        {
            ClearSwingLine();
            attackPhaseTimer -= Time.deltaTime;
            if (attackPhaseTimer <= 0f) ChooseMovementState();
        }
    }

    private void RegisterDashAssistRay()
    {
        Vector2 originPos = attackOrigin != null ? (Vector2)attackOrigin.position : (Vector2)transform.position;

        float angleDeg = swingStartAngleDeg;
        if (FacingDirection < 0) angleDeg = -angleDeg;

        Vector2 dir = DirFromAngle(angleDeg);
        Vector2 segEnd = originPos + dir * swingLength;

        Player.GetDashDetectCircle(out Vector2 dashCenter, out float dashRadius);
        if (SegmentIntersectsCircle(originPos, segEnd, dashCenter, dashRadius)) Player.RegisterDashCandidate(segEnd);
    }

    private void UpdateHit()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f) ChooseMovementState();
    }

    private void EnterState(State newState)
    {
        if (state == newState) return;

        state = newState;

        switch (state)
        {
            case State.Idle:
                Anim.Play(AnimIdle);
                break;
            case State.Walk:
                Anim.Play(AnimWalk);
                break;
            case State.Chase:
                Anim.Play(AnimChase);
                break;
            case State.BackWalk:
                Anim.Play(AnimBackWalk);
                break;
            case State.Attack:
                Anim.Play(AnimAttack);

                float clipLen = GetAnimLength(AnimAttack);

                if (attackPrepPercent >= attackEndPercent)
                {
                    Debug.LogError("SlasherEnemy: attackPrepPercent must be < attackEndPercent. Resetting to 0.25 / 0.75.");
                    attackPrepPercent = 0.25f;
                    attackEndPercent = 0.75f;
                }

                attackWindupRuntime = clipLen * attackPrepPercent;
                swingDurationRuntime = clipLen * (attackEndPercent - attackPrepPercent);
                attackRecoverRuntime = clipLen - (attackWindupRuntime + swingDurationRuntime);
                if (attackRecoverRuntime < 0.01f) attackRecoverRuntime = 0.01f;

                attackPhase = 0;
                attackPhaseTimer = attackWindupRuntime;
                attackResolved = false;
                ClearSwingLine();
                break;

            case State.Hit:
                Anim.Play(AnimHit);
                stateTimer = GetAnimLength(AnimHit);
                ClearSwingLine();
                break;
        }
    }

    private void ChooseMovementState()
    {
        bool inCone = IsPlayerInSwingCone();
        bool inWalk = InWalkRange();
        bool inBack = InBackOffRange();

        State nextState;

        if (inCone)
        {
            if (attackCooldownTimer <= 0f)
            {
                nextState = State.Attack;
            }
            else
            {
                if (inBack) nextState = State.BackWalk;
                else nextState = State.Idle;
            }
        }
        else
        {
            if (inWalk) nextState = State.Walk;
            else nextState = State.Chase;
        }

        EnterState(nextState);
    }

    private void PerformSwingStep()
    {
        if (attackResolved) return;

        Vector2 originPos = (Vector2)attackOrigin.position;

        float norm = 1f - (attackPhaseTimer / swingDurationRuntime);
        if (norm < 0f) norm = 0f;
        if (norm > 1f) norm = 1f;

        float angleDeg = Mathf.Lerp(swingStartAngleDeg, swingEndAngleDeg, norm);
        if (FacingDirection < 0) angleDeg = -angleDeg;

        Vector2 dir = DirFromAngle(angleDeg);
        Vector2 segEnd = originPos + dir * swingLength;

        Player.GetParryDetectCircle(out Vector2 parryCenter, out float parryRadius);
        if (SegmentIntersectsCircle(originPos, segEnd, parryCenter, parryRadius))
            Player.RegisterParryCandidate(this, segEnd, attackDamage);

        Player.GetDashDetectCircle(out Vector2 dashCenter, out float dashRadius);
        if (SegmentIntersectsCircle(originPos, segEnd, dashCenter, dashRadius))
            Player.RegisterDashCandidate(segEnd);

        RaycastHit2D hit = Physics2D.Raycast(originPos, dir, swingLength, playerHitMask);
        if (hit.collider != null)
        {
            if (Player.TryHit(attackDamage, originPos + dir * hit.distance))
            {
                UpdateSwingLine(originPos, dir, hit.distance);
                attackResolved = true;
                StartAttackCooldown();
                Player.ClearParryCandidate(this);
                ClearSwingLine();
            }
            else UpdateSwingLine(originPos, dir, swingLength);
        }
        else UpdateSwingLine(originPos, dir, swingLength);
    }

    private bool SegmentIntersectsCircle(Vector2 a, Vector2 b, Vector2 c, float r)
    {
        Vector2 ab = b - a;
        float abLenSq = ab.sqrMagnitude;
        float t = Vector2.Dot(c - a, ab) / abLenSq;

        if (t < 0f) t = 0f;
        else if (t > 1f) t = 1f;

        Vector2 closest = a + ab * t;
        float distSq = (c - closest).sqrMagnitude;
        float rSq = r * r;

        if (distSq <= rSq) return true;
        return false;
    }

    private void StartAttackCooldown()
    {
        attackCooldownTimer = Random.Range(1.5f, 3f);
    }

    private void MoveTowardsPlayer(float speed)
    {
        Vector2 a = transform.position;
        Vector2 b = Player.transform.position;

        float dx = b.x - a.x;

        float dirSign = 0f;
        if (dx > 0f) dirSign = 1f;
        else if (dx < 0f) dirSign = -1f;

        Body.linearVelocity = new Vector2(dirSign * speed, Body.linearVelocity.y);
    }

    private void MoveAwayFromPlayer(float speed)
    {
        Vector2 a = transform.position;
        Vector2 b = Player.transform.position;

        float dx = b.x - a.x;

        float dirSign = 0f;
        if (dx > 0f) dirSign = 1f;
        else if (dx < 0f) dirSign = -1f;

        Body.linearVelocity = new Vector2(-dirSign * speed, Body.linearVelocity.y);
    }

    private void StopHorizontal()
    {
        Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
    }

    private bool InWalkRange()
    {
        return walkRange.OverlapPoint(Player.transform.position);
    }

    private bool InBackOffRange()
    {
        return backOffRange.OverlapPoint(Player.transform.position);
    }

    private bool IsPlayerInSwingCone()
    {
        Vector2 originPos = attackOrigin != null ? (Vector2)attackOrigin.position : (Vector2)transform.position;
        Vector2 toPlayer = (Vector2)Player.transform.position - originPos;

        float dist = toPlayer.magnitude;
        if (dist > swingLength) return false;

        Vector2 forward = Vector2.right * FacingDirection;
        float ang = Vector2.SignedAngle(forward, toPlayer);

        float minAng;
        float maxAng;

        if (FacingDirection >= 0)
        {
            minAng = Mathf.Min(swingStartAngleDeg, swingEndAngleDeg);
            maxAng = Mathf.Max(swingStartAngleDeg, swingEndAngleDeg);
        }
        else
        {
            float a = -swingStartAngleDeg;
            float b = -swingEndAngleDeg;
            minAng = Mathf.Min(a, b);
            maxAng = Mathf.Max(a, b);
        }

        if (ang < minAng || ang > maxAng) return false;
        return true;
    }

    private Vector2 DirFromAngle(float angleDeg)
    {
        Vector2 forward = Vector2.right * FacingDirection;
        Quaternion rot = Quaternion.AngleAxis(angleDeg, Vector3.forward);
        Vector2 dir = rot * forward;
        return dir.normalized;
    }

    public void OnPerfectParry(Vector2 hitPoint)
    {
        if (state == State.Attack)
        {
            EnterState(State.Hit);
            StartAttackCooldown();
            attackResolved = true;
            attackPhase = 2;
            attackPhaseTimer = attackRecoverRuntime;
            Player.ClearParryCandidate(this);
            ClearSwingLine();
        }
    }

    public void OnImperfectParry(Vector2 hitPoint)
    {
        if (state == State.Attack)
        {
            attackResolved = true;
            StartAttackCooldown();
            Player.ClearParryCandidate(this);
            ClearSwingLine();
        }
    }

    public void OnCounterParry(Vector2 hitPoint)
    {
        Die();
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 originPos = attackOrigin.position;

        int steps = 24;
        float facing = Application.isPlaying ? FacingDirection : (transform.lossyScale.x >= 0f ? 1f : -1f);
        Vector2 forward = Vector2.right * facing;

        float minLocal = facing >= 0f
            ? Mathf.Min(swingStartAngleDeg, swingEndAngleDeg)
            : Mathf.Min(-swingStartAngleDeg, -swingEndAngleDeg);

        float maxLocal = facing >= 0f
            ? Mathf.Max(swingStartAngleDeg, swingEndAngleDeg)
            : Mathf.Max(-swingStartAngleDeg, -swingEndAngleDeg);

        Gizmos.color = Color.red;

        float step = (maxLocal - minLocal) / steps;
        Vector3 prev = originPos + (Vector3)((Quaternion.AngleAxis(minLocal, Vector3.forward) * (Vector3)forward).normalized * swingLength);

        for (int i = 1; i <= steps; i++)
        {
            float a = minLocal + step * i;
            Vector3 curr = originPos + (Vector3)((Quaternion.AngleAxis(a, Vector3.forward) * (Vector3)forward).normalized * swingLength);
            Gizmos.DrawLine(prev, curr);
            prev = curr;
        }

        Vector3 minDir = (Quaternion.AngleAxis(minLocal, Vector3.forward) * (Vector3)forward).normalized * swingLength;
        Vector3 maxDir = (Quaternion.AngleAxis(maxLocal, Vector3.forward) * (Vector3)forward).normalized * swingLength;

        Gizmos.DrawLine(originPos, originPos + minDir);
        Gizmos.DrawLine(originPos, originPos + maxDir);
    }

    private void UpdateSwingLine(Vector2 origin, Vector2 dir, float length)
    {
        swingLine.positionCount = 2;
        swingLine.SetPosition(0, origin);
        swingLine.SetPosition(1, origin + dir.normalized * length);
    }

    private void ClearSwingLine()
    {
        swingLine.positionCount = 0;
    }
}