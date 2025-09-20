using UnityEngine;

public sealed class PlayerHealth : MonoBehaviour, IDamageable
{
    public int MaxHP = 5;
    public int HP;

    private void Awake()
    {
        HP = MaxHP;
    }

    public void Hit(int damage)
    {
        HP -= damage;
        if (HP <= 0) Die();
    }

    private void Die()
    {
        HP = 0;
        //gameObject.SetActive(false);
    }
}
