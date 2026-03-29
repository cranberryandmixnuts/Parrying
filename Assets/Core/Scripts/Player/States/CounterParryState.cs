using UnityEngine;

public sealed class CounterParryState : PlayerState
{
    private float timer;
    private float cachedGravity;

    public override PlayerStateType StateType => PlayerStateType.CounterParry;

    public CounterParryState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        player.AudioManager.PlayOneShotSFX("카운터 패링 시전", player.gameObject);

        player.CancelJump();
        cachedGravity = player.Rigidbody.gravityScale;
        player.Rigidbody.gravityScale = 0f;
        player.Rigidbody.linearVelocity = Vector2.zero;
        player.currentSpeedAbs = 0f;
        player.counterParryFirstResolved = false;
        if (player.isGround)
        {
            player.Anim.Play("Ground Counter Parry");
            timer = player.GetAnimLength("Ground Counter Parry");
        }
        else
        {
            player.Anim.Play("Air Counter Parry");
            timer = player.GetAnimLength("Air Counter Parry");
        }

        player.Vitals.SetInvincibleTimer(timer);
        player.Effects.PlayCounterParry();
    }

    public override void Update()
    {
        if (!player.counterParryFirstResolved)
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
                    player.AudioManager.PlayOneShotSFX("카운터 패링 성공", player.gameObject);
                    player.counterParryFirstResolved = true;
                    c.attacker.OnCounterParry(c.hitPoint);
                    player.Effects.DoCounterParryImpact();
                    player.Effects.PlayCounterParrySuccess();

                    player.ClearParryCandidate(c.attacker);
                    player.parryCandidates.Clear();
                }
            }
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
            stateMachine.ChangeState(new LocomotionState(player, stateMachine));
    }

    public override void FixedUpdate()
    {
        player.Rigidbody.linearVelocity = Vector2.zero;
    }

    public override void Exit()
    {
        player.Rigidbody.gravityScale = cachedGravity;
        if (player.counterParryFirstResolved)
            player.Vitals.SetInvincibleTimer(player.Settings.counterParryExtraInvincibility);
    }
}