using UnityEngine;
using Sirenix.OdinInspector;

public sealed class PlayerVitals : Singleton<PlayerVitals, GlobalScope>
{
    [TabGroup("Player Vitals", "Setup"), BoxGroup("Player Vitals/Setup/Scene References"), SerializeField, Required]
    private PlayerSettings settings;

    [TabGroup("Player Vitals", "Runtime"), BoxGroup("Player Vitals/Runtime/Status"), SuffixLabel("HP", true), MinValue(0), MaxValue("@settings.maxHealth")]
    public int currentHealth = 1000;

    [TabGroup("Player Vitals", "Runtime"), BoxGroup("Player Vitals/Runtime/Status"), SuffixLabel("EN", true), MinValue(0), MaxValue("@settings.maxEnergy")]
    public int currentEnergy = 500;

    [TabGroup("Player Vitals", "Runtime"), BoxGroup("Player Vitals/Runtime/Invincible"), ReadOnly, SerializeField]
    private bool IsInvincible = false;

    [TabGroup("Player Vitals", "Runtime"), BoxGroup("Player Vitals/Runtime/Invincible"), SerializeField]
    private float invincibleTimer = 0f;

    [TabGroup("Player Vitals", "Runtime"), BoxGroup("Player Vitals/Runtime/State"), ShowInInspector, ReadOnly]
    public int Health
    {
        get => currentHealth;
        private set
        {
            Debug.Log($"Setting player health: {value}");
            currentHealth = Mathf.Clamp(value, 0, settings.maxHealth);
        }
    }

    [TabGroup("Player Vitals", "Runtime"), BoxGroup("Player Vitals/Runtime/State"), ShowInInspector, ReadOnly]
    public int Energy
    {
        get => currentEnergy;
        private set
        {
            Debug.Log($"Setting player energy: {value}");
            currentEnergy = Mathf.Clamp(value, 0, settings.maxEnergy);
        }
    }

    private SceneType savedcurrentScene = SceneType.None;
    private int currentSceneEntranceHealth = -1;
    private int currentSceneEntranceEnergy = -1;

    public int MaxHealth => settings.maxHealth;
    public int MaxEnergy => settings.maxEnergy;

    private void OnEnable()
    {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.TransitionCompleted += SaveAndLoadSceneStatus;
    }

    private void Start()
    {
        InitializePlayerStatus();
        SceneLoader.Instance.TransitionCompleted += SaveAndLoadSceneStatus;
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

    private void OnDisable()
    {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.TransitionCompleted -= SaveAndLoadSceneStatus;
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

        if (ignoreInvincible && Health <= damage)
            Health = 1;
        else
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

    private void SaveAndLoadSceneStatus(SceneType CurrentSceneType)
    {
        if (savedcurrentScene == CurrentSceneType)
        {
            if (currentSceneEntranceHealth >= 0)
                Health = currentSceneEntranceHealth;

            if (currentSceneEntranceEnergy >= 0)
                Energy = currentSceneEntranceEnergy;
        }

        savedcurrentScene = CurrentSceneType;
        currentSceneEntranceHealth = Health;
        currentSceneEntranceEnergy = Energy;
    }

    [Button]
    public void InitializePlayerStatus()
    {
        Health = settings.maxHealth;
        Energy = settings.maxEnergy;

        savedcurrentScene = SceneType.None;
        currentSceneEntranceHealth = -1;
        currentSceneEntranceEnergy = -1;
    }
}
