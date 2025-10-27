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
        if (player.isGround) player.Animator.Play("Ground Counter Parry"); else player.Animator.Play("Air Counter Parry");
        timer = player.PowerParryDuration;
    }

    public override void Update()
    {
        if (!player.counterParryFirstResolved)
        {
            Projectile proj;
            if (player.TryDetectIncomingAttack(out proj))
            {
                player.counterParryFirstResolved = true;
                player.SetInvincible(true);

                if (proj != null)
                {
                    IParryStack s = proj.Source != null ? proj.Source.GetComponentInParent<IParryStack>() : null;
                    if (s != null) s.AddOrRemove(-1);
                    proj.ReflectToSource();
                }

                GameEffects.Instance.DoCounterParryImpact();
            }
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            stateMachine.ChangeState(new LocomotionState(player, stateMachine));
        }
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