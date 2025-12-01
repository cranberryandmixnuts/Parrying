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

    public HealState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

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
        player.Healing.Play();
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
                            player.Vitals.Energy >= player.Settings.healEnergyPerTick &&
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
                        player.Vitals.Energy >= player.Settings.healEnergyPerTick &&
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

                        while (healTickTimer >= player.Settings.healTickInterval)
                        {
                            healTickTimer -= player.Settings.healTickInterval;

                            bool ok = player.Vitals.TryConsumeEnergy(player.Settings.healEnergyPerTick);
                            if (!ok)
                            {
                                player.Anim.Play("Exit_Heal");
                                phase = HealPhase.Exit;
                                exitTimeLeft = player.GetAnimLength("Exit_Heal");
                                break;
                            }

                            player.Vitals.ApplyHeal(player.Settings.healHealthPerTick);

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
                    player.Healing.Stop();

                    if (exitTimeLeft <= 0f)
                    {
                        player.SetEffectState(PlayerEffectState.None);
                        stateMachine.ChangeState(new LocomotionState(player, stateMachine));
                    }

                    break;
                }
        }
    }
}