using UnityEngine;

public sealed class ParryState : PlayerState
{
    private float timer;

    public override PlayerStateType StateType => PlayerStateType.Parry;

    public ParryState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.CancelJump(true);
        if (!player.isGround) player.airParryAvailable = false;
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
        if (player.isGround) player.Animator.Play("Ground Normal Parry"); else player.Animator.Play("Air Normal Parry");
    }

    public override void Update()
    {
        if (!player.parryHadSuccessThisWindow)
        {
            Projectile proj;
            if (player.TryDetectIncomingAttack(out proj))
            {
                float elapsed = Time.time - player.parryWindowStartTime;
                float frac = player.parryWindowDuration > 0f ? elapsed / player.parryWindowDuration : 1f;

                if (frac <= 0.6f)
                {
                    player.GainEnergy(player.PerfectParryEnergyGain);
                    player.parryHadSuccessThisWindow = true;
                    player.SetInvincible(true);
                    if (proj != null) proj.Neutralize();

                    GameEffects.Instance.DoPerfectParryImpact();
                }
                else
                {
                    int chip = proj != null ? Mathf.CeilToInt(proj.Damage * 0.5f) : 0;
                    if (chip > 0) player.ApplyChipDamageNoHit(chip);
                    player.GainEnergy(player.ImperfectParryEnergyGain);
                    player.parryHadSuccessThisWindow = true;
                    player.SetInvincible(true);
                    if (proj != null) proj.ConsumeAndDestroy();

                    GameEffects.Instance.DoImperfectParryImpact();
                }
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