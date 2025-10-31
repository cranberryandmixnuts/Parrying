using UnityEngine;

public sealed class ParryState : PlayerState
{
    private float timer;

    public override PlayerStateType StateType => PlayerStateType.Parry;

    public ParryState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.CancelJump(true);

        if (!player.isGround)
            player.airParryAvailable = false;

        if (player.isGround)
        {
            player.currentSpeedAbs = 0f;
            player.Rigidbody.linearVelocity = new Vector2(0f, player.Rigidbody.linearVelocity.y);
        }

        timer = player.ParryWindow;
        player.NotifyParryWindowBegin(timer);
        player.parryHadSuccessThisWindow = false;
        player.SetInvincible(false);
        player.EnterParryWindow();

        if (player.isGround)
            player.Animator.Play("Ground Normal Parry");
        else
            player.Animator.Play("Air Normal Parry");
    }

    public override void Update()
    {
        if (!player.parryHadSuccessThisWindow)
        {
            if (player.parryCandidates != null && player.parryCandidates.Count > 0)
            {
                var c = player.parryCandidates[0];
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
                        player.GainEnergy(player.PerfectParryEnergyGain);
                        player.parryHadSuccessThisWindow = true;
                        player.SetInvincible(true);
                        c.attacker.OnPerfectParry(c.hitPoint);
                    }
                    else
                    {
                        int chip = c.ImperfectParryDamage > 0 ? Mathf.CeilToInt(c.ImperfectParryDamage) : 0;
                        if (chip > 0) player.ApplyChipDamageNoHit(chip);
                        player.GainEnergy(player.ImperfectParryEnergyGain);
                        player.parryHadSuccessThisWindow = true;
                        player.SetInvincible(true);
                        c.attacker.OnImperfectParry(c.hitPoint);
                    }

                    if (!player.isGround) player.airParryAvailable = true;

                    player.ClearParryCandidate(c.attacker);
                    player.parryCandidates.Clear();
                }
            }
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
            stateMachine.ChangeState(new LocomotionState(player, stateMachine));
    }

    public override void Exit()
    {
        player.NotifyParryWindowEnd();
        player.ExitParryWindow();
        player.SetInvincible(false);
    }
}