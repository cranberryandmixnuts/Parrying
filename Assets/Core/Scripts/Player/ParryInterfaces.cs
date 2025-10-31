using UnityEngine;

public enum ProjectileHitResponse
{
    Consume,
    IgnoreContinue,
    NeutralizeContinue,
    ReflectToSource,
    ConsumedAlready
}

public interface IProjectileResponder
{
    ProjectileHitResponse OnProjectileHit(Projectile projectile, Collider2D myCollider);
}

public enum MeleeHitResult
{
    Damage,
    Ignore
}

public interface IParryStack
{
    void AddOrRemove(int delta);
}