using UnityEngine;

public sealed class ChargerEnemy : EnemyBase, IDamageable, IParryReactive, IParryStack
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

    private const string AnimWalk = "Walk";
    private const string AnimBackWalk = "BackWalk";
    private const string AnimCharge = "Charge";
    private const string AnimAttack = "Attack";
    private const string AnimStop = "Stop";
    private const string AnimDeath = "Death";

    [Header("Ranges")]
    [SerializeField] private Collider2D attackCollider;
    [SerializeField] private Collider2D backOffRange;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 1f;
    [SerializeField] private float attackSpeed = 7f;
    [SerializeField] private float stopFriction = 20f;

    [Header("Timing")]
    [SerializeField] private Vector2 attackCooldownRange = new(1.5f, 3f);
    [SerializeField] private float missBehindDuration = 1f;
    [SerializeField] private float overshootAfterParryDuration = 0.5f;
    [SerializeField] private float backWalkDurationMin = 1f;
    [SerializeField] private float backWalkDurationMax = 3f;

    [Header("Attack")]
    [SerializeField] private int contactDamage = 10;
    [SerializeField] private LayerMask playerHitMask;

    private State state;
    private float cooldownTimer;
    private float chargeTimer;
    private float attackDir;
    private float behindTimer;
    private float overshootTimer;
    private float backWalkTimer;
    private float stopTimer;
    private float deathTimer;
    private bool lethalActive;

    private readonly Collider2D[] overlapResults = new Collider2D[8];

    protected override void Start()
    {
        base.Start();
        ResetAttackCooldown();
        EnterWalk();
    }

    protected override void OnUpdate()
    {
        if (state == State.Walk || state == State.BackWalk)
            FacePlayer();

        if (state == State.Walk)
            UpdateWalk();
        else if (state == State.BackWalk)
            UpdateBackWalk();
        else if (state == State.Charge)
            UpdateCharge();
        else if (state == State.Attack)
            UpdateAttack();
        else if (state == State.Stop)
            UpdateStop();
        else if (state == State.Death)
            UpdateDeath();
    }

    protected override void OnFixedUpdate()
    {
        if (state == State.Walk)
            Body.linearVelocity = new Vector2(FacingDirection * walkSpeed, Body.linearVelocity.y);
        else if (state == State.BackWalk)
            Body.linearVelocity = new Vector2(-FacingDirection * walkSpeed, Body.linearVelocity.y);
        else if (state == State.Charge)
            Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
        else if (state == State.Attack)
            Body.linearVelocity = new Vector2(attackDir * attackSpeed, Body.linearVelocity.y);
        else if (state == State.Stop)
            Body.linearVelocity = new Vector2(Mathf.MoveTowards(Body.linearVelocity.x, 0f, stopFriction * Time.fixedDeltaTime), Body.linearVelocity.y);
        else if (state == State.Death)
            Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);

        if (state == State.Attack) HandleAttackHitbox();
    }

    private void EnterWalk()
    {
        state = State.Walk;
        lethalActive = false;
        overshootTimer = 0f;
        behindTimer = 0f;
        stopTimer = 0f;
        backWalkTimer = 0f;
        Anim.Play(AnimWalk);
    }

    private void UpdateWalk()
    {
        cooldownTimer -= Time.deltaTime;

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
        Anim.Play(AnimBackWalk);
    }

    private void UpdateBackWalk()
    {
        cooldownTimer -= Time.deltaTime;

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
        attackDir = FacingDirection;
        lethalActive = false;
        overshootTimer = 0f;
        behindTimer = 0f;
        stopTimer = 0f;
        backWalkTimer = 0f;
        Anim.Play(AnimCharge);
        chargeTimer = GetAnimLength(AnimCharge);
    }

    private void UpdateCharge()
    {
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
        Anim.Play(AnimAttack);
    }

    private void UpdateAttack()
    {
        if (overshootTimer > 0f)
        {
            overshootTimer -= Time.deltaTime;
            if (overshootTimer <= 0f)
            {
                EnterStop();
                return;
            }
        }

        bool playerIsBehind = (attackDir > 0f && Player.transform.position.x < transform.position.x) ||
                              (attackDir < 0f && Player.transform.position.x > transform.position.x);

        if (playerIsBehind)
        {
            behindTimer += Time.deltaTime;
            if (behindTimer >= missBehindDuration)
            {
                EnterStop();
                return;
            }
        }
        else behindTimer = 0f;
    }

    private void EnterStop()
    {
        state = State.Stop;
        lethalActive = false;
        overshootTimer = 0f;
        behindTimer = 0f;
        backWalkTimer = 0f;
        Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
        Player.ClearParryCandidate(this);
        ResetAttackCooldown();
        Anim.Play(AnimStop);
        stopTimer = GetAnimLength(AnimStop);
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
        Body.linearVelocity = Vector2.zero;
        Body.simulated = false;
        Player.ClearParryCandidate(this);
        Anim.Play(AnimDeath);
        deathTimer = GetAnimLength(AnimDeath);
    }

    private void UpdateDeath()
    {
        deathTimer -= Time.deltaTime;
        if (deathTimer <= 0f) Destroy(gameObject);
    }

    private bool IsPlayerTooClose()
    {
        return backOffRange.OverlapPoint(Player.transform.position);
    }

    private void HandleAttackHitbox()
    {
        if (!lethalActive) return;

        Vector2 hitPoint = attackCollider.bounds.center;

        Player.GetParryDetectCircle(out Vector2 parryCenter, out float parryRadius);
        if (IsColliderWithinCircle(attackCollider, parryCenter, parryRadius))
            Player.RegisterParryCandidate(this, hitPoint, contactDamage);

        Player.GetDashDetectCircle(out Vector2 dashCenter, out float dashRadius);
        if (IsColliderWithinCircle(attackCollider, dashCenter, dashRadius))
            Player.RegisterDashCandidate(hitPoint);

        if (OverlapsPlayerBody())
        {
            Player.Hit(contactDamage, hitPoint);
            lethalActive = false;
            overshootTimer = overshootAfterParryDuration;
            Player.ClearParryCandidate(this);
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
            if (pc == Player)
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
        Player.ClearParryCandidate(this);
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