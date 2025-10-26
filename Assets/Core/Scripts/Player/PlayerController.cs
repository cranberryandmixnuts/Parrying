using System;
using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour, IDamageable
{
    public static PlayerController Instance { get; private set; }
    public Animator Animator { get; private set; }

    public enum PlayerEffectState
    {
        None,
        Dash,
        Heal,
        Parry,
        CounterParry,
        Hit,
        Death
    }

    public event Action<PlayerEffectState> OnEffectStateChanged;

    [SerializeField] private KeyData keyData;

    public float MoveInput { get; private set; }
    public bool DashPressed { get; private set; }
    public bool ParryPressed { get; private set; }
    public bool ParryHeld { get; private set; }
    public bool HealHeld { get; private set; }

    public bool JumpPressed
    {
        set
        {
            if (value) jumpBufferTimer = jumpBufferTime;
        }
    }

    public PlayerStateType CurrentStateType => stateMachine.CurrentStateType;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 1000;
    [SerializeField] private int maxEnergy = 500;
    [SerializeField] private int startHealth = 1000;
    [SerializeField] private int startEnergy = 500;
    public int MaxHealth => maxHealth;
    public int MaxEnergy => maxEnergy;
    public int Health { get; private set; }
    public int Energy { get; private set; }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    public float MoveSpeed => moveSpeed;
    public Vector2 CurrentVelocity => rb.linearVelocity;

    [Header("Jump")]
    [SerializeField] private AnimationCurve jumpForceCurve;
    [SerializeField] private float maxJumpTime = 0.4f;
    [SerializeField] private float maxJumpForce = 20f;
    [SerializeField] private float jumpHeightMultiplier = 1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float coyoteTime = 0.1f;
    public float MaxJumpTime => maxJumpTime;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 50f;
    [SerializeField] private float dashDuration = 0.1f;
    [SerializeField] private float dashCooldown = 0.8f;
    [SerializeField] private int dashExtremeGain = 50;
    public float DashSpeed => dashSpeed;
    public float DashDuration => dashDuration;
    public float DashCooldown => dashCooldown;
    public int DashExtremeGain => dashExtremeGain;

    [Header("Parry")]
    [SerializeField] private float parryWindow = 0.2f;
    [SerializeField] private float parryHitstop = 0.15f;
    [SerializeField] private int perfectParryEnergyGain = 100;
    [SerializeField] private int imperfectParryEnergyGain = 50;
    public float ParryWindow => parryWindow;
    public float ParryHitstop => parryHitstop;
    public int PerfectParryEnergyGain => perfectParryEnergyGain;
    public int ImperfectParryEnergyGain => imperfectParryEnergyGain;

    [Header("Power Parry")]
    [SerializeField] private float powerParryHoldTime = 0.1f;
    [SerializeField] private float powerParryPrepTick = 0.1f;
    [SerializeField] private int powerParryPrepEnterCost = 300;
    [SerializeField] private int powerParryPrepCost = 5;
    [SerializeField] private float powerParryNoDrainTime = 0.6f;
    [SerializeField] private float powerParryDuration = 0.6f;
    public float PowerParryHoldTime => powerParryHoldTime;
    public float PowerParryPrepTick => powerParryPrepTick;
    public int PowerParryPrepEnterCost => powerParryPrepEnterCost;
    public int PowerParryPrepCost => powerParryPrepCost;
    public float PowerParryNoDrainTime => powerParryNoDrainTime;
    public float PowerParryDuration => powerParryDuration;

    [Header("Heal")]
    [SerializeField] private float healTickInterval = 0.1f;
    [SerializeField] private int healEnergyPerTick = 10;
    [SerializeField] private int healHealthPerTick = 10;
    [SerializeField] private float healEndLag = 0.3f;
    public float HealTickInterval => healTickInterval;
    public int HealEnergyPerTick => healEnergyPerTick;
    public int HealHealthPerTick => healHealthPerTick;
    public float HealEndLag => healEndLag;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private BoxCollider2D groundCheckBox;

    [Header("Hit")]
    [SerializeField] private float hitStunDuration = 0.25f;
    [SerializeField] private float knockbackDuration = 0.1f;
    [SerializeField] private float knockbackForce = 10f;
    public float HitStunDuration => hitStunDuration;
    public float KnockbackForce => knockbackForce;

    [Header("Move Inertia")]
    [SerializeField] private float accelTime = 0.3f;
    [SerializeField] private float airReleaseDecelTime = 0.4f;
    [SerializeField][Range(0f, 1f)] private float startSpeedRatio = 0.45f;
    [SerializeField] private float postDashCarryWindow = 0.1f;
    public float PostDashCarryWindow => postDashCarryWindow;

    [Header("Parry Detect")]
    [SerializeField] private CircleCollider2D parryDetectCollider;
    [SerializeField] private LayerMask parryDetectMask;

    public bool isGround;

    private Rigidbody2D rb;
    private BoxCollider2D boxCol;
    private SpriteRenderer spriteRenderer;

    [HideInInspector] public int facingDirection = 1;
    [HideInInspector] public bool isJumping;
    [HideInInspector] public bool JumpHeld;
    [HideInInspector] public float jumpTimeCounter;
    [HideInInspector] public float jumpBufferTimer;
    [HideInInspector] public float coyoteTimer;
    [HideInInspector] public bool canAirDash = true;
    [HideInInspector] public float lastDashTime = -999f;
    [HideInInspector] public float dashTimer;

    [HideInInspector] public float currentSpeedAbs;
    [HideInInspector] public int lastMoveSign;
    [HideInInspector] public float postDashCarryTimer;
    [HideInInspector] public int postDashCarryDir;

    [HideInInspector] public float parryBufferTimer;
    [HideInInspector] public bool airParryAvailable = true;

    [HideInInspector] public float parryHoldTimer;
    [HideInInspector] public bool inPowerParryPrep;
    [HideInInspector] public float powerParryPrepTickTimer;
    [HideInInspector] public bool powerParryPrepLocked;
    [HideInInspector] public float powerParryPrepElapsed;

    [HideInInspector] public float parryWindowStartTime;
    [HideInInspector] public float parryWindowDuration;
    [HideInInspector] public bool parryHadSuccessThisWindow;
    [HideInInspector] public bool counterParryFirstResolved;
    private float parryGraceEndTime;
    public bool IsParryGraceActive => Time.time < parryGraceEndTime;

    [HideInInspector] public Vector2 lastHitKnockDir;

    private PlayerStateMachine stateMachine;
    private bool isInvincible = false;
    private bool isKnockback = false;
    private float knockbackTimer = 0f;

    private bool parryWindowActive = false;
    private bool counterParryActive = false;
    private bool healLocked = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCol = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        Animator = GetComponent<Animator>();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (keyData == null)
            throw new InvalidOperationException("KeyData is required");
        if (groundCheckBox == null)
            throw new InvalidOperationException("GroundCheck BoxCollider2D is required");

        Health = Mathf.Clamp(startHealth, 0, maxHealth);
        Energy = Mathf.Clamp(startEnergy, 0, maxEnergy);
    }

    private void Start()
    {
        stateMachine = new PlayerStateMachine();
        stateMachine.Initialize(new LocomotionState(this, stateMachine));
        SetEffectState(PlayerEffectState.None);
    }

    private void Update()
    {
        PollInput();
        UpdateGround();

        if (postDashCarryTimer > 0f) postDashCarryTimer -= Time.deltaTime;
        if (jumpBufferTimer > 0f) jumpBufferTimer -= Time.deltaTime;
        if (parryBufferTimer > 0f) parryBufferTimer -= Time.deltaTime;

        if (isKnockback)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0f) isKnockback = false;
        }

        stateMachine.Update();
    }

    private void FixedUpdate()
    {
        stateMachine.FixedUpdate();
    }

    private void PollInput()
    {
        float horizontal = 0f;
        if (Input.GetKey(keyData.Player.LeftMoveKey)) horizontal -= 1f;
        if (Input.GetKey(keyData.Player.RightMoveKey)) horizontal += 1f;
        MoveInput = horizontal;

        bool jp = Input.GetKeyDown(keyData.Player.JumpKey);
        JumpPressed = jp;
        JumpHeld = Input.GetKey(keyData.Player.JumpKey);
        if (Input.GetKeyUp(keyData.Player.JumpKey)) StopRising();

        DashPressed = Input.GetKeyDown(keyData.Player.DashKey);
        ParryHeld = Input.GetKey(keyData.Player.ParryKey);
        ParryPressed = Input.GetKeyDown(keyData.Player.ParryKey);
        if (ParryPressed) SetParryBuffer();
        if (!ParryHeld) parryHoldTimer = 0f;
        HealHeld = Input.GetKey(keyData.Player.HealKey);
    }

    public void SetEffectState(PlayerEffectState newState)
    {
        if (CurrentEffectState == newState) return;
        CurrentEffectState = newState;
        OnEffectStateChanged?.Invoke(newState);
    }

    private void UpdateGround()
    {
        Bounds b = groundCheckBox.bounds;
        isGround = Physics2D.OverlapBox(b.center, b.size, 0f, groundLayer) != null;

        if (isGround) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        if (isGround) canAirDash = true;
        if (isGround) airParryAvailable = true;
    }

    public void HandleMove(float speed)
    {
        if (isKnockback) return;

        int inputSign = 0;
        if (MoveInput > 0.01f) inputSign = 1;
        else if (MoveInput < -0.01f) inputSign = -1;

        float dt = Time.fixedDeltaTime;

        if (inputSign == 0)
        {
            if (isGround) currentSpeedAbs = 0f;
            else
            {
                if (airReleaseDecelTime > 0f)
                {
                    float decel = (currentSpeedAbs / airReleaseDecelTime) * dt;
                    currentSpeedAbs = Mathf.Max(0f, currentSpeedAbs - decel);
                }
                else currentSpeedAbs = 0f;
            }
        }
        else
        {
            bool directionChanged = lastMoveSign != 0 && inputSign != lastMoveSign && currentSpeedAbs > 0.001f;

            if (postDashCarryTimer > 0f && inputSign == postDashCarryDir)
            {
                currentSpeedAbs = speed;
                postDashCarryTimer = 0f;
            }
            else
            {
                float startSpeed = Mathf.Max(0.0001f, startSpeedRatio * speed);
                if (directionChanged || currentSpeedAbs <= 0f) currentSpeedAbs = startSpeed;

                if (accelTime <= 0f) currentSpeedAbs = speed;
                else
                {
                    float accel = (speed - currentSpeedAbs) / accelTime * dt;
                    currentSpeedAbs = Mathf.Clamp(currentSpeedAbs + accel, 0f, speed);
                }
            }

            lastMoveSign = inputSign;
            facingDirection = inputSign > 0 ? 1 : -1;
        }

        float vx = (inputSign != 0 ? inputSign : (currentSpeedAbs > 0.001f ? lastMoveSign : 0)) * currentSpeedAbs;
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
    }

    public void HandleJump()
    {
        if (isJumping && CurrentStateType == PlayerStateType.Locomotion)
        {
            jumpTimeCounter += Time.fixedDeltaTime;
            float t = jumpTimeCounter / maxJumpTime;
            float force = jumpForceCurve.Evaluate(t) * maxJumpForce * jumpHeightMultiplier;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
        }
    }

    public void StopRising()
    {
        if (rb.linearVelocity.y > 0f) rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.3f);
        jumpTimeCounter = maxJumpTime;
    }

    public void CancelJump(bool cutVelocity)
    {
        isJumping = false;
        if (cutVelocity) StopRising();
    }

    public void Heal(int amount)
    {
        Health = Mathf.Clamp(Health + amount, 0, maxHealth);
    }

    public bool TryConsumeEnergy(int amount)
    {
        if (Energy < amount) return false;
        Energy = Mathf.Clamp(Energy - amount, 0, maxEnergy);
        return true;
    }

    public void GainEnergy(int amount)
    {
        Energy = Mathf.Clamp(Energy + amount, 0, maxEnergy);
    }

    public void EnterParryWindow()
    {
        parryWindowActive = true;
        SetEffectState(PlayerEffectState.Parry);
    }

    public void ExitParryWindow()
    {
        parryWindowActive = false;
        if (CurrentEffectState == PlayerEffectState.Parry) SetEffectState(PlayerEffectState.None);
    }

    public void EnterCounterParry()
    {
        counterParryActive = true;
        SetEffectState(PlayerEffectState.CounterParry);
    }

    public void ExitCounterParry()
    {
        counterParryActive = false;
        if (CurrentEffectState == PlayerEffectState.CounterParry) SetEffectState(PlayerEffectState.None);
    }

    public void EnterHeal()
    {
        if (healLocked) return;
        SetEffectState(PlayerEffectState.Heal);
    }

    public void ExitHeal()
    {
        if (CurrentEffectState == PlayerEffectState.Heal) SetEffectState(PlayerEffectState.None);
    }

    public void StartHealEndLag()
    {
        StartCoroutine(CoHealEndLag());
    }

    private IEnumerator CoHealEndLag()
    {
        healLocked = true;
        float t = healEndLag;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }
        healLocked = false;
    }

    public bool CanStartHeal()
    {
        return !healLocked;
    }

    public void ExitParryFlags()
    {
        parryWindowActive = false;
        counterParryActive = false;
    }

    public void Die()
    {
        stateMachine.ChangeState(new DeathState(this, stateMachine));
    }

    public void SetInvincible(bool v)
    {
        isInvincible = v;
    }

    public void ConsumeParryPressed()
    {
        ParryPressed = false;
    }

    public void ConsumeDashPressed()
    {
        DashPressed = false;
    }

    public bool HasParryBuffer()
    {
        return parryBufferTimer > 0f;
    }

    public void SetParryBuffer()
    {
        parryBufferTimer = 0.06f;
    }

    public void ConsumeParryBuffer()
    {
        parryBufferTimer = 0f;
    }

    public void NotifyParryWindowBegin(float duration)
    {
        parryWindowStartTime = Time.time;
        parryWindowDuration = duration;
        parryHadSuccessThisWindow = false;
    }

    public void NotifyParryWindowEnd()
    {
        parryWindowDuration = 0f;
    }

    public void AddParryGrace(float duration)
    {
        if (duration > 0f) parryGraceEndTime = Mathf.Max(parryGraceEndTime, Time.time + duration);
    }

    public void ApplyChipDamageNoHit(int amount)
    {
        Health = Mathf.Clamp(Health - amount, 0, maxHealth);
        if (Health <= 0) stateMachine.ChangeState(new DeathState(this, stateMachine));
    }

    private void ForceBreakPowerParryPrep()
    {
        if (inPowerParryPrep) inPowerParryPrep = false;
        powerParryPrepLocked = true;
        parryHoldTimer = 0f;
        powerParryPrepTickTimer = 0f;
        powerParryPrepElapsed = 0f;
    }

    public void Hit(int damage, Vector2 attackPos)
    {
        if (isInvincible) return;
        if (IsParryGraceActive) return;

        ForceBreakPowerParryPrep();

        Health = Mathf.Clamp(Health - damage, 0, maxHealth);

        Vector2 dir = ((Vector2)transform.position - attackPos).normalized;
        if (dir == Vector2.zero) dir = Vector2.up;
        lastHitKnockDir = dir;

        if (Health <= 0)
        {
            stateMachine.ChangeState(new DeathState(this, stateMachine));
            return;
        }

        stateMachine.ChangeState(new HitState(this, stateMachine));
    }

    public bool TryDetectIncomingAttack(out Projectile projectile)
    {
        projectile = null;

        float scaleX = Mathf.Abs(parryDetectCollider.transform.lossyScale.x);
        float scaleY = Mathf.Abs(parryDetectCollider.transform.lossyScale.y);
        float worldRadius = parryDetectCollider.radius * Mathf.Max(scaleX, scaleY);
        Vector2 center = parryDetectCollider.bounds.center;
        float r2 = worldRadius * worldRadius;

        float bestDistSq = float.PositiveInfinity;
        Projectile bestProj = null;

        for (int i = 0; i < Projectile.ActiveProjectiles.Count; i++)
        {
            Projectile pr = Projectile.ActiveProjectiles[i];
            if (pr == null) continue;
            if (!pr.IsDeadly) continue;

            Vector2 prPos = pr.transform.position;
            float d2 = (prPos - center).sqrMagnitude;
            if (d2 <= r2 && d2 < bestDistSq)
            {
                bestDistSq = d2;
                bestProj = pr;
            }
        }

        if (bestProj == null) return false;
        projectile = bestProj;
        return true;
    }

    public Rigidbody2D Rigidbody => rb;
    public BoxCollider2D BoxCollider => boxCol;
    public SpriteRenderer SpriteRenderer => spriteRenderer;
    public PlayerEffectState CurrentEffectState { get; private set; }
}