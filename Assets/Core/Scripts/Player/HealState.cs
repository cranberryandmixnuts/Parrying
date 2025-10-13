using UnityEngine;

public sealed class HealState : PlayerState
{
    public override PlayerStateType StateType => PlayerStateType.Heal;

    public HealState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.EnterHeal();
        player.Animator.Play("Heal");
    }

    public override void Update()
    {
        if (!player.isGround || !player.HealHeld || player.Energy <= 0f || player.Health >= player.MaxHealth)
        {
            stateMachine.ChangeState(new LocomotionState(player, stateMachine));
            return;
        }

        float e = player.HealEnergyPerSecond * Time.deltaTime;
        float h = player.HealHealthPerSecond * Time.deltaTime;

        if (player.TryConsumeEnergy(e))
            player.Heal(h);
        else
            stateMachine.ChangeState(new LocomotionState(player, stateMachine));
    }

    public override void Exit()
    {
        player.ExitHeal();
        if (player.HealEndLag > 0f)
            player.StartHealEndLag();
    }
}
