using UnityEngine;

public sealed class CounterParryState : PlayerState
{
    private float timer;
    private float cachedGravity;

    public override PlayerStateType StateType => PlayerStateType.CounterParry;

    public CounterParryState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.CancelJump(true);
        cachedGravity = player.Rigidbody.gravityScale;
        player.Rigidbody.gravityScale = 0f;
        player.Rigidbody.linearVelocity = Vector2.zero;
        player.currentSpeedAbs = 0f;
        player.EnterCounterParry();
        player.counterParryFirstResolved = false;
        player.SetInvincible(false);
        if (player.isGround) player.Anim.Play("Ground Counter Parry"); else player.Anim.Play("Air Counter Parry");
        timer = player.PowerParryDuration;
    }

    public override void Update()
    {
        if (!player.counterParryFirstResolved)
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
                    player.counterParryFirstResolved = true;
                    player.SetInvincible(true);
                    c.attacker.OnCounterParry(c.hitPoint);
                    GameEffects.Instance.DoCounterParryImpact();

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
        player.SetEffectState(PlayerController.PlayerEffectState.None);
        if (player.counterParryFirstResolved) player.AddParryGrace(0.3f);
        player.ExitCounterParry();
        player.SetInvincible(false);
    }
}