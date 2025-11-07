using UnityEngine;

[CreateAssetMenu(fileName = "BossSettings", menuName = "Scriptable Objects/BossSettings")]
public sealed class BossSettings : ScriptableObject
{
    [Header("Common")]
    public LayerMask playerHitMask;
    public int p1Stacks = 3;
    public int p2Stacks = 1;
    public float idleDelay = 0.6f;
    public float groggyDuration = 2.2f;

    [Header("Sword Drop")]
    public int swordDamage = 16;
    public float swordStartAngle = 60f;
    public float swordEndAngle = -80f;
    public float swordBladeLength = 3.2f;
    public float swordBladeThickness = 0.8f;

    [Header("Plunge Tuning")]
    public float plungeTeleTime = 0.45f;
    public float plungeActiveTime = 0.28f;
    public int plungeDamage = 18;

    [Header("Rush Tuning")]
    public float rushSpeed = 12f;
    public float rushMaxTime = 2.0f;
    public float missBehindTime = 0.6f;
    public int rushDamage = 14;

    [Header("Volley+Laser Tuning")]
    public int missileVolleys = 3;
    public float missileVolleyInterval = 0.5f;
    public float laserWindupTime = 0.6f;
    public float laserActiveTime = 1.2f;
    public int laserDamage = 12;

    [Header("Radial Tuning")]
    public int radialSets = 12;
    public float radialBeat = 0.25f;
    public float radialActiveEach = 0.18f;
    public int radialDamage = 12;
}