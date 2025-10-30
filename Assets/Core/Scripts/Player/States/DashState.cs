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

        int f = Time.frameCount;
        int detected = 0;
        int n = player.dashCandidates.Count;
        for (int i = 0; i < n; i++)
        {
            if (player.dashCandidates[i].frame == f) detected++;
            else if (player.dashCandidates[i].frame == f - 1) detected++;
        }

        player.dashCandidates.Clear();

        if (detected > 0)
        {
            extremeDashSuccess = true;

            int gain = 0;
            if (detected >= 1) gain += 50;
            if (detected >= 2) gain += 30;
            if (detected >= 3) gain += 10;

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