using UnityEngine;
using Sirenix.OdinInspector;

public sealed class PlayerVitals : MonoBehaviour
{
    [TabGroup("Player Vitals", "Runtime"), BoxGroup("Player Vitals/Runtime/Invincible"), ReadOnly, SerializeField]
    private bool IsInvincible = false;

    [TabGroup("Player Vitals", "Runtime"), BoxGroup("Player Vitals/Runtime/Invincible"), ReadOnly, SerializeField]
    private float invincibleTimer = 0f;

    [TabGroup("Player Vitals", "Setup"), BoxGroup("Player Vitals/Setup/Scene References"), SerializeField, Required]
    private PlayerSettings settings;

    [TabGroup("Player Vitals", "Setup"), BoxGroup("Player Vitals/Setup/Scene References"), SerializeField, Required]
    private PlayerController player;

    [TabGroup("Player Vitals", "Runtime"), BoxGroup("Player Vitals/Runtime/State"), ShowInInspector, ReadOnly]
    public int Health
    {
        get => settings.currentHealth;
        private set
        {
            Debug.Log($"Setting player health: {value}");
            settings.currentHealth = Mathf.Clamp(value, 0, settings.maxHealth);
        }
    }

    [TabGroup("Player Vitals", "Runtime"), BoxGroup("Player Vitals/Runtime/State"), ShowInInspector, ReadOnly]
    public int Energy
    {
        get => settings.currentEnergy;
        private set
        {
            Debug.Log($"Setting player energy: {value}");
            settings.currentEnergy = Mathf.Clamp(value, 0, settings.maxEnergy);
        }
    }

    public int MaxHealth => settings.maxHealth;
    public int MaxEnergy => settings.maxEnergy;

    public void InitializePlayerStatus()
    {
        Health = settings.maxHealth;
        Energy = settings.maxEnergy;
    }

    private void Update()
    {
        if (invincibleTimer > 0f)
        {
            IsInvincible = true;
            invincibleTimer -= Time.deltaTime;
        }
        else
        {
            IsInvincible = false;
            invincibleTimer = 0f;
        }
    }

    public bool SetInvincibleTimer(float time)
    {
        if (time <= 0 || invincibleTimer > time)
        {
            Debug.Log("Invalid invincibility time or existing invincibility is longer. No change applied.");
            return false;
        }

        invincibleTimer = time;
        return true;
    }

    public bool ApplyDamage(int damage, bool ignoreInvincible)
    {
        if (!ignoreInvincible && IsInvincible || damage <= 0 || Health <= 0)
        {
            Debug.Log("Player is invincible, damage is non-positive, or player is already dead. No damage applied.");
            return false;
        }

        Health -= damage;

        return true;
    }

    public bool ApplyHeal(int healAmount)
    {
        if (healAmount <= 0 || Health >= settings.maxHealth)
        {
            Debug.Log("Heal amount is non-positive or health is already full. No healing applied.");
            return false;
        }

        Health += healAmount;
        return true;
    }

    public bool TryConsumeEnergy(int amount)
    {
        if (Energy < amount) return false;

        if (amount <= 0)
        {
            Debug.LogWarning("Energy consumption amount is non-positive. No energy consumed.");
            return false;
        }

        Energy -= amount;
        return true;
    }

    public bool GainEnergy(int amount)
    {
        if(amount <= 0 || Energy >= settings.maxEnergy)
        {
            Debug.Log("Energy gain amount is non-positive or energy is already full. No energy gained.");
            return false;
        }

        Energy += amount;
        return true;
    }
}
