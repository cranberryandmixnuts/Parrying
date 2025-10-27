using UnityEngine;

public sealed class DashState : PlayerState
{
    private float cachedGravity;
    private float dashMoveSpeed;
    private bool extremeDashSuccess;

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

        if (player.isGround)
            player.Animator.Play("Ground Dash");
        else
            player.Animator.Play("Air Dash");

        if (!player.isGround) player.canAirDash = false;

        player.SetInvincible(true);

        int detectedCount = player.CountIncomingAttacks(true);

        if (detectedCount > 0)
        {
            extremeDashSuccess = true;

            int gain = 0;
            if (detectedCount >= 1) gain += 50;
            if (detectedCount >= 2) gain += 30;
            if (detectedCount >= 3) gain += 10;

            player.GainEnergy(gain);

            dashMoveSpeed = player.DashSpeed * 1.1f;

            GameEffects.Instance.DoExtremeDashImpact();
        }
        else
        {
            extremeDashSuccess = false;
            dashMoveSpeed = player.DashSpeed;
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
        player.Rigidbody.linearVelocity = new Vector2(player.facingDirection * dashMoveSpeed, 0f);
    }

    public override void Exit()
    {
        player.Rigidbody.gravityScale = cachedGravity;
        player.SetInvincible(false);
        player.SetEffectState(PlayerController.PlayerEffectState.None);
        player.postDashCarryDir = player.facingDirection;
        player.postDashCarryTimer = player.PostDashCarryWindow;

        if (extremeDashSuccess)
        {
            player.canAirDash = true;
            player.lastDashTime = Time.time - player.DashCooldown;
        }
    }
}