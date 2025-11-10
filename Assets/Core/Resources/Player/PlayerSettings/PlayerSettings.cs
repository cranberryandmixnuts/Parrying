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
    public float dashSpeed = 30f;
    public float dashDuration = 0.16f;
    public float dashCooldown = 0.8f;
    public float dashExtremeExtraInvincibility = 0.3f;

    [Header("Parry")]
    public float parryWindow = 0.2f;
    public int perfectParryEnergyGain = 100;
    public int imperfectParryEnergyGain = 50;

    [Header("Power Parry")]
    public float powerParryHoldTime = 0.1f;
    public float powerParryPrepTick = 0.1f;
    public int powerParryPrepEnterCost = 300;
    public int powerParryPrepCost = 5;
    public float powerParryNoDrainTime = 0.6f;
    public float powerParryDuration = 0.5f;

    [Header("Heal")]
    public float healTickInterval = 0.1f;
    public int healEnergyPerTick = 10;
    public int healHealthPerTick = 10;
    public float healEndLag = 0.3f;

    [Header("Hit")]
    public float hitStunDuration = 0.25f;
    public float hitInvincibleTime = 1f;
    public float knockbackDuration = 0.1f;
    public float knockbackForce = 10f;

    [Header("Move Inertia")]
    public float accelTime = 0.25f;
    public float airReleaseDecelTime = 0.3f;
    [Range(0f, 1f)] public float startSpeedRatio = 0.35f;
    public float postDashCarryWindow = 0.1f;
}