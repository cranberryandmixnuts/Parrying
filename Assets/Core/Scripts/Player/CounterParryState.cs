using UnityEngine;

public sealed class CounterParryState : PlayerState
{
    private float drainTimer;

    public override PlayerStateType StateType => PlayerStateType.CounterParry;

    public CounterParryState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        if (!player.TryConsumeEnergy(player.CounterEnterCost))
            stateMachine.ChangeState(new LocomotionState(player, stateMachine));
        else
        {
            player.EnterCounterParry();
            if (player.isGround) player.Animator.Play("Ground Enhanced Parry");
            else player.Animator.Play("Air Enhanced Parry");
            drainTimer = 0f;
        }
    }

    public override void Update()
    {
        if (TryHandleDash()) return;

        if (!player.ParryHeld)
        {
            stateMachine.ChangeState(new LocomotionState(player, stateMachine));
            return;
        }

        drainTimer += Time.deltaTime;
        float drain = player.CounterDrainPerSecond * Time.deltaTime;
        if (!player.TryConsumeEnergy(drain))
        {
            stateMachine.ChangeState(new LocomotionState(player, stateMachine));
            return;
        }
    }

    public override void Exit()
    {
        player.ExitCounterParry();
    }
}
