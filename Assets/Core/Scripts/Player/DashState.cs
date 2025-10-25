using UnityEngine;

public sealed class DashState : PlayerState
{
    private float cachedGravity;

    public override PlayerStateType StateType => PlayerStateType.Dash;

    public DashState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.CancelJump(true);
        player.lastDashTime = Time.time;
        player.dashTimer = player.DashDuration;
        cachedGravity = player.Rigidbody.gravityScale;
        player.Rigidbody.gravityScale = 0f;
        player.SetEffectState(PlayerController.PlayerEffectState.Dash);
        if (player.isGround) player.Animator.Play("Ground Dash"); else player.Animator.Play("Air Dash");
        if (!player.isGround) player.canAirDash = false;

        player.SetInvincible(true);

        Projectile proj;
        if (player.TryDetectIncomingAttack(out proj))
        {
            player.GainEnergy(player.DashExtremeGain);
        }
    }

    public override void Update()
    {
        player.dashTimer -= Time.deltaTime;
        if (player.dashTimer <= 0f)
        {
            stateMachine.ChangeState(new LocomotionState(player, stateMachine));
        }
    }

    public override void FixedUpdate()
    {
        player.Rigidbody.linearVelocity = new Vector2(player.facingDirection * player.DashSpeed, 0f);
    }

    public override void Exit()
    {
        player.Rigidbody.gravityScale = cachedGravity;
        player.SetInvincible(false);
        player.SetEffectState(PlayerController.PlayerEffectState.None);
        player.postDashCarryDir = player.facingDirection;
        player.postDashCarryTimer = player.PostDashCarryWindow;
    }
}