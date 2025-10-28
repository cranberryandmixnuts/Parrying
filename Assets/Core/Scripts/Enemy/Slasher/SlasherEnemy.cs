using UnityEngine;

public sealed class SlasherEnemy : MonoBehaviour
{
    private enum SlasherState
    {
        Idle,
        Walk,
        Chase,
        Attack,
        Hit,
        Death
    }

    [Header("Distances")]
    [SerializeField] private float detectDistance = 8f;
    [SerializeField] private float chaseDistance = 5f;
    [SerializeField] private float attackRange = 1.5f;

    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float chaseSpeed = 4f;

    [Header("Attack")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackWindup = 0.25f;
    [SerializeField] private float attackRecover = 0.3f;
    [SerializeField] private Transform attackOrigin;

    [Header("Reactions")]
    [SerializeField] private float hitStunDuration = 0.4f;
    [SerializeField] private float deathDespawnDelay = 1.0f;

    [Header("Animation Names")]
    [SerializeField] private string idleAnimName = "Idle";
    [SerializeField] private string walkAnimName = "Walk";
    [SerializeField] private string chaseAnimName = "Chase";
    [SerializeField] private string attackAnimName = "Attack";
    [SerializeField] private string hitAnimName = "Hit";
    [SerializeField] private string deathAnimName = "Death";

    private Rigidbody2D rb;
    private Animator animator;
    private PlayerController player;

    private SlasherState currentState;
    private bool playerDetected;
    private float stateTimer;
    private bool attackDidStrike;
    private int facingDirection = 1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        currentState = SlasherState.Idle;
        playerDetected = false;
        stateTimer = 0f;
        attackDidStrike = false;

        PlayAnimation(idleAnimName);
    }

    private void Start()
    {
        player = PlayerController.Instance;
    }

    private void Update()
    {
        if (currentState != SlasherState.Death)
            FacePlayer();

        switch (currentState)
        {
            case SlasherState.Idle:
                UpdateIdle();
                break;
            case SlasherState.Walk:
                UpdateWalk();
                break;
            case SlasherState.Chase:
                UpdateChase();
                break;
            case SlasherState.Attack:
                UpdateAttack();
                break;
            case SlasherState.Hit:
                UpdateHit();
                break;
            case SlasherState.Death:
                UpdateDeath();
                break;
        }
    }

    private void FixedUpdate()
    {
        switch (currentState)
        {
            case SlasherState.Walk:
                MoveTowardsPlayer(walkSpeed);
                break;
            case SlasherState.Chase:
                MoveTowardsPlayer(chaseSpeed);
                break;
            default:
                StopHorizontal();
                break;
        }
    }

    private void UpdateIdle()
    {
        float dist = DistanceToPlayer();
        if (dist <= detectDistance)
        {
            playerDetected = true;
            ChooseMovementState();
        }
    }

    private void UpdateWalk()
    {
        float dist = DistanceToPlayer();

        if (dist <= attackRange)
        {
            EnterState(SlasherState.Attack);
            return;
        }

        if (dist > chaseDistance)
        {
            EnterState(SlasherState.Chase);
            return;
        }
    }

    private void UpdateChase()
    {
        float dist = DistanceToPlayer();

        if (dist <= attackRange)
        {
            EnterState(SlasherState.Attack);
            return;
        }

        if (dist <= chaseDistance)
        {
            EnterState(SlasherState.Walk);
            return;
        }
    }

    private void UpdateAttack()
    {
        stateTimer -= Time.deltaTime;

        if (!attackDidStrike)
        {
            if (stateTimer <= 0f)
            {
                ResolveStrike();

                if (currentState == SlasherState.Attack)
                {
                    attackDidStrike = true;
                    stateTimer = attackRecover;
                }
            }

            return;
        }

        if (stateTimer <= 0f)
            ChooseMovementState();
    }

    private void UpdateHit()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
            ChooseMovementState();
    }

    private void UpdateDeath()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
            Destroy(gameObject);
    }

    private void EnterState(SlasherState newState)
    {
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
        else if (currentState == SlasherState.Attack)
        {
            PlayAnimation(attackAnimName);
            attackDidStrike = false;
            stateTimer = attackWindup;
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
        float dist = DistanceToPlayer();
        playerDetected = true;

        if (currentState == SlasherState.Death)
            return;

        if (dist <= attackRange)
        {
            EnterState(SlasherState.Attack);
            return;
        }

        if (dist > chaseDistance)
        {
            EnterState(SlasherState.Chase);
            return;
        }

        EnterState(SlasherState.Walk);
    }

    private void ResolveStrike()
    {
        float dist = DistanceToPlayer();
        if (dist > attackRange)
            return;

        PlayerController.PlayerEffectState effect = player.CurrentEffectState;

        if (effect == PlayerController.PlayerEffectState.CounterParry)
        {
            EnterState(SlasherState.Death);
            return;
        }

        if (effect == PlayerController.PlayerEffectState.Parry)
        {
            EnterState(SlasherState.Hit);
            return;
        }

        Vector2 atkPos = attackOrigin != null ? (Vector2)attackOrigin.position : (Vector2)transform.position;
        player.Hit(attackDamage, atkPos);
    }

    private float DistanceToPlayer()
    {
        Vector2 a = transform.position;
        Vector2 b = player.transform.position;
        return Vector2.Distance(a, b);
    }

    private void MoveTowardsPlayer(float speed)
    {
        Vector2 a = transform.position;
        Vector2 b = player.transform.position;
        float dx = b.x - a.x;
        float dirSign = dx > 0f ? 1f : (dx < 0f ? -1f : 0f);
        Vector2 v = rb.linearVelocity;
        v.x = dirSign * speed;
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
        if (animator != null && !string.IsNullOrEmpty(clipName))
            animator.Play(clipName);
    }
}