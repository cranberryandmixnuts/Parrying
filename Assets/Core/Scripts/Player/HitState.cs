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
        player.Animator.Play("Hit");
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            player.SetInvincible(false);
            if (player.Health <= 0f)
                stateMachine.ChangeState(new DeathState(player, stateMachine));
            else
                stateMachine.ChangeState(new LocomotionState(player, stateMachine));
        }
    }
}
