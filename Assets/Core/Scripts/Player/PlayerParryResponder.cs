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
                if (s != null) s.AddOrRemove(-1);
                return ProjectileHitResponse.ReflectToSource;
            }
            return ProjectileHitResponse.IgnoreContinue;
        }

        if (p.CurrentStateType == PlayerStateType.Parry)
        {
            if (p.parryHadSuccessThisWindow) return ProjectileHitResponse.IgnoreContinue;

            float elapsed = Time.time - p.parryWindowStartTime;
            float frac = p.parryWindowDuration > 0f ? elapsed / p.parryWindowDuration : 1f;

            if (frac <= 0.5f)
            {
                p.GainEnergy(p.ParryEnergyGain);
                p.parryHadSuccessThisWindow = true;
                p.ImmediateReParry();
                return ProjectileHitResponse.NeutralizeContinue;
            }
            else
            {
                float d = Mathf.Max(0f, projectile.Damage) * 0.5f;
                p.ApplyRawDamage(d);
                p.GainEnergy(p.ParryEnergyGain * 0.5f);
                p.parryHadSuccessThisWindow = true;
                p.ImmediateReParry();
                return ProjectileHitResponse.Consume;
            }
        }

        if (p.IsParryGraceActive) return ProjectileHitResponse.IgnoreContinue;

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
                if (s != null) s.AddOrRemove(-1);
                return MeleeHitResult.Ignore;
            }
            return MeleeHitResult.Ignore;
        }

        if (p.CurrentStateType == PlayerStateType.Parry)
        {
            if (p.parryHadSuccessThisWindow) return MeleeHitResult.Ignore;

            float elapsed = Time.time - p.parryWindowStartTime;
            float frac = p.parryWindowDuration > 0f ? elapsed / p.parryWindowDuration : 1f;

            if (frac <= 0.5f)
            {
                p.GainEnergy(p.ParryEnergyGain);
                p.parryHadSuccessThisWindow = true;
                p.ImmediateReParry();
                return MeleeHitResult.Ignore;
            }
            else
            {
                float d = Mathf.Max(0, damage) * 0.5f;
                p.ApplyRawDamage(d);
                p.GainEnergy(p.ParryEnergyGain * 0.5f);
                p.parryHadSuccessThisWindow = true;
                p.ImmediateReParry();
                return MeleeHitResult.Ignore;
            }
        }

        if (p.IsParryGraceActive) return MeleeHitResult.Ignore;

        return MeleeHitResult.Damage;
    }
}