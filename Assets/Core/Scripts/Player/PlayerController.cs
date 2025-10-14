using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
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
        Dying
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
        set { if (value) jumpBufferTimer = jumpBufferTime; }
    }

    public PlayerStateType CurrentStateType => stateMachine.CurrentStateType;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 5f;
    [SerializeField] private float maxEnergy = 3f;
    [SerializeField] private float startHealth = 5f;
    [SerializeField] private float startEnergy = 1.5f;
    public float MaxHealth => maxHealth;
    public float MaxEnergy => maxEnergy;
    public float Health { get; private set; }
    public float Energy { get; private set; }

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
    public float DashSpeed => dashSpeed;
    public float DashDuration => dashDuration;
    public float DashCooldown => dashCooldown;

    [Header("Parry")]
    [SerializeField] private float parryWindow = 0.07f;
    [SerializeField] private float parryEnergyGain = 1.0f;
    [SerializeField] private float parryHitstop = 0.15f;
    [SerializeField] private float counterEnterCost = 0.5f;
    [SerializeField] private float counterDrainPerSecond = 0.4f;
    public float ParryWindow => parryWindow;
    public float CounterEnterCost => counterEnterCost;
    public float CounterDrainPerSecond => counterDrainPerSecond;

    [Header("Heal")]
    [SerializeField] private float healEnergyPerSecond = 1.2f;
    [SerializeField] private float healHealthPerSecond = 1.0f;
    [SerializeField] private float healEndLag = 0.3f;
    public float HealEnergyPerSecond => healEnergyPerSecond;
    public float HealHealthPerSecond => healHealthPerSecond;
    public float HealEndLag => healEndLag;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private BoxCollider2D groundCheckBox;

    [Header("Hit")]
    [SerializeField] private float hitStunDuration = 0.25f;
    [SerializeField] private float knockbackDuration = 0.1f;
    [SerializeField] private float knockbackForce = 10f;
    public float HitStunDuration => hitStunDuration;

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

        Health = Mathf.Clamp(startHealth, 0f, maxHealth);
        Energy = Mathf.Clamp(startEnergy, 0f, maxEnergy);
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

        if (jumpBufferTimer > 0f) jumpBufferTimer -= Time.deltaTime;

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
        HealHeld = Input.GetKey(keyData.Player.HealKey);
    }

    

    public void ConsumeParryPressed()
    {
        ParryPressed = false;
    }

    public void ConsumeDashPressed()
    {
        DashPressed = false;
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
    }

    public void HandleMove(float speed)
    {
        if (isKnockback) return;

        rb.linearVelocity = new Vector2(MoveInput * speed, rb.linearVelocity.y);

        if (MoveInput != 0)
        {
            facingDirection = MoveInput > 0 ? 1 : -1;
            //transform.rotation = Quaternion.Euler(0f, facingDirection == -1 ? 180f : 0f, 0f); 반전코드
        }
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

    public void Heal(float amount)
    {
        Health = Mathf.Clamp(Health + amount, 0f, maxHealth);
    }

    public bool TryConsumeEnergy(float amount)
    {
        if (Energy < amount) return false;
        Energy = Mathf.Clamp(Energy - amount, 0f, maxEnergy);
        return true;
    }

    public void GainEnergy(float amount)
    {
        Energy = Mathf.Clamp(Energy + amount, 0f, maxEnergy);
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
        if (!gameObject.activeInHierarchy) return;
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

    public void ExitParryFlags()
    {
        parryWindowActive = false;
        counterParryActive = false;
    }

    public void OnIncomingAttack(AttackData data)
    {
        if (isInvincible) return;

        if (data.Parryable && counterParryActive)
        {
            GainEnergy(parryEnergyGain);
            StartCoroutine(CoHitstop(parryHitstop));
            return;
        }

        if (data.Parryable && parryWindowActive)
        {
            GainEnergy(parryEnergyGain);
            StartCoroutine(CoHitstop(parryHitstop));
            return;
        }

        TakeDamage(data);
    }

    private void TakeDamage(AttackData data)
    {
        Health = Mathf.Clamp(Health - data.Damage, 0f, maxHealth);
        if (Health <= 0f)
        {
            stateMachine.ChangeState(new DieState(this, stateMachine));
            return;
        }

        Vector2 dir = data.Direction.sqrMagnitude < 0.0001f ? new Vector2(-facingDirection, 1f) : data.Direction;
        ApplyKnockback(dir, knockbackForce);
        stateMachine.ChangeState(new HitState(this, stateMachine));
    }

    private IEnumerator CoHitstop(float duration)
    {
        float prev = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = prev;
    }

    public void Die()
    {
        stateMachine.ChangeState(new DieState(this, stateMachine));
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        isKnockback = true;
        knockbackTimer = knockbackDuration;
        rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
        SetEffectState(PlayerEffectState.Hit);
    }

    public void SetInvincible(bool v)
    {
        isInvincible = v;
    }

    public Rigidbody2D Rigidbody => rb;
    public BoxCollider2D BoxCollider => boxCol;
    public SpriteRenderer SpriteRenderer => spriteRenderer;
    public PlayerEffectState CurrentEffectState { get; private set; }
}
