using UnityEngine;

public sealed class DieState : PlayerState
{
    public override PlayerStateType StateType => PlayerStateType.Death;

    public DieState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.SetEffectState(PlayerController.PlayerEffectState.Dying);
        player.Animator.Play("Die");
        player.Rigidbody.linearVelocity = Vector2.zero;
        Collider2D col = player.BoxCollider;
        if (col != null) col.enabled = false;
    }
}
