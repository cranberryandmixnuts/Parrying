using UnityEngine;

public sealed class ParryState : PlayerState
{
    private float timer;

    public override PlayerStateType StateType => PlayerStateType.Parry;

    public ParryState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        timer = player.ParryWindow;
        player.EnterParryWindow();
        player.Animator.Play("Parry");
    }

    public override void Update()
    {
        if (TryHandleDash()) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
            stateMachine.ChangeState(new LocomotionState(player, stateMachine));

        if (player.ParryHeld && player.Energy >= player.CounterEnterCost)
            stateMachine.ChangeState(new CounterParryState(player, stateMachine));
    }

    public override void Exit()
    {
        player.ExitParryWindow();
    }
}
