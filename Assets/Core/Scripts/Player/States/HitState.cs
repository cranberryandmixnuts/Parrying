using UnityEngine;
using static PlayerController;

public sealed class HitState : PlayerState
{
    private float timer;

    public override PlayerStateType StateType => PlayerStateType.Hit;

    public HitState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.Vitals.SetInvincibleTimer(player.Settings.hitInvincibleTime);
        player.currentSpeedAbs = 0f;
        player.Rigidbody.linearVelocity = Vector2.zero;
        Vector2 dir = player.lastHitKnockDir.sqrMagnitude > 0f ? player.lastHitKnockDir : Vector2.up;
        player.Rigidbody.AddForce(dir.normalized * player.Settings.knockbackForce, ForceMode2D.Impulse);
        player.Anim.Play("Hit");
        timer = player.GetAnimLength("Hit");
        player.SetEffectState(PlayerController.PlayerEffectState.Hit);
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            player.SetEffectState(PlayerEffectState.None);
            stateMachine.ChangeState(new LocomotionState(player, stateMachine));
        }
    }
}