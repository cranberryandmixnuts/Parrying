using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public sealed class SlasherEnemy : MonoBehaviour, IParryReactive
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

    [Header("Ranges")]
    [SerializeField] private Collider2D walkRange;
    [SerializeField] private Collider2D backOffRange;

    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float chaseSpeed = 4f;

    [Header("Attack Timing")]
    [SerializeField] private Vector2 attackCooldownRange = new(1.5f, 3f);
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackWindup = 0.25f;
    [SerializeField] private float swingDuration = 0.15f;
    [SerializeField] private float attackRecover = 0.3f;

    [Header("Attack Geometry")]
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private float swingStartAngleDeg = -60f;
    [SerializeField] private float swingEndAngleDeg = 30f;
    [SerializeField] private float swingLength = 2f;
    [SerializeField] private LayerMask playerHitMask;

    [Header("Reactions")]
    [SerializeField] private float hitStunDuration = 0.4f;
    [SerializeField] private float deathDespawnDelay = 1.0f;

    [Header("Animation Names")]
    [SerializeField] private string idleAnimName = "Idle";
    [SerializeField] private string walkAnimName = "Walk";
    [SerializeField] private string chaseAnimName = "Chase";
    [SerializeField] private string backWalkAnimName = "BackWalk";
    [SerializeField] private string attackAnimName = "Attack";
    [SerializeField] private string hitAnimName = "Hit";
    [SerializeField] private string deathAnimName = "Death";

    private Rigidbody2D rb;
    private Animator animator;
    private PlayerController player;

    private SlasherState currentState;

    private float stateTimer;
    private float attackCooldownTimer;

    private int facingDirection = 1;

    private int attackPhase;
    private float attackPhaseTimer;
    private bool attackResolved;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        currentState = SlasherState.Chase;

        stateTimer = 0f;
        attackCooldownTimer = 0f;

        attackPhase = 0;
        attackPhaseTimer = 0f;
        attackResolved = false;

        PlayAnimation(chaseAnimName);
    }

    private void Start()
    {
        player = PlayerController.Instance;
    }

    private void Update()
    {
        if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;

        if (currentState != SlasherState.Death) FacePlayer();

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

    private void FixedUpdate()
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
                attackPhaseTimer = swingDuration;
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
                attackPhaseTimer = attackRecover;
                StartAttackCooldown();
                player.ClearParryCandidate(this);
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

        player.GetDashDetectCircle(out Vector2 dashCenter, out float dashRadius);

        if (SegmentIntersectsCircle(originPos, segEnd, dashCenter, dashRadius))
            player.RegisterDashCandidate(segEnd);
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
        {
            PlayAnimation(idleAnimName);
        }
        else if (currentState == SlasherState.Walk)
        {
            PlayAnimation(walkAnimName);
        }
        else if (currentState == SlasherState.Chase)
        {
            PlayAnimation(chaseAnimName);
        }
        else if (currentState == SlasherState.BackWalk)
        {
            PlayAnimation(backWalkAnimName);
        }
        else if (currentState == SlasherState.Attack)
        {
            PlayAnimation(attackAnimName);
            attackPhase = 0;
            attackPhaseTimer = attackWindup;
            attackResolved = false;
        }
        else if (currentState == SlasherState.Hit)
        {
            PlayAnimation(hitAnimName);
            stateTimer = hitStunDuration;
        }
        else if (currentState == SlasherState.Death)
        {
            PlayAnimation(deathAnimName);
            stateTimer = deathDespawnDelay;
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

        float norm = 1f - (attackPhaseTimer / swingDuration);
        if (norm < 0f) norm = 0f;
        if (norm > 1f) norm = 1f;

        float angleDeg = Mathf.Lerp(swingStartAngleDeg, swingEndAngleDeg, norm);
        Vector2 dir = DirFromAngle(angleDeg);

        Vector2 segEnd = originPos + dir * swingLength;

        player.GetParryDetectCircle(out Vector2 parryCenter, out float parryRadius);

        bool parryZoneHit = SegmentIntersectsCircle(originPos, segEnd, parryCenter, parryRadius);

        if (parryZoneHit)
            player.RegisterParryCandidate(this, segEnd, attackDamage);

        player.GetDashDetectCircle(out Vector2 dashCenter, out float dashRadius);

        bool dashZoneHit = SegmentIntersectsCircle(originPos, segEnd, dashCenter, dashRadius);

        if (dashZoneHit)
            player.RegisterDashCandidate(segEnd);

        RaycastHit2D hit = Physics2D.Raycast(originPos, dir, swingLength, playerHitMask);

        if (hit.collider != null)
        {
            Vector2 hitPos = originPos + dir * hit.distance;
            player.Hit(attackDamage, hitPos);

            attackResolved = true;
            attackPhase = 2;
            attackPhaseTimer = attackRecover;
            StartAttackCooldown();
            player.ClearParryCandidate(this);
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
        attackCooldownTimer = Random.Range(attackCooldownRange.x, attackCooldownRange.y);
    }

    private void MoveTowardsPlayer(float speed)
    {
        Vector2 a = transform.position;
        Vector2 b = player.transform.position;

        float dx = b.x - a.x;
        float dirSign = 0f;
        if (dx > 0f) dirSign = 1f;
        else if (dx < 0f) dirSign = -1f;

        Vector2 v = rb.linearVelocity;
        v.x = dirSign * speed;
        rb.linearVelocity = new Vector2(v.x, rb.linearVelocity.y);
    }

    private void MoveAwayFromPlayer(float speed)
    {
        Vector2 a = transform.position;
        Vector2 b = player.transform.position;

        float dx = b.x - a.x;
        float dirSign = 0f;
        if (dx > 0f) dirSign = 1f;
        else if (dx < 0f) dirSign = -1f;

        Vector2 v = rb.linearVelocity;
        v.x = -dirSign * speed;
        rb.linearVelocity = new Vector2(v.x, rb.linearVelocity.y);
    }

    private void StopHorizontal()
    {
        Vector2 v = rb.linearVelocity;
        v.x = 0f;
        rb.linearVelocity = new Vector2(v.x, rb.linearVelocity.y);
    }

    private void FacePlayer()
    {
        float dx = player.transform.position.x - transform.position.x;
        if (dx > 0f) facingDirection = 1;
        else if (dx < 0f) facingDirection = -1;

        transform.rotation = Quaternion.Euler(0f, facingDirection == -1 ? 180f : 0f, 0f);
    }

    private void PlayAnimation(string clipName)
    {
        if (animator != null && !string.IsNullOrEmpty(clipName)) animator.Play(clipName);
    }

    private bool CheckRange(Collider2D col, Vector3 pos)
    {
        return col.OverlapPoint(pos);
    }

    private bool InWalkRange()
    {
        return CheckRange(walkRange, player.transform.position);
    }

    private bool InBackOffRange()
    {
        return CheckRange(backOffRange, player.transform.position);
    }

    private bool IsPlayerInSwingCone()
    {
        Vector2 originPos = attackOrigin != null ? (Vector2)attackOrigin.position : (Vector2)transform.position;
        Vector2 toPlayer = (Vector2)player.transform.position - originPos;

        float dist = toPlayer.magnitude;
        if (dist > swingLength) return false;

        Vector2 forward = Vector2.right * facingDirection;
        float ang = Vector2.SignedAngle(forward, toPlayer);

        float minAng = Mathf.Min(swingStartAngleDeg, swingEndAngleDeg);
        float maxAng = Mathf.Max(swingStartAngleDeg, swingEndAngleDeg);

        if (ang < minAng || ang > maxAng) return false;

        return true;
    }

    private Vector2 DirFromAngle(float angleDeg)
    {
        Vector2 forward = Vector2.right * facingDirection;
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
            attackPhaseTimer = attackRecover;
            player.ClearParryCandidate(this);
        }
    }

    public void OnImperfectParry(Vector2 hitPoint)
    {
        if (currentState == SlasherState.Attack)
        {
            attackResolved = true;
            attackPhase = 2;
            attackPhaseTimer = attackRecover;
            StartAttackCooldown();
            player.ClearParryCandidate(this);
        }
    }

    public void OnCounterParry(Vector2 hitPoint)
    {
        EnterState(SlasherState.Death);
        attackResolved = true;
        player.ClearParryCandidate(this);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackOrigin == null) return;

        Vector3 originPos = attackOrigin.position;

        Gizmos.color = Color.red;

        int steps = 16;
        float minA = swingStartAngleDeg;
        float maxA = swingEndAngleDeg;

        Vector3 prevPoint = originPos;
        bool prevSet = false;

        for (int i = 0; i <= steps; i++)
        {
            float t = (steps == 0) ? 0f : (float)i / steps;
            float a = Mathf.Lerp(minA, maxA, t);
            Vector2 dir = DirFromAngle(a);
            Vector3 point = originPos + (Vector3)(dir * swingLength);

            if (prevSet) Gizmos.DrawLine(prevPoint, point);

            prevPoint = point;
            prevSet = true;

            if (i == 0 || i == steps) Gizmos.DrawLine(originPos, point);
        }
    }
}