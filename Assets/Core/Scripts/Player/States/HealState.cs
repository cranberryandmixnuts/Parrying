using UnityEngine;
using static PlayerController;

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
    private float enterTimeLeft;
    private float exitTimeLeft;

    public override PlayerStateType StateType => PlayerStateType.Heal;

    public HealState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.currentSpeedAbs = 0f;
        player.Rigidbody.linearVelocity = Vector2.zero;

        player.SetEffectState(PlayerEffectState.Heal);
        player.Anim.Play("Enter_Heal");
        phase = HealPhase.Enter;
        allowLoop = true;
        healTickTimer = 0f;
        enterTimeLeft = player.GetAnimLength("Enter_Heal");
        exitTimeLeft = 0f;
    }

    public override void Update()
    {
        switch (phase)
        {
            case HealPhase.Enter:
                {
                    if (!player.HealHeld)
                        allowLoop = false;

                    enterTimeLeft -= Time.deltaTime;

                    if (enterTimeLeft <= 0f)
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
                            exitTimeLeft = player.GetAnimLength("Exit_Heal");
                        }
                    }

                    break;
                }

            case HealPhase.Loop:
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
                        exitTimeLeft = player.GetAnimLength("Exit_Heal");
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
                                exitTimeLeft = player.GetAnimLength("Exit_Heal");
                                break;
                            }

                            player.Vitals.ApplyHeal(player.HealHealthPerTick);

                            if (player.Vitals.Health >= player.Vitals.MaxHealth)
                            {
                                player.Anim.Play("Exit_Heal");
                                phase = HealPhase.Exit;
                                exitTimeLeft = player.GetAnimLength("Exit_Heal");
                                break;
                            }
                        }
                    }

                    break;
                }

            case HealPhase.Exit:
                {
                    exitTimeLeft -= Time.deltaTime;

                    if (exitTimeLeft <= 0f)
                        stateMachine.ChangeState(new LocomotionState(player, stateMachine));

                    break;
                }
        }
    }
}