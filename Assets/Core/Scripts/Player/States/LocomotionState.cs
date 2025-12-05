using UnityEngine;

public sealed class LocomotionState : PlayerState
{
    public override PlayerStateType StateType => PlayerStateType.Locomotion;

    public LocomotionState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter() { }

    public override void Update()
    {
        if (TryHandleDash()) return;

        bool canNormalParry = (player.isGround || player.airParryAvailable) && player.HasParryBuffer;
        bool holding = player.ParryHeld;
        bool prepLocked = player.counterParryPrepLocked;

        if (holding)
        {
            player.parryHoldTimer += Time.deltaTime;

            if (!prepLocked && !player.inCounterParryPrep && player.parryHoldTimer >= player.Settings.counterParryHoldTime)
            {
                if (player.Vitals.TryConsumeEnergy(player.Settings.counterParryEnterCost))
                {
                    player.inCounterParryPrep = true;
                    player.counterParryPrepTickTimer = 0f;
                    player.counterParryPrepElapsed = 0f;
                }
                else
                {
                    player.inCounterParryPrep = false;
                    player.counterParryPrepLocked = true;
                }
            }

            if (player.inCounterParryPrep)
            {
                player.counterParryPrepElapsed += Time.deltaTime;

                if (player.counterParryPrepElapsed >= player.Settings.counterParryNoDrainTime)
                {
                    player.counterParryPrepTickTimer += Time.deltaTime;
                    if (player.counterParryPrepTickTimer >= player.Settings.counterParryDrainTick)
                    {
                        int ticks = Mathf.FloorToInt(player.counterParryPrepTickTimer / player.Settings.counterParryDrainTick);
                        player.counterParryPrepTickTimer -= ticks * player.Settings.counterParryDrainTick;
                        int cost = ticks * player.Settings.counterParryDrainCost;
                        if (!player.Vitals.TryConsumeEnergy(cost))
                        {
                            player.inCounterParryPrep = false;
                            player.counterParryPrepLocked = true;
                        }
                    }
                }
            }
        }
        else
        {
            if (player.inCounterParryPrep)
            {
                player.inCounterParryPrep = false;
                player.parryHoldTimer = 0f;
                player.ConsumeParryBuffer();
                stateMachine.ChangeState(new CounterParryState(player, stateMachine));
                return;
            }

            if (player.counterParryPrepLocked)
                player.counterParryPrepLocked = false;

            player.parryHoldTimer = 0f;
        }

        if (canNormalParry && !player.inCounterParryPrep && (!holding || player.parryHoldTimer < player.Settings.counterParryHoldTime))
        {
            player.ConsumeParryBuffer();
            stateMachine.ChangeState(new ParryState(player, stateMachine));
            return;
        }

        bool canStartHeal =
            player.HealHeld &&
            player.isGround &&
            player.Vitals.Health < player.Vitals.MaxHealth &&
            player.Vitals.Energy >= player.Settings.healEnergyPerTick;

        if (canStartHeal)
        {
            stateMachine.ChangeState(new HealState(player, stateMachine));
            return;
        }

        bool startedJump = false;

        if (player.jumpBufferTimer > 0f && (player.isGround || player.coyoteTimer > 0f))
        {
            player.jumpBufferTimer = 0f;
            player.isJumping = true;
            player.jumpTimeCounter = 0f;
            player.Anim.Play("Jump");
            startedJump = true;
        }

        if (!player.JumpHeld && player.isJumping)
            player.StopRising();

        if (startedJump) return;

        if (!player.isJumping)
        {
            if (player.isGround)
            {
                if (Mathf.Abs(player.CurrentVelocity.x) > 0.01f || Mathf.Abs(player.MoveInput) > 0.01f)
                    player.Anim.Play("Run");
                else
                    player.Anim.Play("Idle");
            }
            else
            {
                if (player.Rigidbody.linearVelocity.y < 0f)
                    player.Anim.Play("Fall");
            }
        }
    }

    public override void FixedUpdate()
    {
        player.HandleMove(player.Settings.moveSpeed);
        player.HandleJump();

        if (player.isJumping && player.jumpTimeCounter >= player.Settings.maxJumpTime)
            player.isJumping = false;
    }

    private bool TryHandleDash()
    {
        if (!player.HasDashBuffer) return false;
        if (Time.time < player.lastDashTime + player.Settings.dashCooldown) return false;
        if (!(player.isGround || player.canAirDash)) return false;

        player.ConsumeDashBuffer();
        stateMachine.ChangeState(new DashState(player, stateMachine));
        return true;
    }
}