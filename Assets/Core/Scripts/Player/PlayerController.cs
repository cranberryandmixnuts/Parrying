using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }
    public Animator Anim { get; private set; }

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

    [SerializeField] private PlayerSettings settings;
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
            if (value) jumpBufferTimer = settings.jumpBufferTime;
        }
    }

    public PlayerStateType CurrentStateType => stateMachine.CurrentStateType;
    public float MoveSpeed => settings.moveSpeed;
    public Vector2 CurrentVelocity => rb.linearVelocity;
    public float MaxJumpTime => settings.maxJumpTime;
    public float DashSpeed => settings.dashSpeed;
    public float DashCooldown => settings.dashCooldown;
    public float DashExtremeExtraInvincibility => settings.extremeDashExtraInvincibility;
    public int PerfectParryEnergyGain => settings.perfectParryEnergyGain;
    public int ImperfectParryEnergyGain => settings.imperfectParryEnergyGain;
    public float PowerParryHoldTime => settings.powerParryHoldTime;
    public float PowerParryPrepTick => settings.powerParryPrepTick;
    public int PowerParryPrepEnterCost => settings.powerParryPrepEnterCost;
    public int PowerParryPrepCost => settings.powerParryPrepCost;
    public float PowerParryNoDrainTime => settings.powerParryNoDrainTime;
    public float HealTickInterval => settings.healTickInterval;
    public int HealEnergyPerTick => settings.healEnergyPerTick;
    public int HealHealthPerTick => settings.healHealthPerTick;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private BoxCollider2D groundCheckBox;

    public float HitInvincibleTime => settings.hitInvincibleTime;
    public float KnockbackForce => settings.knockbackForce;
    public float KnockbackDuration => settings.knockbackDuration;
    public float PostDashCarryWindow => settings.postDashCarryWindow;

    [Header("Detect")]
    [SerializeField] private CircleCollider2D parryDetectCollider;
    [SerializeField] private CircleCollider2D dashDetectCollider;

    [HideInInspector] public bool isGround;

    private Rigidbody2D rb;
    private BoxCollider2D boxCol;

    [HideInInspector] public List<ParryCandidate> parryCandidates = new();
    [HideInInspector] public List<DashCandidate> dashCandidates = new();


    [HideInInspector] public int facingDirection = 1;
    [HideInInspector] public bool isJumping;
    [HideInInspector] public bool JumpHeld;
    [HideInInspector] public float jumpTimeCounter;
    [HideInInspector] public float jumpBufferTimer;
    [HideInInspector] public float coyoteTimer;
    [HideInInspector] public bool canAirDash = true;
    [HideInInspector] public float lastDashTime = -999f;
    [HideInInspector] public float lastExtremeDash = -999f;

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
    public bool CanExtremeDash => (Time.time - lastExtremeDash) >= settings.extremeDashCooldown;

    [HideInInspector] public Vector2 lastHitKnockDir;
    private PlayerStateMachine stateMachine;

    public PlayerVitals Vitals;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCol = GetComponent<BoxCollider2D>();
        Anim = GetComponent<Animator>();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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

        stateMachine.Update();
    }

    private void FixedUpdate()
    {
        stateMachine.FixedUpdate();
    }

    private void LateUpdate()
    {
        int f = Time.frameCount;
        for (int i = dashCandidates.Count - 1; i >= 0; i--)
        {
            if (dashCandidates[i].frame < f) dashCandidates.RemoveAt(i);
        }
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
        isGround = groundCheckBox.IsTouchingLayers(groundLayer);

        if (isGround) coyoteTimer = settings.coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        if (isGround) canAirDash = true;
        if (isGround) airParryAvailable = true;
    }

    public void HandleMove(float speed)
    {
        int inputSign = 0;
        if (MoveInput > 0.01f) inputSign = 1;
        else if (MoveInput < -0.01f) inputSign = -1;

        float dt = Time.fixedDeltaTime;

        if (inputSign == 0)
        {
            if (isGround) currentSpeedAbs = 0f;
            else
            {
                if (settings.airReleaseDecelTime > 0f)
                {
                    float decel = (currentSpeedAbs / settings.airReleaseDecelTime) * dt;
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
                float startSpeed = Mathf.Max(0.0001f, settings.startSpeedRatio * speed);
                if (directionChanged || currentSpeedAbs <= 0f) currentSpeedAbs = startSpeed;

                if (settings.accelTime <= 0f) currentSpeedAbs = speed;
                else
                {
                    float accel = (speed - currentSpeedAbs) / settings.accelTime * dt;
                    currentSpeedAbs = Mathf.Clamp(currentSpeedAbs + accel, 0f, speed);
                }
            }

            lastMoveSign = inputSign;
            facingDirection = inputSign > 0 ? 1 : -1;
        }

        float vx = (inputSign != 0 ? inputSign : (currentSpeedAbs > 0.001f ? lastMoveSign : 0)) * currentSpeedAbs;
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

        transform.rotation = Quaternion.Euler(0f, facingDirection == -1 ? 180f : 0f, 0f);
    }

    public void HandleJump()
    {
        if (isJumping && CurrentStateType == PlayerStateType.Locomotion)
        {
            jumpTimeCounter += Time.fixedDeltaTime;
            float t = jumpTimeCounter / settings.maxJumpTime;
            float force = settings.jumpForceCurve.Evaluate(t) * settings.maxJumpForce * settings.jumpHeightMultiplier;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
        }
    }

    public void StopRising()
    {
        if (rb.linearVelocity.y > 0f) rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.3f);
        jumpTimeCounter = settings.maxJumpTime;
    }

    public void CancelJump(bool cutVelocity)
    {
        isJumping = false;
        if (cutVelocity) StopRising();
    }

    public void RegisterParryCandidate(IParryReactive attacker, Vector2 hitPoint, int damage)
    {
        ParryCandidate c;
        c.attacker = attacker;
        c.hitPoint = hitPoint;
        Vector2 p = transform.position;
        c.sqrDistance = ((Vector2)p - hitPoint).sqrMagnitude;
        c.ImperfectParryDamage = damage / 2;
        parryCandidates.Add(c);
    }

    public void ClearParryCandidate(IParryReactive attacker)
    {
        for (int i = parryCandidates.Count - 1; i >= 0; i--)
        {
            if (parryCandidates[i].attacker == attacker)
                parryCandidates.RemoveAt(i);
        }
    }

    public void RegisterDashCandidate(Vector2 point)
    {
        DashCandidate c;
        c.point = point;
        c.frame = Time.frameCount;
        dashCandidates.Add(c);
    }

    public void GetParryDetectCircle(out Vector2 center, out float radius)
    {
        float scaleX = Mathf.Abs(parryDetectCollider.transform.lossyScale.x);
        float scaleY = Mathf.Abs(parryDetectCollider.transform.lossyScale.y);
        float scale = Mathf.Max(scaleX, scaleY);

        radius = parryDetectCollider.radius * scale;
        center = parryDetectCollider.bounds.center;
    }

    public void GetDashDetectCircle(out Vector2 center, out float radius)
    {
        float scaleX = Mathf.Abs(dashDetectCollider.transform.lossyScale.x);
        float scaleY = Mathf.Abs(dashDetectCollider.transform.lossyScale.y);
        float scale = Mathf.Max(scaleX, scaleY);

        radius = dashDetectCollider.radius * scale;
        center = dashDetectCollider.bounds.center;
    }

    public void Die()
    {
        Destroy(gameObject);
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

    public bool TryHit(int damage, Vector2 attackPos)
    {
        if (IsParryGraceActive) return false;
        if (!Vitals.ApplyDamage(damage, false)) return false;

        inPowerParryPrep = false;
        powerParryPrepLocked = true;
        parryHoldTimer = 0f;
        powerParryPrepTickTimer = 0f;
        powerParryPrepElapsed = 0f;


        Vector2 dir = ((Vector2)transform.position - attackPos).normalized;
        if (dir == Vector2.zero) dir = Vector2.up;
        lastHitKnockDir = dir;

        if(Vitals.Health > 0f) stateMachine.ChangeState(new HitState(this, stateMachine));
        else stateMachine.ChangeState(new DeathState(this, stateMachine));

        return true;
    }

    public float GetAnimLength(string stateName)
    {
        AnimatorStateInfo current = Anim.GetCurrentAnimatorStateInfo(0);
        if (current.IsName(stateName))
        {
            float global = Anim.speed;
            if (global <= 0f) return Mathf.Infinity;
            return current.length / global;
        }

        AnimatorStateInfo next = Anim.GetNextAnimatorStateInfo(0);
        if (next.IsName(stateName))
        {
            float global = Anim.speed;
            if (global <= 0f) return Mathf.Infinity;
            return next.length / global;
        }

        Anim.Update(0f);

        current = Anim.GetCurrentAnimatorStateInfo(0);
        if (current.IsName(stateName))
        {
            float global = Anim.speed;
            if (global <= 0f) return Mathf.Infinity;
            return current.length / global;
        }

        next = Anim.GetNextAnimatorStateInfo(0);
        if (next.IsName(stateName))
        {
            float global = Anim.speed;
            if (global <= 0f) return Mathf.Infinity;
            return next.length / global;
        }

        Debug.LogError($"PlayerController: Animator state '{stateName}' not found or not playing.");
        return 0f;
    }

    public Rigidbody2D Rigidbody => rb;
    public BoxCollider2D BoxCollider => boxCol;
    public PlayerEffectState CurrentEffectState { get; private set; }
}