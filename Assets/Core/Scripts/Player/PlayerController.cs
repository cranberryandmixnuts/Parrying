using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }
    public Animator Anim { get; private set; }

    public float MoveInput { get; private set; }
    public bool ParryHeld { get; private set; }
    public bool HealHeld { get; private set; }
    public bool ParryPressed
    {
        set
        {
            if (value) parryBufferTimer = settings.parryBufferTime;
        }
    }

    public bool DashPressed
    {
        set
        {
            if (value) dashBufferTimer = settings.dashBufferTime;
        }
    }

    public bool JumpPressed
    {
        set
        {
            if (value) jumpBufferTimer = settings.jumpBufferTime;
        }
    }

    public PlayerStateType CurrentStateType => stateMachine.CurrentStateType;
    public Vector2 CurrentVelocity => rb.linearVelocity;

    [Header("Scene Refs")]
    [SerializeField] private PlayerVitals vitals;
    [SerializeField] private PlayerSettings settings;
    [SerializeField] private PlayerEffects effects;
    public PlayerVitals Vitals => vitals;
    public PlayerSettings Settings => settings;
    public PlayerEffects Effects => effects;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private BoxCollider2D groundCheckBox;

    [Header("Combat Detect")]
    [SerializeField] private CircleCollider2D parryDetectCollider;
    [SerializeField] private CircleCollider2D dashDetectCollider;

    private Rigidbody2D rb;
    private BoxCollider2D boxCol;

    [HideInInspector] public List<ParryCandidate> parryCandidates = new();
    [HideInInspector] public List<DashCandidate> dashCandidates = new();

    [HideInInspector] public bool isGround;
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
    [HideInInspector] public float dashBufferTimer;
    [HideInInspector] public bool airParryAvailable = true;
    [HideInInspector] public float parryHoldTimer;
    [HideInInspector] public bool inCounterParryPrep;
    [HideInInspector] public float counterParryPrepTickTimer;
    [HideInInspector] public bool counterParryPrepLocked;
    [HideInInspector] public float counterParryPrepElapsed;
    [HideInInspector] public float parryWindowStartTime;
    [HideInInspector] public float parryWindowDuration;
    [HideInInspector] public bool parryHadSuccessThisWindow;
    [HideInInspector] public bool counterParryFirstResolved;

    public bool HasParryBuffer => parryBufferTimer > 0f;
    public void ConsumeParryBuffer() => parryBufferTimer = 0f;

    public bool HasDashBuffer => dashBufferTimer > 0f;
    public void ConsumeDashBuffer() => dashBufferTimer = 0f;

    private PlayerStateMachine stateMachine;

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
    }

    private void Update()
    {
        PollInput();
        UpdateGround();

        if (postDashCarryTimer > 0f) postDashCarryTimer -= Time.deltaTime;
        if (jumpBufferTimer > 0f) jumpBufferTimer -= Time.deltaTime;
        if (parryBufferTimer > 0f) parryBufferTimer -= Time.deltaTime;
        if (dashBufferTimer > 0f) dashBufferTimer -= Time.deltaTime;

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
        InputService input = InputService.Instance;

        MoveInput = input.MoveAxis;

        JumpPressed = input.JumpDown;
        JumpHeld = input.JumpHeld;
        if (input.JumpUp) StopRising();

        DashPressed = input.DashDown;

        ParryHeld = input.ParryHeld;
        ParryPressed = input.ParryDown;
        if (!ParryHeld) parryHoldTimer = 0f;
        HealHeld = input.HealHeld;
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
            if (isGround)
                currentSpeedAbs = 0f;
            else
            {
                float baseSpeed = Mathf.Abs(rb.linearVelocity.x);

                if (settings.airReleaseDecelTime > 0f)
                {
                    float decel = (baseSpeed / settings.airReleaseDecelTime) * dt;
                    currentSpeedAbs = Mathf.Max(0f, baseSpeed - decel);
                }
                else
                    currentSpeedAbs = 0f;

                if (Mathf.Abs(rb.linearVelocity.x) > 0.001f)
                    lastMoveSign = rb.linearVelocity.x >= 0f ? 1 : -1;
                else if (currentSpeedAbs <= 0.001f)
                    lastMoveSign = 0;
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
                if (directionChanged || currentSpeedAbs <= 0f)
                    currentSpeedAbs = startSpeed;

                float accelTime = isGround ? settings.groundAccelTime : settings.airAccelTime;

                if (accelTime <= 0f)
                    currentSpeedAbs = speed;
                else
                {
                    float accel = (speed - currentSpeedAbs) / accelTime * dt;
                    currentSpeedAbs = Mathf.Clamp(currentSpeedAbs + accel, 0f, speed);
                }
            }

            lastMoveSign = inputSign;
            facingDirection = inputSign > 0 ? 1 : -1;
        }

        float vxDir;

        if (inputSign != 0)
            vxDir = inputSign;
        else if (currentSpeedAbs > 0.001f)
            vxDir = lastMoveSign;
        else
            vxDir = 0f;

        float vx = vxDir * currentSpeedAbs;
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

        transform.rotation = Quaternion.Euler(0f, facingDirection == -1 ? 180f : 0f, 0f);
    }

    public void HandleJump()
    {
        if (isJumping)
        {
            jumpTimeCounter += Time.fixedDeltaTime;
            float t = jumpTimeCounter / settings.maxJumpTime;
            float force = settings.jumpForceCurve.Evaluate(t) * settings.maxJumpForce;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
        }
    }

    public void StopRising()
    {
        if (rb.linearVelocity.y > 0f) rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.3f);
        jumpTimeCounter = settings.maxJumpTime;
    }

    public void CancelJump()
    {
        isJumping = false;
        StopRising();
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
        //Destroy(gameObject);
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

    public bool TryHit(int damage, Vector2 attackPos)
    {
        if (!Vitals.ApplyDamage(damage, false)) return false;

        inCounterParryPrep = false;
        counterParryPrepLocked = true;
        parryHoldTimer = 0f;
        counterParryPrepTickTimer = 0f;
        counterParryPrepElapsed = 0f;

        if (Vitals.Health > 0f) stateMachine.ChangeState(new HitState(this, stateMachine, ((Vector2)transform.position).x > attackPos.x));
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
}