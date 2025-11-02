using UnityEngine;

public sealed class SlasherEnemy : EnemyBase, IParryReactive
{
    private enum SlasherState
    {
        Idle,
        Walk,
        Chase,
        BackWalk,
        Attack,
        Hit,
        Death
    }

    private const string AnimIdle = "Idle";
    private const string AnimWalk = "Walk";
    private const string AnimChase = "Chase";
    private const string AnimBackWalk = "BackWalk";
    private const string AnimAttack = "Attack";
    private const string AnimHit = "Hit";
    private const string AnimDeath = "Death";

    [Header("Ranges")]
    [SerializeField] private Collider2D walkRange;
    [SerializeField] private Collider2D backOffRange;

    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float chaseSpeed = 4f;

    [Header("Attack Geometry")]
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private float swingStartAngleDeg = -60f;
    [SerializeField] private float swingEndAngleDeg = 30f;
    [SerializeField] private float swingLength = 2f;
    [SerializeField] private LayerMask playerHitMask;

    [Header("Attack Timing (Percent)")]
    [SerializeField, Range(0f, 1f)] private float attackPrepPercent = 0.2f;
    [SerializeField, Range(0f, 1f)] private float attackEndPercent = 0.9f;

    [Header("Attack Damage")]
    [SerializeField] private int attackDamage = 10;

    private SlasherState currentState;
    private float stateTimer;
    private float attackCooldownTimer;
    private int attackPhase;
    private float attackPhaseTimer;
    private bool attackResolved;

    private float attackWindupRuntime;
    private float swingDurationRuntime;
    private float attackRecoverRuntime;

    protected override string DeathAnimName
    {
        get { return AnimDeath; }
    }

    protected override void Awake()
    {
        base.Awake();
        currentState = SlasherState.Chase;
        stateTimer = 0f;
        attackCooldownTimer = 0f;
        attackPhase = 0;
        attackPhaseTimer = 0f;
        attackResolved = false;
        PlayAnim(AnimChase);
    }

    protected override void OnUpdate()
    {
        if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;

        if (currentState != SlasherState.Attack &&
            currentState != SlasherState.Hit &&
            currentState != SlasherState.Death)
            FacePlayer();

        if (currentState == SlasherState.Idle)
            UpdateIdle();
        else if (currentState == SlasherState.Walk)
            UpdateWalk();
        else if (currentState == SlasherState.Chase)
            UpdateChase();
        else if (currentState == SlasherState.BackWalk)
            UpdateBackWalk();
        else if (currentState == SlasherState.Attack)
            UpdateAttack();
        else if (currentState == SlasherState.Hit)
            UpdateHit();
        else if (currentState == SlasherState.Death)
            UpdateDeath();
    }

    protected override void OnFixedUpdate()
    {
        if (currentState == SlasherState.Walk)
            MoveTowardsPlayer(walkSpeed);
        else if (currentState == SlasherState.Chase)
            MoveTowardsPlayer(chaseSpeed);
        else if (currentState == SlasherState.BackWalk)
            MoveAwayFromPlayer(walkSpeed);
        else
            StopHorizontal();
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
            EnterState(SlasherState.Attack);
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
            }
            return;
        }

        if (attackPhase == 2)
        {
            attackPhaseTimer -= Time.deltaTime;
            if (attackPhaseTimer <= 0f) ChooseMovementState();
        }
    }

    private void RegisterDashAssistRay()
    {
        Vector2 originPos = attackOrigin != null ? (Vector2)attackOrigin.position : (Vector2)transform.position;
        float angleDeg = swingStartAngleDeg;
        Vector2 dir = DirFromAngle(angleDeg);
        Vector2 segEnd = originPos + dir * swingLength;

        Player.GetDashDetectCircle(out Vector2 dashCenter, out float dashRadius);

        if (SegmentIntersectsCircle(originPos, segEnd, dashCenter, dashRadius))
            Player.RegisterDashCandidate(segEnd);
    }

    private void UpdateHit()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f) ChooseMovementState();
    }

    private void UpdateDeath()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f) Destroy(gameObject);
    }

    private void EnterState(SlasherState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        if (currentState == SlasherState.Idle)
            PlayAnim(AnimIdle);
        else if (currentState == SlasherState.Walk)
            PlayAnim(AnimWalk);
        else if (currentState == SlasherState.Chase)
            PlayAnim(AnimChase);
        else if (currentState == SlasherState.BackWalk)
            PlayAnim(AnimBackWalk);
        else if (currentState == SlasherState.Attack)
        {
            PlayAnim(AnimAttack);
            float clipLen = GetAnimLength(AnimAttack);
            if (clipLen <= 0f) clipLen = 1f;

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
        }
        else if (currentState == SlasherState.Hit)
        {
            PlayAnim(AnimHit);
            stateTimer = GetAnimLength(AnimHit);
        }
        else if (currentState == SlasherState.Death)
        {
            PlayAnim(AnimDeath);
            stateTimer = GetAnimLength(AnimDeath);
        }
    }

    private void ChooseMovementState()
    {
        if (currentState == SlasherState.Death) return;

        bool inCone = IsPlayerInSwingCone();
        bool inWalk = InWalkRange();
        bool inBack = InBackOffRange();

        SlasherState nextState;

        if (inCone)
        {
            if (attackCooldownTimer <= 0f)
            {
                nextState = SlasherState.Attack;
            }
            else
            {
                if (inBack)
                    nextState = SlasherState.BackWalk;
                else
                    nextState = SlasherState.Idle;
            }
        }
        else
        {
            if (inWalk)
                nextState = SlasherState.Walk;
            else
                nextState = SlasherState.Chase;
        }

        EnterState(nextState);
    }

    private void PerformSwingStep()
    {
        if (attackResolved) return;

        Vector2 originPos = attackOrigin != null ? (Vector2)attackOrigin.position : (Vector2)transform.position;

        float norm = 1f - (attackPhaseTimer / swingDurationRuntime);
        if (norm < 0f) norm = 0f;
        if (norm > 1f) norm = 1f;

        float angleDeg = Mathf.Lerp(swingStartAngleDeg, swingEndAngleDeg, norm);
        Vector2 dir = DirFromAngle(angleDeg);

        Vector2 segEnd = originPos + dir * swingLength;

        Player.GetParryDetectCircle(out Vector2 parryCenter, out float parryRadius);

        bool parryZoneHit = SegmentIntersectsCircle(originPos, segEnd, parryCenter, parryRadius);

        if (parryZoneHit)
            Player.RegisterParryCandidate(this, segEnd, attackDamage);

        Player.GetDashDetectCircle(out Vector2 dashCenter, out float dashRadius);

        bool dashZoneHit = SegmentIntersectsCircle(originPos, segEnd, dashCenter, dashRadius);

        if (dashZoneHit)
            Player.RegisterDashCandidate(segEnd);

        RaycastHit2D hit = Physics2D.Raycast(originPos, dir, swingLength, playerHitMask);

        if (hit.collider != null)
        {
            Vector2 hitPos = originPos + dir * hit.distance;
            Player.Hit(attackDamage, hitPos);

            attackResolved = true;
            attackPhase = 2;
            attackPhaseTimer = attackRecoverRuntime;
            StartAttackCooldown();
            Player.ClearParryCandidate(this);
        }
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

        Vector2 v = Body.linearVelocity;
        v.x = dirSign * speed;
        Body.linearVelocity = new Vector2(v.x, Body.linearVelocity.y);
    }

    private void MoveAwayFromPlayer(float speed)
    {
        Vector2 a = transform.position;
        Vector2 b = Player.transform.position;

        float dx = b.x - a.x;
        float dirSign = 0f;
        if (dx > 0f) dirSign = 1f;
        else if (dx < 0f) dirSign = -1f;

        Vector2 v = Body.linearVelocity;
        v.x = -dirSign * speed;
        Body.linearVelocity = new Vector2(v.x, Body.linearVelocity.y);
    }

    private void StopHorizontal()
    {
        Vector2 v = Body.linearVelocity;
        v.x = 0f;
        Body.linearVelocity = new Vector2(v.x, Body.linearVelocity.y);
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

        float minAng = Mathf.Min(swingStartAngleDeg, swingEndAngleDeg);
        float maxAng = Mathf.Max(swingStartAngleDeg, swingEndAngleDeg);

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
        if (currentState == SlasherState.Attack)
        {
            EnterState(SlasherState.Hit);
            StartAttackCooldown();
            attackResolved = true;
            attackPhase = 2;
            attackPhaseTimer = attackRecoverRuntime;
            Player.ClearParryCandidate(this);
        }
    }

    public void OnImperfectParry(Vector2 hitPoint)
    {
        if (currentState == SlasherState.Attack)
        {
            attackResolved = true;
            attackPhase = 2;
            attackPhaseTimer = attackRecoverRuntime;
            StartAttackCooldown();
            Player.ClearParryCandidate(this);
        }
    }

    public void OnCounterParry(Vector2 hitPoint)
    {
        EnterState(SlasherState.Death);
        attackResolved = true;
        Player.ClearParryCandidate(this);
    }
}