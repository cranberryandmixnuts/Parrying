using UnityEngine;

public sealed class ChargerEnemy : MonoBehaviour, IDamageable, IParryReactive, IParryStack
{
    private enum State
    {
        Walk,
        BackWalk,
        Charge,
        Attack,
        Stop,
        Death
    }

    [Header("Ranges")]
    [SerializeField] private Collider2D attackCollider;
    [SerializeField] private Collider2D tooCloseRangeCollider;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 1f;
    [SerializeField] private float attackSpeed = 7f;
    [SerializeField] private float stopFriction = 20f;

    [Header("Timing")]
    [SerializeField] private Vector2 attackCooldownRange = new(3f, 5f);
    [SerializeField] private float chargeWindupDuration = 1f;
    [SerializeField] private float missBehindDuration = 2f;
    [SerializeField] private float overshootAfterParryDuration = 0.5f;
    [SerializeField] private float backWalkDurationMin = 1f;
    [SerializeField] private float backWalkDurationMax = 3f;

    [Header("Attack")]
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private LayerMask playerHitMask;

    private PlayerController player;
    private Rigidbody2D rb;
    private Animator animator;

    private State state;
    private int facingDirection = 1;
    private float cooldownTimer;
    private float chargeTimer;
    private float attackDir;
    private float behindTimer;
    private float overshootTimer;
    private float backWalkTimer;
    private float stopTimer;
    private bool lethalActive;

    private readonly Collider2D[] overlapResults = new Collider2D[8];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        player = PlayerController.Instance;
        ResetAttackCooldown();
        EnterWalk();
    }

    private void Update()
    {
        if (state == State.Death) return;

        cooldownTimer -= Time.deltaTime;

        switch (state)
        {
            case State.Walk:
                UpdateWalk();
                break;
            case State.BackWalk:
                UpdateBackWalk();
                break;
            case State.Charge:
                UpdateCharge();
                break;
            case State.Attack:
                UpdateAttack();
                break;
            case State.Stop:
                UpdateStop();
                break;
        }
    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case State.Walk:
                rb.linearVelocity = new Vector2(facingDirection * walkSpeed, rb.linearVelocity.y);
                break;
            case State.BackWalk:
                rb.linearVelocity = new Vector2(-facingDirection * walkSpeed, rb.linearVelocity.y);
                break;
            case State.Charge:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;
            case State.Attack:
                rb.linearVelocity = new Vector2(attackDir * attackSpeed, rb.linearVelocity.y);
                break;
            case State.Stop:
                rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, 0f, stopFriction * Time.fixedDeltaTime), rb.linearVelocity.y);
                break;
            case State.Death:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;
        }

        if (state == State.Attack)
            HandleAttackHitbox();
    }

    private void EnterWalk()
    {
        state = State.Walk;
        lethalActive = false;
        overshootTimer = 0f;
        behindTimer = 0f;
        stopTimer = 0f;
        backWalkTimer = 0f;
        FacePlayer();
        animator.Play("Walk");
    }

    private void UpdateWalk()
    {
        FacePlayer();

        if (cooldownTimer <= 0f)
        {
            EnterCharge();
            return;
        }

        if (IsPlayerTooClose())
        {
            EnterBackWalk();
            return;
        }
    }

    private void EnterBackWalk()
    {
        state = State.BackWalk;
        lethalActive = false;
        overshootTimer = 0f;
        behindTimer = 0f;
        stopTimer = 0f;
        backWalkTimer = Random.Range(backWalkDurationMin, backWalkDurationMax);
        FacePlayer();
        animator.Play("BackWalk");
    }

    private void UpdateBackWalk()
    {
        FacePlayer();

        if (cooldownTimer <= 0f)
        {
            EnterCharge();
            return;
        }

        backWalkTimer -= Time.deltaTime;
        if (backWalkTimer <= 0f)
        {
            EnterWalk();
            return;
        }
    }

    private void EnterCharge()
    {
        state = State.Charge;
        FacePlayer();
        attackDir = facingDirection;
        lethalActive = false;
        overshootTimer = 0f;
        behindTimer = 0f;
        stopTimer = 0f;
        backWalkTimer = 0f;
        chargeTimer = chargeWindupDuration;
        animator.Play("Charge");
    }

    private void UpdateCharge()
    {
        ApplyFacing((int)attackDir);

        chargeTimer -= Time.deltaTime;
        if (chargeTimer <= 0f)
        {
            EnterAttack();
            return;
        }
    }

    private void EnterAttack()
    {
        state = State.Attack;
        lethalActive = true;
        overshootTimer = -1f;
        behindTimer = 0f;
        stopTimer = 0f;
        backWalkTimer = 0f;
        animator.Play("Attack");
    }

    private void UpdateAttack()
    {
        ApplyFacing((int)attackDir);

        if (overshootTimer > 0f)
        {
            overshootTimer -= Time.deltaTime;
            if (overshootTimer <= 0f)
            {
                EnterStop();
                return;
            }
        }

        bool playerIsBehind = (attackDir > 0f && player.transform.position.x < transform.position.x) ||
                              (attackDir < 0f && player.transform.position.x > transform.position.x);

        if (playerIsBehind)
        {
            behindTimer += Time.deltaTime;
            if (behindTimer >= missBehindDuration)
            {
                EnterStop();
                return;
            }
        }
        else
        {
            behindTimer = 0f;
        }
    }

    private void EnterStop()
    {
        state = State.Stop;
        lethalActive = false;
        overshootTimer = 0f;
        behindTimer = 0f;
        backWalkTimer = 0f;
        stopTimer = 0.2f;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        player.ClearParryCandidate(this);
        ResetAttackCooldown();
        animator.Play("Stop");
    }

    private void UpdateStop()
    {
        stopTimer -= Time.deltaTime;
        if (stopTimer <= 0f)
        {
            EnterWalk();
            return;
        }
    }

    private void EnterDeath()
    {
        state = State.Death;
        lethalActive = false;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        player.ClearParryCandidate(this);
        animator.Play("Death");
    }

    private void FacePlayer()
    {
        float dx = player.transform.position.x - transform.position.x;
        if (dx > 0f) facingDirection = 1;
        else if (dx < 0f) facingDirection = -1;

        transform.rotation = Quaternion.Euler(0f, facingDirection == -1 ? 180f : 0f, 0f);
    }

    private void ApplyFacing(int dir)
    {
        if (dir > 0) facingDirection = 1;
        else if (dir < 0) facingDirection = -1;

        transform.rotation = Quaternion.Euler(0f, facingDirection == -1 ? 180f : 0f, 0f);
    }

    private bool IsPlayerTooClose()
    {
        return tooCloseRangeCollider.OverlapPoint(player.transform.position);
    }

    private void HandleAttackHitbox()
    {
        Vector2 hitPoint = attackCollider.bounds.center;

        if (lethalActive)
        {
            player.GetParryDetectCircle(out Vector2 parryCenter, out float parryRadius);

            if (IsColliderWithinCircle(attackCollider, parryCenter, parryRadius))
                player.RegisterParryCandidate(this, hitPoint);

            player.GetDashDetectCircle(out Vector2 dashCenter, out float dashRadius);

            if (IsColliderWithinCircle(attackCollider, dashCenter, dashRadius))
                player.RegisterDashCandidate(hitPoint);

            if (OverlapsPlayerBody())
            {
                player.Hit(contactDamage, hitPoint);
                lethalActive = false;
                overshootTimer = overshootAfterParryDuration;
                player.ClearParryCandidate(this);
            }
        }
    }

    private bool IsColliderWithinCircle(Collider2D col, Vector2 center, float radius)
    {
        Vector2 closest = col.ClosestPoint(center);
        float dx = closest.x - center.x;
        float dy = closest.y - center.y;
        float distSq = dx * dx + dy * dy;
        float r2 = radius * radius;
        return distSq <= r2;
    }

    private bool OverlapsPlayerBody()
    {
        ContactFilter2D f = new();
        f.SetLayerMask(playerHitMask);
        f.useTriggers = true;

        int count = attackCollider.Overlap(f, overlapResults);
        for (int i = 0; i < count; i++)
        {
            if (overlapResults[i] == null) continue;
            PlayerController pc = overlapResults[i].GetComponentInParent<PlayerController>();
            if (pc == player)
                return true;
        }

        return false;
    }

    private void ResetAttackCooldown()
    {
        cooldownTimer = Random.Range(attackCooldownRange.x, attackCooldownRange.y);
    }

    public void OnPerfectParry(Vector2 hitPoint)
    {
        if (state != State.Attack) return;
        if (!lethalActive) return;

        lethalActive = false;
        overshootTimer = overshootAfterParryDuration;
        player.ClearParryCandidate(this);
    }

    public void OnImperfectParry(Vector2 hitPoint)
    {
        OnPerfectParry(hitPoint);
    }

    public void OnCounterParry(Vector2 hitPoint)
    {
        if (state == State.Death) return;
        EnterDeath();
    }

    public void Hit(int damage, Vector2 attackPos)
    {
        if (state == State.Death) return;
        EnterDeath();
    }

    public void AddOrRemove(int delta)
    {
        if (delta < 0) EnterDeath();
    }
}