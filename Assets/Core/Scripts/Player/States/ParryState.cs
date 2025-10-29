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
                player.parryCandidates.Sort((a, b) => a.sqrDistance.CompareTo(b.sqrDistance));
                var c = player.parryCandidates[0];

                float elapsed = Time.time - player.parryWindowStartTime;
                float frac = player.parryWindowDuration > 0f ? elapsed / player.parryWindowDuration : 1f;

                if (frac <= 0.5f)
                {
                    player.GainEnergy(player.PerfectParryEnergyGain);
                    player.parryHadSuccessThisWindow = true;
                    player.SetInvincible(true);
                    if (!player.isGround) player.airParryAvailable = true;
                    GameEffects.Instance.DoPerfectParryImpact();
                    c.attacker?.OnPerfectParry(c.hitPoint);
                }
                else
                {
                    player.GainEnergy(player.ImperfectParryEnergyGain);
                    player.parryHadSuccessThisWindow = true;
                    player.SetInvincible(true);
                    if (!player.isGround) player.airParryAvailable = true;
                    c.attacker?.OnImperfectParry(c.hitPoint);
                }

                player.ClearParryCandidate(c.attacker);
                player.parryCandidates.Clear();
            }
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            stateMachine.ChangeState(new LocomotionState(player, stateMachine));
        }
    }

    public override void Exit()
    {
        player.NotifyParryWindowEnd();
        player.ExitParryWindow();
        player.SetInvincible(false);
    }
}