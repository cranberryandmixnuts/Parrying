using UnityEngine;

public sealed class HealState : PlayerState
{
    private enum HealPhase
    {
        Enter,
        Loop,
        Exit
    }

    private HealPhase phase;
    private bool allowLoop;
    private float healTickTimer;

    public override PlayerStateType StateType => PlayerStateType.Heal;

    public HealState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.currentSpeedAbs = 0f;
        player.Rigidbody.linearVelocity = Vector2.zero;

        player.EnterHeal();
        player.Animator.Play("Enter_Heal");
        phase = HealPhase.Enter;
        allowLoop = true;
        healTickTimer = 0f;
    }

    public override void Update()
    {
        if (phase == HealPhase.Enter)
        {
            if (!player.HealHeld)
                allowLoop = false;

            AnimatorStateInfo s = player.Animator.GetCurrentAnimatorStateInfo(0);
            bool doneEnter = !s.IsName("Enter_Heal") || s.normalizedTime >= 0.99f;

            if (doneEnter)
            {
                bool canLoop =
                    allowLoop &&
                    player.HealHeld &&
                    player.isGround &&
                    player.Energy >= player.HealEnergyPerTick &&
                    player.Health < player.MaxHealth;

                if (canLoop)
                {
                    player.Animator.Play("Healing");
                    phase = HealPhase.Loop;
                }
                else
                {
                    player.Animator.Play("Exit_Heal");
                    phase = HealPhase.Exit;
                }
            }
        }
        else if (phase == HealPhase.Loop)
        {
            bool canStay =
                player.HealHeld &&
                player.isGround &&
                player.Energy >= player.HealEnergyPerTick &&
                player.Health < player.MaxHealth;

            if (!canStay)
            {
                player.Animator.Play("Exit_Heal");
                phase = HealPhase.Exit;
            }
            else
            {
                healTickTimer += Time.deltaTime;

                while (healTickTimer >= player.HealTickInterval)
                {
                    healTickTimer -= player.HealTickInterval;

                    bool ok = player.TryConsumeEnergy(player.HealEnergyPerTick);
                    if (!ok)
                    {
                        player.Animator.Play("Exit_Heal");
                        phase = HealPhase.Exit;
                        break;
                    }

                    player.Heal(player.HealHealthPerTick);

                    if (player.Health >= player.MaxHealth)
                    {
                        player.Animator.Play("Exit_Heal");
                        phase = HealPhase.Exit;
                        break;
                    }
                }
            }
        }
        else if (phase == HealPhase.Exit)
        {
            AnimatorStateInfo s2 = player.Animator.GetCurrentAnimatorStateInfo(0);
            bool doneExit = !s2.IsName("Exit_Heal") || s2.normalizedTime >= 0.99f;
            if (doneExit)
            {
                stateMachine.ChangeState(new LocomotionState(player, stateMachine));
            }
        }
    }

    public override void Exit()
    {
        player.ExitHeal();
        if (player.HealEndLag > 0f)
            player.StartHealEndLag();
    }
}