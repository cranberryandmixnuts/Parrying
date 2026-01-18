using UnityEngine;

public sealed class DashState : PlayerState
{
    private bool extremeDashSuccess;
    private float timer;
    private float dashDuration;
    private float dashDistance;
    private float dashStartTime;
    private float lastProgress;
    private int dashFacing;

    private RigidbodyConstraints2D cachedConstraints;

    public override PlayerStateType StateType => PlayerStateType.Dash;

    public DashState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        player.CancelJump();
        player.lastDashTime = Time.time;

        cachedConstraints = player.Rigidbody.constraints;
        player.Rigidbody.constraints = cachedConstraints | RigidbodyConstraints2D.FreezePositionY;

        player.Anim.Play("Dash");
        dashDuration = player.GetAnimLength("Dash");
        timer = dashDuration;
        dashDistance = player.Settings.dashDistance;
        dashStartTime = Time.time;
        lastProgress = 0f;
        dashFacing = player.facingDirection;

        if (!player.isGround) player.canAirDash = false;

        player.Vitals.SetInvincibleTimer(timer);

        int f = Time.frameCount;
        int detected = 0;
        int n = player.dashCandidates.Count;
        for (int i = 0; i < n; i++)
        {
            if (player.dashCandidates[i].frame == f) detected++;
            else if (player.dashCandidates[i].frame == f - 1) detected++;
        }

        player.dashCandidates.Clear();

        if (detected > 0 && (Time.time - player.lastExtremeDash) >= player.Settings.extremeDashCooldown)
        {
            extremeDashSuccess = true;
            player.lastExtremeDash = Time.time;

            int gain = 0;
            if (detected >= 1) gain += 50;
            if (detected >= 2) gain += 30;
            if (detected >= 3) gain += 10;

            player.Vitals.GainEnergy(gain);
            player.Effects.DoExtremeDashImpact();
            dashDistance += player.Settings.extremeDashExtraDistance;
        }
        else
            extremeDashSuccess = false;
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
            stateMachine.ChangeState(new LocomotionState(player, stateMachine));
    }

    public override void FixedUpdate()
    {
        float raw = Mathf.Clamp01((Time.time - dashStartTime) / dashDuration);
        float curve = 1f - Mathf.Pow(1f - raw, player.Settings.fadePower);
        float eased = Mathf.Lerp(curve, raw, player.Settings.minLinearBlend);
        if (eased < lastProgress) eased = lastProgress;

        float deltaProgress = eased - lastProgress;
        float deltaX = dashDistance * deltaProgress * dashFacing;
        float vx = deltaX / Time.fixedDeltaTime;

        player.Rigidbody.linearVelocity = new Vector2(vx, 0f);

        lastProgress = eased;
    }

    public override void Exit()
    {
        if (extremeDashSuccess)
        {
            player.canAirDash = true;
            player.lastDashTime = Time.time;
            player.Vitals.SetInvincibleTimer(player.Settings.extremeDashExtraInvincibility);
        }

        player.Rigidbody.constraints = cachedConstraints;
        player.postDashCarryDir = player.facingDirection;
        player.postDashCarryTimer = player.Settings.postDashCarryWindow;
        player.Rigidbody.linearVelocity = Vector2.zero;
    }
}