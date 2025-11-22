using UnityEngine;

public sealed class LocomotionState : PlayerState
{
    public override PlayerStateType StateType => PlayerStateType.Locomotion;

    public LocomotionState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.SetEffectState(PlayerController.PlayerEffectState.None);
    }

    public override void Update()
    {
        if (TryHandleDash()) return;

        bool canNormalParry = (player.isGround || player.airParryAvailable) && player.HasParryBuffer();
        bool holding = player.ParryHeld;
        bool prepLocked = player.powerParryPrepLocked;

        if (holding)
        {
            player.parryHoldTimer += Time.deltaTime;

            if (!prepLocked && !player.inPowerParryPrep && player.parryHoldTimer >= player.Settings.powerParryHoldTime)
            {
                if (player.Vitals.TryConsumeEnergy(player.Settings.powerParryEnterCost))
                {
                    player.inPowerParryPrep = true;
                    player.powerParryPrepTickTimer = 0f;
                    player.powerParryPrepElapsed = 0f;
                }
                else
                {
                    player.inPowerParryPrep = false;
                    player.powerParryPrepLocked = true;
                }
            }

            if (player.inPowerParryPrep)
            {
                player.powerParryPrepElapsed += Time.deltaTime;

                if (player.powerParryPrepElapsed >= player.Settings.powerParryNoDrainTime)
                {
                    player.powerParryPrepTickTimer += Time.deltaTime;
                    if (player.powerParryPrepTickTimer >= player.Settings.powerParryDrainTick)
                    {
                        int ticks = Mathf.FloorToInt(player.powerParryPrepTickTimer / player.Settings.powerParryDrainTick);
                        player.powerParryPrepTickTimer -= ticks * player.Settings.powerParryDrainTick;
                        int cost = ticks * player.Settings.powerParryDrainCost;
                        if (!player.Vitals.TryConsumeEnergy(cost))
                        {
                            player.inPowerParryPrep = false;
                            player.powerParryPrepLocked = true;
                        }
                    }
                }
            }
        }
        else
        {
            if (player.inPowerParryPrep)
            {
                player.inPowerParryPrep = false;
                player.parryHoldTimer = 0f;
                player.ConsumeParryPressed();
                player.ConsumeParryBuffer();
                stateMachine.ChangeState(new CounterParryState(player, stateMachine));
                return;
            }

            if (player.powerParryPrepLocked)
                player.powerParryPrepLocked = false;

            player.parryHoldTimer = 0f;
        }

        if (canNormalParry && !player.inPowerParryPrep && (!holding || player.parryHoldTimer < player.Settings.powerParryHoldTime))
        {
            player.ConsumeParryPressed();
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
        if (!player.DashPressed) return false;
        if (Time.time < player.lastDashTime + player.Settings.dashCooldown) return false;
        if (!(player.isGround || player.canAirDash)) return false;

        player.ConsumeDashPressed();
        stateMachine.ChangeState(new DashState(player, stateMachine));
        return true;
    }
}