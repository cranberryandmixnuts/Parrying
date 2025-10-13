using UnityEngine;

public sealed class LocomotionState : PlayerState
{
    public override PlayerStateType StateType => PlayerStateType.Locomotion;

    public LocomotionState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.SetEffectState(PlayerController.PlayerEffectState.None);
        player.ExitParryFlags();
    }

    public override void Update()
    {
        if (TryHandleDash()) return;

        if (player.ParryPressed)
        {
            player.ConsumeParryPressed();
            stateMachine.ChangeState(new ParryState(player, stateMachine));
            return;
        }

        if (player.ParryHeld && player.Energy >= player.CounterEnterCost)
        {
            stateMachine.ChangeState(new CounterParryState(player, stateMachine));
            return;
        }

        if (player.HealHeld && player.isGround && player.Health < player.MaxHealth && player.Energy > 0f)
        {
            stateMachine.ChangeState(new HealState(player, stateMachine));
            return;
        }

        if (player.jumpBufferTimer > 0f && (player.isGround || player.coyoteTimer > 0f))
        {
            player.jumpBufferTimer = 0f;
            player.isJumping = true;
            player.jumpTimeCounter = 0f;
            player.Animator.Play("Jump");
        }

        if (!player.JumpHeld && player.isJumping)
            player.StopRising();

        if (player.isGround)
        {
            if (Mathf.Abs(player.CurrentVelocity.x) > 0.01f || Mathf.Abs(player.MoveInput) > 0.01f)
                player.Animator.Play("Run");
            else
                player.Animator.Play("Idle");
        }
        else
        {
            if (player.Rigidbody.linearVelocity.y < 0f)
                player.Animator.Play("Fall");
        }
    }

    public override void FixedUpdate()
    {
        player.HandleMove(player.MoveSpeed);
        player.HandleJump();

        if (player.isJumping && player.jumpTimeCounter >= player.MaxJumpTime)
            player.isJumping = false;
    }
}
