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
        player.SetInvincible(true);
        player.EnterCounterParry();
        if (player.isGround) player.Animator.Play("Ground Counter Parry"); else player.Animator.Play("Air Counter Parry");
        timer = player.PowerParryDuration;
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            player.SetInvincible(false);
            player.ExitCounterParry();
            player.Rigidbody.gravityScale = cachedGravity;
            player.SetEffectState(PlayerController.PlayerEffectState.None);
            stateMachine.ChangeState(new LocomotionState(player, stateMachine));
        }
    }

    public override void FixedUpdate()
    {
        player.Rigidbody.linearVelocity = Vector2.zero;
    }
}