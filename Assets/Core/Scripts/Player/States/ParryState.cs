using UnityEngine;

public sealed class ParryState : PlayerState
{
    private const float AirParryKnockbackAngle = 60f;

    private float timer;
    private bool wasAirParry;
    private float airParryKnockbackTimer;
    private float airParryKnockbackSlowTimer;
    private float airParryKnockbackHorizontalSign;

    public override PlayerStateType StateType => PlayerStateType.Parry;

    public ParryState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        wasAirParry = !player.isGround;
        airParryKnockbackTimer = 0f;
        airParryKnockbackSlowTimer = 0f;
        airParryKnockbackHorizontalSign = 0f;

        if (!player.isGround)
            player.airParryAvailable = false;

        if (player.isGround)
        {
            player.currentSpeedAbs = 0f;
            player.Rigidbody.linearVelocity = new Vector2(0f, player.Rigidbody.linearVelocity.y);
        }

        if (player.isGround)
        {
            player.Anim.Play("Ground Normal Parry", -1, 0f);
            timer = player.GetAnimLength("Ground Normal Parry");
        }
        else
        {
            player.Anim.Play("Air Normal Parry", -1, 0f);
            timer = player.GetAnimLength("Air Normal Parry");
        }

        player.NotifyParryWindowBegin(timer);
        player.Vitals.SetInvincibleTimer(timer);
        player.parryHadSuccessThisWindow = false;
    }

    public override void Update()
    {
        if (wasAirParry && player.isGround)
        {
            stateMachine.ChangeState(new LocomotionState(player, stateMachine));
            return;
        }

        if (!player.JumpHeld && player.isJumping)
            player.StopRising();

        if (!player.parryHadSuccessThisWindow)
        {
            if (player.parryCandidates.Count > 0)
            {
                ParryCandidate c = player.parryCandidates[0];
                Object uo = c.attacker as Object;

                if (uo == null)
                {
                    player.ClearParryCandidate(c.attacker);
                    player.parryCandidates.Clear();
                }
                else
                {
                    float elapsed = Time.time - player.parryWindowStartTime;
                    float frac = player.parryWindowDuration > 0f ? elapsed / player.parryWindowDuration : 1f;

                    if (frac <= 0.5f)
                    {
                        player.Effects.DoPerfectParryImpact();
                        player.Effects.PlayParry();
                        player.Vitals.GainEnergy(player.Settings.perfectParryEnergyGain);
                        player.parryHadSuccessThisWindow = true;
                        c.attacker.OnPerfectParry(c.hitPoint);
                    }
                    else
                    {
                        player.Vitals.ApplyDamage(c.ImperfectParryDamage, true);
                        player.Vitals.GainEnergy(player.Settings.imperfectParryEnergyGain);
                        player.parryHadSuccessThisWindow = true;
                        c.attacker.OnImperfectParry(c.hitPoint);
                    }

                    if (!player.isGround)
                    {
                        ApplyAirParryKnockback(c);
                        player.airParryAvailable = true;
                    }

                    player.ClearParryCandidate(c.attacker);
                    player.parryCandidates.Clear();
                }
            }
        }

        if (player.parryHadSuccessThisWindow && player.HasParryBuffer)
        {
            player.ConsumeParryBuffer();
            stateMachine.ChangeState(new ParryState(player, stateMachine));
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
            stateMachine.ChangeState(new LocomotionState(player, stateMachine));
    }

    public override void FixedUpdate()
    {
        if (!player.isGround)
        {
            if (airParryKnockbackTimer > 0f)
            {
                airParryKnockbackTimer -= Time.fixedDeltaTime;
            }
            else
            {
                float moveSpeed = player.Settings.moveSpeed;

                if (airParryKnockbackSlowTimer > 0f)
                {
                    airParryKnockbackSlowTimer -= Time.fixedDeltaTime;

                    if (IsMovingAgainstAirParryKnockback())
                        moveSpeed *= player.Settings.airParryKnockbackSlowScale;
                }

                player.HandleMove(moveSpeed);
            }

            player.HandleJump();

            if (player.isJumping && player.jumpTimeCounter >= player.Settings.maxJumpTime)
                player.isJumping = false;
        }
    }

    public override void Exit()
    {
        if (wasAirParry && !player.isGround)
            player.Anim.Play("Fall");

        if (player.parryHadSuccessThisWindow)
            player.Vitals.SetInvincibleTimer(player.Settings.parryExtraInvincibility);

        player.NotifyParryWindowEnd();
    }

    private void ApplyAirParryKnockback(ParryCandidate c)
    {
        Vector2 playerPos = player.transform.position;
        Vector2 hitPoint = c.hitPoint;

        float horizontalSign = playerPos.x >= hitPoint.x ? 1f : -1f;
        float rad = AirParryKnockbackAngle * Mathf.Deg2Rad;

        player.CancelJump();
        player.Rigidbody.linearVelocity = Vector2.zero;

        Vector2 dir = new Vector2(horizontalSign * Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
        player.Rigidbody.AddForce(dir * player.Settings.airParryKnockbackForce, ForceMode2D.Impulse);

        airParryKnockbackTimer = player.Settings.airParryKnockbackDuration;
        airParryKnockbackSlowTimer = player.Settings.airParryKnockbackSlowDuration;
        airParryKnockbackHorizontalSign = Mathf.Sign(dir.x);
    }

    private bool IsMovingAgainstAirParryKnockback()
    {
        if (airParryKnockbackHorizontalSign == 0f)
            return false;

        float vx = player.Rigidbody.linearVelocity.x;
        if (Mathf.Approximately(vx, 0f))
            return false;

        float currentSign = Mathf.Sign(vx);
        float knockbackSign = Mathf.Sign(airParryKnockbackHorizontalSign);

        if (currentSign == 0f)
            return false;

        return currentSign == -knockbackSign;
    }
}