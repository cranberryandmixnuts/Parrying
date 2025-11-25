using UnityEngine;
using static PlayerController;

public sealed class ParryState : PlayerState
{
    private float timer;
    private bool wasAirParry;

    public override PlayerStateType StateType => PlayerStateType.Parry;

    public ParryState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        wasAirParry = !player.isGround;

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
        player.SetEffectState(PlayerEffectState.Parry);
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
                UnityEngine.Object uo = c.attacker as UnityEngine.Object;

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
                        GameEffects.Instance.DoPerfectParryImpact();
                        player.Vitals.GainEnergy(player.Settings.perfectParryEnergyGain);
                        player.parryHadSuccessThisWindow = true;
                        c.attacker.OnPerfectParry(c.hitPoint);
                    }
                    else
                    {
                        int chip = c.ImperfectParryDamage > 0 ? Mathf.CeilToInt(c.ImperfectParryDamage) : 0;
                        if (chip > 0)
                            player.Vitals.ApplyDamage(chip, true);

                        player.Vitals.GainEnergy(player.Settings.imperfectParryEnergyGain);
                        player.parryHadSuccessThisWindow = true;
                        c.attacker.OnImperfectParry(c.hitPoint);
                    }

                    if (!player.isGround)
                        player.airParryAvailable = true;

                    player.ClearParryCandidate(c.attacker);
                    player.parryCandidates.Clear();
                }
            }
        }

        if (player.parryHadSuccessThisWindow && player.ParryPressed)
        {
            player.ConsumeParryPressed();
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
            player.HandleMove(player.Settings.moveSpeed);
            player.HandleJump();

            if (player.isJumping && player.jumpTimeCounter >= player.Settings.maxJumpTime)
                player.isJumping = false;
        }
    }

    public override void Exit()
    {
        if (wasAirParry && !player.isGround)
            player.Anim.Play("Fall");

        player.SetEffectState(PlayerEffectState.None);
        player.NotifyParryWindowEnd();
    }
}