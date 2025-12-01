using UnityEngine;

[CreateAssetMenu(fileName = "BossSettings", menuName = "Scriptable Objects/BossSettings")]
public sealed class BossSettings : ScriptableObject
{
    [Header("Common")]
    public LayerMask playerHitMask;
    public LayerMask groundLayer;
    public int p1Stacks = 9;
    public int p2Stacks = 1;
    public float idleDelay = 1f;
    public float groggyDuration = 2f;

    [Header("Sword Drop")]
    public int swordDamage = 10;
    public float swordStartAngle = 90f;
    public float swordEndAngle = 0f;
    public float swordBladeLength = 9f;
    public float swordBladeThickness = 0.5f;

    [Header("Plunge Tuning")]
    public float plungeFallDelay = 0.1f;
    public float plungeFallSpeed = 16f;
    public int plungeDamage = 10;
    public float plungeBounceUpSpeed = 12f;

    [Header("Rush Tuning")]
    public float rushStartDelay = 0.1f;
    public float rushSpeed = 12f;
    public float rushMaxTime = 2.0f;
    public float missBehindTime = 0.6f;
    public int rushDamage = 10;

    [Header("Volley&Laser Tuning")]
    public int missileVolleys = 3;
    public float missileVolleyInterval = 0.5f;
    public float laserWindupTime = 1f;
    public float extraWarningTail = 0.5f;
    public float laserActiveTime = 0.3f;
    public float missileToLaserExtraDelay = 0.5f;
    public float laserTurnSpeedDegPerSec = 240f;
    public int laserDamage = 10;
    public float volleySideOffset = 6f;
    public float laserLength = 14f;
    public float laserThickness = 0.25f;

    [Header("Radial Tuning")]
    public int radialDamage = 10;
    public float radialBeat = 0.3f;
    public Vector2 radialBeamCountRange = new(8f, 16f);
    public float radialMinAngleSeparationDeg = 8f;
}