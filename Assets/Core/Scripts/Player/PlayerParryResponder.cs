using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public sealed class PlayerParryResponder : MonoBehaviour, IProjectileResponder, IMeleeHitResponder
{
    private PlayerController p;

    private void Awake()
    {
        p = GetComponent<PlayerController>();
    }

    public ProjectileHitResponse OnProjectileHit(Projectile projectile, Collider2D myCollider)
    {
        if (p.CurrentStateType == PlayerStateType.CounterParry)
        {
            if (!p.counterParryFirstResolved)
            {
                p.counterParryFirstResolved = true;

                IParryStack s = projectile.Source != null ? projectile.Source.GetComponentInParent<IParryStack>() : null;
                if (s != null)
                    s.AddOrRemove(-1);

                p.SetInvincible(true);

                GameEffects.Instance.DoCounterParryImpact();

                return ProjectileHitResponse.ReflectToSource;
            }

            return ProjectileHitResponse.IgnoreContinue;
        }

        if (p.CurrentStateType == PlayerStateType.Parry)
        {
            if (!p.parryHadSuccessThisWindow)
            {
                float elapsed = Time.time - p.parryWindowStartTime;
                float frac = p.parryWindowDuration > 0f ? elapsed / p.parryWindowDuration : 1f;

                if (frac <= 0.5f)
                {
                    p.GainEnergy(p.PerfectParryEnergyGain);
                    p.parryHadSuccessThisWindow = true;
                    p.SetInvincible(true);

                    if (!p.isGround)
                        p.airParryAvailable = true;

                    GameEffects.Instance.DoPerfectParryImpact();

                    return ProjectileHitResponse.NeutralizeContinue;
                }
                else
                {
                    int chip = Mathf.CeilToInt(projectile.Damage * 0.5f);
                    if (chip > 0)
                        p.ApplyChipDamageNoHit(chip);

                    p.GainEnergy(p.ImperfectParryEnergyGain);
                    p.parryHadSuccessThisWindow = true;
                    p.SetInvincible(true);

                    if (!p.isGround)
                        p.airParryAvailable = true;

                    return ProjectileHitResponse.ConsumedAlready;
                }
            }

            return ProjectileHitResponse.IgnoreContinue;
        }

        if (p.IsParryGraceActive || p.CurrentStateType == PlayerStateType.Dash)
            return ProjectileHitResponse.IgnoreContinue;

        return ProjectileHitResponse.Consume;
    }

    public MeleeHitResult OnMeleeHit(MeleeSweepEmitter emitter, Collider2D myCollider, int damage)
    {
        if (p.CurrentStateType == PlayerStateType.CounterParry)
        {
            if (!p.counterParryFirstResolved)
            {
                p.counterParryFirstResolved = true;

                IParryStack s = emitter != null ? emitter.GetComponentInParent<IParryStack>() : null;
                if (s != null)
                    s.AddOrRemove(-1);

                p.SetInvincible(true);

                GameEffects.Instance.DoCounterParryImpact();

                return MeleeHitResult.Ignore;
            }

            return MeleeHitResult.Ignore;
        }

        if (p.CurrentStateType == PlayerStateType.Parry)
        {
            if (!p.parryHadSuccessThisWindow)
            {
                float elapsed = Time.time - p.parryWindowStartTime;
                float frac = p.parryWindowDuration > 0f ? elapsed / p.parryWindowDuration : 1f;

                if (frac <= 0.5f)
                {
                    p.GainEnergy(p.PerfectParryEnergyGain);
                    p.parryHadSuccessThisWindow = true;
                    p.SetInvincible(true);

                    if (!p.isGround)
                        p.airParryAvailable = true;

                    GameEffects.Instance.DoPerfectParryImpact();

                    return MeleeHitResult.Ignore;
                }
                else
                {
                    int chip = Mathf.CeilToInt(damage * 0.5f);
                    if (chip > 0)
                        p.ApplyChipDamageNoHit(chip);

                    p.GainEnergy(p.ImperfectParryEnergyGain);
                    p.parryHadSuccessThisWindow = true;
                    p.SetInvincible(true);

                    if (!p.isGround)
                        p.airParryAvailable = true;

                    return MeleeHitResult.Ignore;
                }
            }

            return MeleeHitResult.Ignore;
        }

        if (p.IsParryGraceActive || p.CurrentStateType == PlayerStateType.Dash)
            return MeleeHitResult.Ignore;

        return MeleeHitResult.Damage;
    }
}