using UnityEngine;
using static PlayerController;

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

        if (player.isGround)
        {
            player.Anim.Play("Ground Normal Parry");
            timer = player.GetAnimLength("Ground Normal Parry");
        }
        else
        {
            player.Anim.Play("Air Normal Parry");
            timer = player.GetAnimLength("Air Normal Parry");
        }

        player.NotifyParryWindowBegin(timer);
        player.Vitals.SetInvincibleTimer(timer);
        player.parryHadSuccessThisWindow = false;
        player.SetEffectState(PlayerEffectState.Parry);
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
                        player.Vitals.GainEnergy(player.Settings.perfectParryEnergyGain);
                        player.parryHadSuccessThisWindow = true;
                        c.attacker.OnPerfectParry(c.hitPoint);
                    }
                    else
                    {
                        int chip = c.ImperfectParryDamage > 0 ? Mathf.CeilToInt(c.ImperfectParryDamage) : 0;
                        if (chip > 0) player.Vitals.ApplyDamage(chip, true);
                        player.Vitals.GainEnergy(player.Settings.imperfectParryEnergyGain);
                        player.parryHadSuccessThisWindow = true;
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
        player.SetEffectState(PlayerEffectState.None);
        player.NotifyParryWindowEnd();
    }
}