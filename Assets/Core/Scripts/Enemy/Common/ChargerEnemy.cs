using UnityEngine;
using Sirenix.OdinInspector;

public sealed class ChargerEnemy : EnemyBase, IParryReactive
{
    private enum State
    {
        Walk,
        BackWalk,
        Charge,
        Attack,
        Stop
    }

    private const string AnimWalk = "Walk";
    private const string AnimBackWalk = "BackWalk";
    private const string AnimCharge = "Charge";
    private const string AnimAttack = "Attack";
    private const string AnimStop = "Stop";
    private const string AnimDeath = "Death";

    [TabGroup("Charger Enemy", "Setup"), BoxGroup("Charger Enemy/Setup/Ranges"), SerializeField, Required]
    private Collider2D attackCollider;

    [TabGroup("Charger Enemy", "Setup"), BoxGroup("Charger Enemy/Setup/Ranges"), SerializeField, Required]
    private Collider2D backOffRange;

    [TabGroup("Charger Enemy", "Setup"), BoxGroup("Charger Enemy/Setup/Attack"), SerializeField]
    private LayerMask playerHitMask;

    [TabGroup("Charger Enemy", "Tuning"), BoxGroup("Charger Enemy/Tuning/Movement"), SerializeField, MinValue(0f), SuffixLabel("u/s", true)]
    private float walkSpeed = 1f;

    [TabGroup("Charger Enemy", "Tuning"), BoxGroup("Charger Enemy/Tuning/Movement"), SerializeField, MinValue(0f), SuffixLabel("u/s", true)]
    private float attackSpeed = 7f;

    [TabGroup("Charger Enemy", "Tuning"), BoxGroup("Charger Enemy/Tuning/Movement"), SerializeField, MinValue(0f), SuffixLabel("u/s^2", true)]
    private float stopFriction = 20f;

    [TabGroup("Charger Enemy", "Tuning"), BoxGroup("Charger Enemy/Tuning/Timing"), SerializeField, MinMaxSlider(0f, 10f, true)]
    private Vector2 attackCooldownRange = new(1.5f, 3f);

    [TabGroup("Charger Enemy", "Tuning"), BoxGroup("Charger Enemy/Tuning/Timing"), SerializeField, MinValue(0f), SuffixLabel("s", true)]
    private float missBehindDuration = 1f;

    [TabGroup("Charger Enemy", "Tuning"), BoxGroup("Charger Enemy/Tuning/Timing"), SerializeField, MinValue(0f), SuffixLabel("s", true)]
    private float overshootAfterParryDuration = 0.5f;

    [TabGroup("Charger Enemy", "Tuning"), BoxGroup("Charger Enemy/Tuning/Timing"), SerializeField, MinValue(0f), SuffixLabel("s", true)]
    private float backWalkDurationMin = 1f;

    [TabGroup("Charger Enemy", "Tuning"), BoxGroup("Charger Enemy/Tuning/Timing"), SerializeField, MinValue(0f), SuffixLabel("s", true)]
    private float backWalkDurationMax = 3f;

    [TabGroup("Charger Enemy", "Tuning"), BoxGroup("Charger Enemy/Tuning/Attack"), SerializeField, MinValue(0), SuffixLabel("HP", true)]
    private int contactDamage = 100;

    private State state;
    private float cooldownTimer;
    private float chargeTimer;
    private float attackDir;
    private float behindTimer;
    private float overshootTimer;
    private float backWalkTimer;
    private float stopTimer;
    private bool lethalActive;

    protected override string DeathAnimName => AnimDeath;

    private readonly Collider2D[] overlapResults = new Collider2D[8];

    protected override void Start()
    {
        base.Start();

        DeathDespawnDelay = -1f;
        ResetAttackCooldown();
        EnterWalk();
    }

    protected override void OnUpdate()
    {
        if (state == State.Walk || state == State.BackWalk) FacePlayer();

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

    protected override void OnFixedUpdate()
    {
        switch (state)
        {
            case State.Walk:
                Body.linearVelocity = new Vector2(FacingDirection * walkSpeed, Body.linearVelocity.y);
                break;
            case State.BackWalk:
                Body.linearVelocity = new Vector2(-FacingDirection * walkSpeed, Body.linearVelocity.y);
                break;
            case State.Charge:
                Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
                break;
            case State.Attack:
                Body.linearVelocity = new Vector2(attackDir * attackSpeed, Body.linearVelocity.y);
                break;
            case State.Stop:
                Body.linearVelocity = new Vector2(Mathf.MoveTowards(Body.linearVelocity.x, 0f, stopFriction * Time.fixedDeltaTime), Body.linearVelocity.y);
                break;
        }

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

        bool playerIsBehind =
            (attackDir > 0f && Player.transform.position.x < transform.position.x) ||
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
            if (!Player.TryHit(contactDamage, hitPoint)) return;
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
            if (pc == Player) return true;
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
        if (IsDead()) return;

        lethalActive = false;
        Die();
    }
}