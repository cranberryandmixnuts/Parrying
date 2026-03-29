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

    [TabGroup("Slasher Enemy", "Tuning"), BoxGroup("Slasher Enemy/Tuning/Attack Timing"), SerializeField, MinMaxSlider(0f, 10f, true)]
    private Vector2 attackIntervalRange = new(1f, 3f);

    [TabGroup("Slasher Enemy", "Tuning"), BoxGroup("Slasher Enemy/Tuning/Attack Timing"), SerializeField, PropertyRange(0f, 1f), SuffixLabel("%", true), MaxValue("@attackEndPercent - 0.1f")]
    private float attackPrepPercent = 0.2f;

    [TabGroup("Slasher Enemy", "Tuning"), BoxGroup("Slasher Enemy/Tuning/Attack Timing"), SerializeField, PropertyRange(0f, 1f), SuffixLabel("%", true)]
    private float attackEndPercent = 0.9f;

    [TabGroup("Slasher Enemy", "Tuning"), BoxGroup("Slasher Enemy/Tuning/Damage"), SerializeField, MinValue(0), SuffixLabel("HP", true)]
    private int attackDamage = 100;

    private State state;
    private float stateTimer;
    private float attackCooldownTimer;
    private int attackPhase;
    private float attackPhaseTimer;
    private bool attackResolved;
    private bool SlashSoundPlayed;

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
    }

    public override void Die()
    {
        if (IsDead()) return;

        AudioManager.Instance.StopSFX(gameObject, "Move");
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

        SyncMoveSFX();
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
            if (SlashSoundPlayed) SlashSoundPlayed = false;
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
            if (!SlashSoundPlayed)
            {
                AudioManager.Instance.PlayOneShotSFX("적 슬래시", gameObject);
                SlashSoundPlayed = true;
            }

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
                AudioManager.Instance.StopSFX(gameObject, "Move");
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
                AudioManager.Instance.StopSFX(gameObject, "Move");

                float clipLen = GetAnimLength(AnimAttack);

                attackWindupRuntime = clipLen * attackPrepPercent;
                swingDurationRuntime = clipLen * (attackEndPercent - attackPrepPercent);
                attackRecoverRuntime = clipLen - (attackWindupRuntime + swingDurationRuntime);
                if (attackRecoverRuntime < 0.01f) attackRecoverRuntime = 0.01f;

                attackPhase = 0;
                attackPhaseTimer = attackWindupRuntime;
                attackResolved = false;
                break;

            case State.Hit:
                Anim.Play(AnimHit);
                AudioManager.Instance.StopSFX(gameObject, "Move");
                stateTimer = GetAnimLength(AnimHit);
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
                attackResolved = true;
                StartAttackCooldown();
                Player.ClearParryCandidate(this);
            }
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

    private void StartAttackCooldown() => attackCooldownTimer = Random.Range(attackIntervalRange.x, attackIntervalRange.y);

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

    private void StopHorizontal() => Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);

    private void SyncMoveSFX()
    {
        float moveAbsX = Mathf.Abs(Body.linearVelocity.x);

        if (state == State.Walk || state == State.BackWalk)
        {
            if (moveAbsX <= 0.01f)
                AudioManager.Instance.StopSFX(gameObject, "Move");
            else
                AudioManager.Instance.PlayLoopSFX("적 걷기", gameObject, "Move");

            return;
        }

        if (state == State.Chase)
        {
            if (moveAbsX <= 0.01f)
                AudioManager.Instance.StopSFX(gameObject, "Move");
            else
                AudioManager.Instance.PlayLoopSFX("적 무겁게 달려옴", gameObject, "Move");

            return;
        }

        AudioManager.Instance.StopSFX(gameObject, "Move");
    }

    private bool InWalkRange() => walkRange.OverlapPoint(Player.transform.position);

    private bool InBackOffRange() => backOffRange.OverlapPoint(Player.transform.position);

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
        }
    }

    public void OnImperfectParry(Vector2 hitPoint)
    {
        if (state == State.Attack)
        {
            attackResolved = true;
            StartAttackCooldown();
            Player.ClearParryCandidate(this);
        }
    }

    public void OnCounterParry(Vector2 hitPoint) => Die();
}