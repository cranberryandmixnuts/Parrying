using UnityEngine;

public sealed class HitState : PlayerState
{
    private float timer;

    public override PlayerStateType StateType => PlayerStateType.Hit;

    public HitState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        timer = player.HitStunDuration;
        player.SetInvincible(true);
        player.currentSpeedAbs = 0f;
        player.Rigidbody.linearVelocity = Vector2.zero;
        Vector2 dir = player.lastHitKnockDir.sqrMagnitude > 0f ? player.lastHitKnockDir : Vector2.up;
        player.Rigidbody.AddForce(dir.normalized * player.KnockbackForce, ForceMode2D.Impulse);
        player.Animator.Play("Hit");
        player.SetEffectState(PlayerController.PlayerEffectState.Hit);
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            player.SetInvincible(false);
            if (player.Health <= 0)
                stateMachine.ChangeState(new DeathState(player, stateMachine));
            else
                stateMachine.ChangeState(new LocomotionState(player, stateMachine));
        }
    }
}