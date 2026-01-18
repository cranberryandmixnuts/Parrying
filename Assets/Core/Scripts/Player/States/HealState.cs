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
    private float enterTimeLeft;
    private float exitTimeLeft;
    private float enterDuration;
    private float exitDuration;

    public override PlayerStateType StateType => PlayerStateType.Heal;

    public HealState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        player.currentSpeedAbs = 0f;
        player.Rigidbody.linearVelocity = Vector2.zero;

        player.Anim.Play("Enter_Heal");
        phase = HealPhase.Enter;
        allowLoop = true;
        healTickTimer = 0f;

        enterDuration = player.GetAnimLength("Enter_Heal");
        enterTimeLeft = enterDuration;

        exitDuration = 0f;
        exitTimeLeft = 0f;

        player.healDelayGauge = 0f;
        player.Effects.PlayHeal();
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

                    if (enterDuration > 0f)
                        player.healDelayGauge = Mathf.Clamp01(1f - (enterTimeLeft / enterDuration));
                    else
                        player.healDelayGauge = 1f;

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
                            player.healDelayGauge = 1f;
                        }
                        else
                        {
                            player.Anim.Play("Exit_Heal");
                            phase = HealPhase.Exit;

                            exitDuration = player.GetAnimLength("Exit_Heal");
                            exitTimeLeft = exitDuration;

                            player.healDelayGauge = 1f;
                        }
                    }

                    break;
                }

            case HealPhase.Loop:
                {
                    player.healDelayGauge = 1f;

                    bool canStay =
                        player.HealHeld &&
                        player.isGround &&
                        player.Vitals.Energy >= player.Settings.healEnergyPerTick &&
                        player.Vitals.Health < player.Vitals.MaxHealth;

                    if (!canStay)
                    {
                        player.Anim.Play("Exit_Heal");
                        phase = HealPhase.Exit;

                        exitDuration = player.GetAnimLength("Exit_Heal");
                        exitTimeLeft = exitDuration;

                        player.healDelayGauge = 1f;
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

                                exitDuration = player.GetAnimLength("Exit_Heal");
                                exitTimeLeft = exitDuration;

                                player.healDelayGauge = 1f;
                                break;
                            }

                            player.Vitals.ApplyHeal(player.Settings.healHealthPerTick);

                            if (player.Vitals.Health >= player.Vitals.MaxHealth)
                            {
                                player.Anim.Play("Exit_Heal");
                                phase = HealPhase.Exit;

                                exitDuration = player.GetAnimLength("Exit_Heal");
                                exitTimeLeft = exitDuration;

                                player.healDelayGauge = 1f;
                                break;
                            }
                        }
                    }

                    break;
                }

            case HealPhase.Exit:
                {
                    if (exitDuration > 0f)
                        player.healDelayGauge = Mathf.Clamp01(exitTimeLeft / exitDuration);
                    else
                        player.healDelayGauge = 0f;

                    exitTimeLeft -= Time.deltaTime;
                    player.Effects.StopHeal();

                    if (exitTimeLeft <= 0f)
                    {
                        stateMachine.ChangeState(new LocomotionState(player, stateMachine));
                    }

                    break;
                }
        }
    }

    public override void Exit()
    {
        player.healDelayGauge = 0f;
        player.Effects.StopHeal();
    }
}