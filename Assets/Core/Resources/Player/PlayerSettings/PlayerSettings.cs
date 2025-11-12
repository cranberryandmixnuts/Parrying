using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSettings", menuName = "Scriptable Objects/PlayerSettings")]
public sealed class PlayerSettings : ScriptableObject
{
    [Header("Stats")]
    public int maxHealth = 1000;
    public int maxEnergy = 500;

    [Header("Movement")]
    public float moveSpeed = 10f;

    [Header("Jump")]
    public AnimationCurve jumpForceCurve;
    public float maxJumpTime = 0.3f;
    public float maxJumpForce = 20f;
    public float jumpHeightMultiplier = 1f;
    public float jumpBufferTime = 0.1f;
    public float coyoteTime = 0.1f;

    [Header("Dash")]
    public float dashDistance = 9f;
    public float dashCooldown = 0.8f;
    public float extremeDashCooldown = 3f;
    public float extremeDashExtraInvincibility = 0.3f;
    public float extremeDashExtraDistance = 3f;
    public float fadePower = 6f;
    public float minLinearBlend = 0.2f;

    [Header("Parry")]
    public int perfectParryEnergyGain = 100;
    public int imperfectParryEnergyGain = 50;

    [Header("Power Parry")]
    public float powerParryHoldTime = 0.1f;
    public float powerParryPrepTick = 0.1f;
    public int powerParryPrepEnterCost = 300;
    public int powerParryPrepCost = 5;
    public float powerParryNoDrainTime = 0.6f;

    [Header("Heal")]
    public float healTickInterval = 0.1f;
    public int healEnergyPerTick = 10;
    public int healHealthPerTick = 10;

    [Header("Hit")]
    public float hitInvincibleTime = 1f;
    public float knockbackForce = 10f;

    [Header("Move Inertia")]
    public float accelTime = 0.25f;
    public float airReleaseDecelTime = 0.3f;
    [Range(0f, 1f)] public float startSpeedRatio = 0.35f;
    public float postDashCarryWindow = 0.1f;
}