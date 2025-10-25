using UnityEngine;

public interface IDamageable
{
    void Hit(int damage, Vector2 attackPos);
}