using UnityEngine;

public sealed class HitState : PlayerState
{
    private float timer;


    public override PlayerStateType StateType => PlayerStateType.Hit;

    private readonly bool KnockDirIsRight;

    public HitState(PlayerController player, PlayerStateMachine stateMachine, bool KnockDirIsRight)
        : base(player, stateMachine) { this.KnockDirIsRight = KnockDirIsRight; }

    public override void Enter()
    {
        AudioManager.Instance.PlayOneShotSFX("플레이어 피격", player.gameObject);

        player.Vitals.SetInvincibleTimer(player.Settings.hitInvincibleTime);
        player.currentSpeedAbs = 0f;
        player.Rigidbody.linearVelocity = Vector2.zero;
        Vector2 KnockDir = KnockDirIsRight ? new Vector2(1f, 1f) : new Vector2(-1f, 1f);
        player.Rigidbody.AddForce(KnockDir.normalized * player.Settings.knockbackForce, ForceMode2D.Impulse);
        player.Anim.Play("Hit");
        timer = player.GetAnimLength("Hit");
        player.Effects.PlayHit();
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            stateMachine.ChangeState(new LocomotionState(player, stateMachine));
        }
    }
}