using UnityEngine;

public sealed class DeathState : PlayerState
{
    public override PlayerStateType StateType => PlayerStateType.Death;

    public DeathState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.SetEffectState(PlayerController.PlayerEffectState.Death);
        player.Anim.Play("Death");
        player.Rigidbody.linearVelocity = Vector2.zero;
        Collider2D col = player.BoxCollider;
        if (col != null) col.enabled = false;
    }
}