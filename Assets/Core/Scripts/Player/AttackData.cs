using UnityEngine;

public struct AttackData
{
    public float Damage;
    public Vector2 Direction;
    public bool Parryable;
    public bool Projectile;

    public AttackData(float damage, Vector2 direction, bool parryable, bool projectile)
    {
        Damage = damage;
        Direction = direction;
        Parryable = parryable;
        Projectile = projectile;
    }
}
