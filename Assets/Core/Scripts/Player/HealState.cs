using UnityEngine;

public sealed class HealState : PlayerState
{
    private bool enteredLoop;

    public override PlayerStateType StateType => PlayerStateType.Heal;

    public HealState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        enteredLoop = false;
        player.EnterHeal();
        player.Animator.Play("Enter_Heal");
    }

    public override void Update()
    {
        if (!enteredLoop)
        {
            var s = player.Animator.GetCurrentAnimatorStateInfo(0);
            if (!s.IsName("Enter_Heal") || s.normalizedTime >= 0.95f)
            {
                player.Animator.Play("Healing");
                enteredLoop = true;
            }
        }

        if (!player.isGround || player.Energy <= 0f || player.Health >= player.MaxHealth || !player.HealHeld)
        {
            if (!player.HealHeld) player.Animator.Play("Exit_Heal");
            stateMachine.ChangeState(new LocomotionState(player, stateMachine));
            return;
        }

        float e = player.HealEnergyPerSecond * Time.deltaTime;
        float h = player.HealHealthPerSecond * Time.deltaTime;

        if (player.TryConsumeEnergy(e)) player.Heal(h); else stateMachine.ChangeState(new LocomotionState(player, stateMachine));
    }

    public override void Exit()
    {
        player.ExitHeal();
        if (player.HealEndLag > 0f) player.StartHealEndLag();
    }
}