using UnityEngine;

public sealed class DeathState : PlayerState
{
    private float timer;

    public override PlayerStateType StateType => PlayerStateType.Death;

    public DeathState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        player.Anim.Play("Death");
        timer = player.GetAnimLength("Death");
        player.Rigidbody.linearVelocity = Vector2.zero;
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f) player.Die();
    }
}