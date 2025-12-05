using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSettings", menuName = "Scriptable Objects/PlayerSettings")]
public sealed class PlayerSettings : ScriptableObject
{
    [Header("Stats")]
    public int maxHealth = 1000;
    public int maxEnergy = 500;

    [Header("Movement")]
    public float moveSpeed = 10f;
    public float groundAccelTime = 0.25f;
    public float airAccelTime = 0.3f;
    public float airReleaseDecelTime = 0.3f;
    [Range(0f, 1f)] public float startSpeedRatio = 0.15f;
    public float postDashCarryWindow = 0.1f;

    [Header("Jump")]
    public AnimationCurve jumpForceCurve;
    public float maxJumpTime = 0.3f;
    public float maxJumpForce = 20f;
    public float coyoteTime = 0.1f;

    [Header("Dash")]
    public float dashDistance = 6f;
    public float dashCooldown = 0.8f;
    public float extremeDashCooldown = 3f;
    public float extremeDashExtraInvincibility = 0.3f;
    public float extremeDashExtraDistance = 1f;
    public float fadePower = 5f;
    public float minLinearBlend = 0.1f;

    [Header("Parry")]
    public int perfectParryEnergyGain = 100;
    public int imperfectParryEnergyGain = 50;
    public float airParryKnockbackForce = 5f;
    public float airParryKnockbackDuration = 0.2f;
    public float airParryKnockbackSlowDuration = 0.3f;
    public float airParryKnockbackSlowScale = 0.5f;
    public float parryExtraInvincibility = 0.1f;

    [Header("Counter Parry")]
    public float counterParryHoldTime = 0.1f;
    public int counterParryEnterCost = 300;
    public float counterParryDrainTick = 0.1f;
    public int counterParryDrainCost = 5;
    public float counterParryNoDrainTime = 0.6f;
    public float counterParryExtraInvincibility = 0.5f;

    [Header("Heal")]
    public float healTickInterval = 0.1f;
    public int healEnergyPerTick = 10;
    public int healHealthPerTick = 10;

    [Header("Hit")]
    public float hitInvincibleTime = 1f;
    public float knockbackForce = 10f;

    [Header("Buffer Time")]
    public float jumpBufferTime = 0.1f;
    public float parryBufferTime = 0.06f;
    public float dashBufferTime = 0.1f;
}