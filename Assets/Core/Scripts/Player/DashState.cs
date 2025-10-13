using UnityEngine;

public sealed class DashState : PlayerState
{
    private float cachedGravity;

    public override PlayerStateType StateType => PlayerStateType.Dash;

    public DashState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.lastDashTime = Time.time;
        player.dashTimer = player.DashDuration;
        cachedGravity = player.Rigidbody.gravityScale;
        player.Rigidbody.gravityScale = 0f;
        player.SetEffectState(PlayerController.PlayerEffectState.Dash);
        player.Animator.Play("Dash");
        if (!player.isGround) player.canAirDash = false;
    }

    public override void Update()
    {
        player.dashTimer -= Time.deltaTime;
        if (player.dashTimer <= 0f)
            stateMachine.ChangeState(new LocomotionState(player, stateMachine));
    }

    public override void FixedUpdate()
    {
        player.Rigidbody.linearVelocity = new Vector2(player.facingDirection * player.DashSpeed, 0f);
    }

    public override void Exit()
    {
        player.Rigidbody.gravityScale = cachedGravity;
        player.SetEffectState(PlayerController.PlayerEffectState.None);
    }
}
