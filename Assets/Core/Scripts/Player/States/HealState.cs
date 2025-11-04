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
        player.Anim.Play("Enter_Heal");
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

            AnimatorStateInfo s = player.Anim.GetCurrentAnimatorStateInfo(0);
            bool doneEnter = !s.IsName("Enter_Heal") || s.normalizedTime >= 0.99f;

            if (doneEnter)
            {
                bool canLoop =
                    allowLoop &&
                    player.HealHeld &&
                    player.isGround &&
                    player.Vitals.Energy >= player.HealEnergyPerTick &&
                    player.Vitals.Health < player.Vitals.MaxHealth;

                if (canLoop)
                {
                    player.Anim.Play("Healing");
                    phase = HealPhase.Loop;
                }
                else
                {
                    player.Anim.Play("Exit_Heal");
                    phase = HealPhase.Exit;
                }
            }
        }
        else if (phase == HealPhase.Loop)
        {
            bool canStay =
                player.HealHeld &&
                player.isGround &&
                player.Vitals.Energy >= player.HealEnergyPerTick &&
                player.Vitals.Health < player.Vitals.MaxHealth;

            if (!canStay)
            {
                player.Anim.Play("Exit_Heal");
                phase = HealPhase.Exit;
            }
            else
            {
                healTickTimer += Time.deltaTime;

                while (healTickTimer >= player.HealTickInterval)
                {
                    healTickTimer -= player.HealTickInterval;

                    bool ok = player.Vitals.TryConsumeEnergy(player.HealEnergyPerTick);
                    if (!ok)
                    {
                        player.Anim.Play("Exit_Heal");
                        phase = HealPhase.Exit;
                        break;
                    }

                    player.Vitals.ApplyHeal(player.HealHealthPerTick);

                    if (player.Vitals.Health >= player.Vitals.MaxHealth)
                    {
                        player.Anim.Play("Exit_Heal");
                        phase = HealPhase.Exit;
                        break;
                    }
                }
            }
        }
        else if (phase == HealPhase.Exit)
        {
            AnimatorStateInfo s2 = player.Anim.GetCurrentAnimatorStateInfo(0);
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