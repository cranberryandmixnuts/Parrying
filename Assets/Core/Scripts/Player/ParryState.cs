using UnityEngine;

public sealed class ParryState : PlayerState
{
    private float timer;

    public override PlayerStateType StateType => PlayerStateType.Parry;

    public ParryState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.CancelJump(true);
        if (!player.isGround) player.airParryAvailable = false;
        if (player.isGround)
        {
            player.currentSpeedAbs = 0f;
            player.Rigidbody.linearVelocity = new Vector2(0f, player.Rigidbody.linearVelocity.y);
        }

        timer = player.ParryWindow;
        player.NotifyParryWindowBegin(timer);
        player.EnterParryWindow();
        if (player.isGround) player.Animator.Play("Ground Normal Parry"); else player.Animator.Play("Air Normal Parry");
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            if (player.isGround && player.HasParryBuffer())
            {
                player.ConsumeParryPressed();
                player.ConsumeParryBuffer();
                stateMachine.ChangeState(new ParryState(player, stateMachine));
            }
            else
            {
                stateMachine.ChangeState(new LocomotionState(player, stateMachine));
            }
        }
    }

    public override void Exit()
    {
        player.NotifyParryWindowEnd();
        player.ExitParryWindow();
    }
}