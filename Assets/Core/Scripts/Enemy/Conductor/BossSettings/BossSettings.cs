using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "BossSettings", menuName = "Scriptable Objects/BossSettings")]
public sealed class BossSettings : ScriptableObject
{
    [TabGroup("BossSettings", "Common"), BoxGroup("BossSettings/Common/Layers")]
    public LayerMask playerHitMask;

    [TabGroup("BossSettings", "Common"), BoxGroup("BossSettings/Common/Layers")]
    public LayerMask groundLayer;

    [TabGroup("BossSettings", "Common"), BoxGroup("BossSettings/Common/Phase 1"), MinValue(1)]
    public int p1CountersToTriggerCurvedSlash = 2;

    [TabGroup("BossSettings", "Common"), BoxGroup("BossSettings/Common/Phase 1"), MinValue(1)]
    public int p1PatternBundleRepeatCount = 3;

    [TabGroup("BossSettings", "Common"), BoxGroup("BossSettings/Common/Phase 1"), ShowInInspector, ReadOnly]
    public int P1TotalStacks => (p1CountersToTriggerCurvedSlash + 1) * p1PatternBundleRepeatCount;

    [TabGroup("BossSettings", "Common"), BoxGroup("BossSettings/Common/Timing"), SuffixLabel("s", true), MinValue(0f)]
    public float idleDelay = 0.6f;

    [TabGroup("BossSettings", "Common"), BoxGroup("BossSettings/Common/Timing"), SuffixLabel("s", true), MinValue(0f)]
    public float groggyDuration = 2f;

    [TabGroup("BossSettings", "Common"), BoxGroup("BossSettings/Common/Misc"), PropertyRange(0f, 1f), SuffixLabel("น่", true)]
    public float repeatPenalty = 0.7f;

    [TabGroup("BossSettings", "Sword Drop"), BoxGroup("BossSettings/Sword Drop/Damage"), MinValue(0)]
    public int swordDamage = 200;

    [TabGroup("BossSettings", "Sword Drop"), BoxGroup("BossSettings/Sword Drop/Geometry"), SuffixLabel("deg", true)]
    public float swordStartAngle = 90f;

    [TabGroup("BossSettings", "Sword Drop"), BoxGroup("BossSettings/Sword Drop/Geometry"), SuffixLabel("deg", true)]
    public float swordEndAngle = 0f;

    [TabGroup("BossSettings", "Sword Drop"), BoxGroup("BossSettings/Sword Drop/Geometry"), SuffixLabel("u", true), MinValue(0f)]
    public float swordBladeLength = 7f;

    [TabGroup("BossSettings", "Sword Drop"), BoxGroup("BossSettings/Sword Drop/Geometry"), SuffixLabel("u", true), MinValue(0f)]
    public float swordBladeThickness = 0.5f;

    [TabGroup("BossSettings", "Plunge"), BoxGroup("BossSettings/Plunge/Timing"), SuffixLabel("s", true), MinValue(0f)]
    public float plungeFallDelay = 0.1f;

    [TabGroup("BossSettings", "Plunge"), BoxGroup("BossSettings/Plunge/Movement"), SuffixLabel("u/s", true), MinValue(0f)]
    public float plungeFallSpeed = 16f;

    [TabGroup("BossSettings", "Plunge"), BoxGroup("BossSettings/Plunge/Damage"), MinValue(0)]
    public int plungeDamage = 100;

    [TabGroup("BossSettings", "Plunge"), BoxGroup("BossSettings/Plunge/Movement"), SuffixLabel("u/s", true), MinValue(0f)]
    public float plungeBounceUpSpeed = 12f;

    [TabGroup("BossSettings", "Curved Slash"), BoxGroup("BossSettings/Curved Slash/Damage"), MinValue(0)]
    public int curvedSlashDamage = 200;

    [TabGroup("BossSettings", "Curved Slash"), BoxGroup("BossSettings/Curved Slash/Geometry"), SuffixLabel("deg", true)]
    public float curvedSlashStartAngle = 130f;

    [TabGroup("BossSettings", "Curved Slash"), BoxGroup("BossSettings/Curved Slash/Geometry"), SuffixLabel("deg", true)]
    public float curvedSlashEndAngle = -20f;

    [TabGroup("BossSettings", "Curved Slash"), BoxGroup("BossSettings/Curved Slash/Geometry"), SuffixLabel("u", true), MinValue(0f)]
    public float curvedSlashBladeLength = 7f;

    [TabGroup("BossSettings", "Curved Slash"), BoxGroup("BossSettings/Curved Slash/Geometry"), SuffixLabel("u", true), MinValue(0f)]
    public float curvedSlashBladeThickness = 0.5f;

    [TabGroup("BossSettings", "Curved Slash"), BoxGroup("BossSettings/Curved Slash/Curve"), SuffixLabel("u", true)]
    public float curvedSlashDownCurveBulgeY = 6f;

    [TabGroup("BossSettings", "Curved Slash"), BoxGroup("BossSettings/Curved Slash/Curve"), SuffixLabel("u", true)]
    public float curvedSlashUpCurveBulgeY = -6f;

    [TabGroup("BossSettings", "Curved Slash"), BoxGroup("BossSettings/Curved Slash/External Rush"), SuffixLabel("u/s", true), MinValue(0f)]
    public float externalRushSpeed = 18f;

    [TabGroup("BossSettings", "Curved Slash"), BoxGroup("BossSettings/Curved Slash/External Rush"), SuffixLabel("u", true), MinValue(0f)]
    public float externalRushSideYRandomRange = 6f;

    [TabGroup("BossSettings", "Curved Slash"), BoxGroup("BossSettings/Curved Slash/External Rush"), SuffixLabel("u", true), MinValue(0f)]
    public float externalRushTopXRandomRange = 10f;

    [TabGroup("BossSettings", "Curved Slash"), BoxGroup("BossSettings/Curved Slash/External Rush"), SuffixLabel("s", true), MinValue(0f)]
    public float externalRushHitDisableTime = 1f;

    [TabGroup("BossSettings", "Rush"), BoxGroup("BossSettings/Rush/Timing"), SuffixLabel("s", true), MinValue(0f)]
    public float rushStartDelay = 0.1f;

    [TabGroup("BossSettings", "Rush"), BoxGroup("BossSettings/Rush/Movement"), SuffixLabel("u/s", true), MinValue(0f)]
    public float rushSpeed = 12f;

    [TabGroup("BossSettings", "Rush"), BoxGroup("BossSettings/Rush/Timing"), SuffixLabel("s", true), MinValue(0f)]
    public float rushMaxTime = 2.0f;

    [TabGroup("BossSettings", "Rush"), BoxGroup("BossSettings/Rush/Timing"), SuffixLabel("s", true), MinValue(0f)]
    public float missBehindTime = 0.6f;

    [TabGroup("BossSettings", "Rush"), BoxGroup("BossSettings/Rush/Damage"), MinValue(0)]
    public int rushDamage = 150;

    [TabGroup("BossSettings", "Volley & Laser"), BoxGroup("BossSettings/Volley & Laser/Missile"), MinValue(0)]
    public int missileVolleys = 3;

    [TabGroup("BossSettings", "Volley & Laser"), BoxGroup("BossSettings/Volley & Laser/Missile"), SuffixLabel("s", true), MinValue(0f)]
    public float missileVolleyInterval = 0.5f;

    [TabGroup("BossSettings", "Volley & Laser"), BoxGroup("BossSettings/Volley & Laser/Missile"), SuffixLabel("HP", true), MinValue(0f)]
    public int projectileDamage = 50;

    [SuffixLabel("s", true), MinValue(0f)]
    [TabGroup("BossSettings", "Volley & Laser"), BoxGroup("BossSettings/Volley & Laser/Laser Shared")]
    [TabGroup("BossSettings", "Radial"), BoxGroup("BossSettings/Radial/Laser Shared")]
    public float laserWindupTime = 1f;

    [SuffixLabel("s", true), MinValue(0f)]
    [TabGroup("BossSettings", "Volley & Laser"), BoxGroup("BossSettings/Volley & Laser/Laser Shared")]
    [TabGroup("BossSettings", "Radial"), BoxGroup("BossSettings/Radial/Laser Shared")]
    public float extraWarningTail = 0.5f;

    [SuffixLabel("s", true), MinValue(0f)]
    [TabGroup("BossSettings", "Volley & Laser"), BoxGroup("BossSettings/Volley & Laser/Laser Shared")]
    [TabGroup("BossSettings", "Radial"), BoxGroup("BossSettings/Radial/Laser Shared")]
    public float laserActiveTime = 0.3f;

    [TabGroup("BossSettings", "Volley & Laser"), BoxGroup("BossSettings/Volley & Laser/Laser Timing"), SuffixLabel("s", true), MinValue(0f)]
    public float missileToLaserExtraDelay = 0.5f;

    [TabGroup("BossSettings", "Volley & Laser"), BoxGroup("BossSettings/Volley & Laser/Laser Movement"), SuffixLabel("deg/s", true), MinValue(0f)]
    public float laserTurnSpeedDegPerSec = 240f;

    [TabGroup("BossSettings", "Volley & Laser"), BoxGroup("BossSettings/Volley & Laser/Damage"), MinValue(0)]
    public int laserDamage = 300;

    [TabGroup("BossSettings", "Volley & Laser"), BoxGroup("BossSettings/Volley & Laser/Geometry"), SuffixLabel("u", true), MinValue(0f)]
    public float volleySideOffset = 6f;

    [SuffixLabel("s", true), MinValue(0f)]
    [TabGroup("BossSettings", "Volley & Laser"), BoxGroup("BossSettings/Volley & Laser/Laser Shared")]
    [TabGroup("BossSettings", "Radial"), BoxGroup("BossSettings/Radial/Laser Shared")]
    public float laserLength = 14f;

    [SuffixLabel("s", true), MinValue(0f)]
    [TabGroup("BossSettings", "Volley & Laser"), BoxGroup("BossSettings/Volley & Laser/Laser Shared")]
    [TabGroup("BossSettings", "Radial"), BoxGroup("BossSettings/Radial/Laser Shared")]
    public float laserThickness = 0.25f;

    [TabGroup("BossSettings", "Radial"), BoxGroup("BossSettings/Radial/Damage"), MinValue(0)]
    public int radialDamage = 300;

    [TabGroup("BossSettings", "Radial"), BoxGroup("BossSettings/Radial/Timing"), SuffixLabel("s", true), MinValue(0f)]
    public float radialBeat = 0.3f;

    [TabGroup("BossSettings", "Radial"), BoxGroup("BossSettings/Radial/Pattern"), MinMaxSlider(1f, 64f, true)]
    public Vector2 radialBeamCountRange = new(8f, 16f);

    [TabGroup("BossSettings", "Radial"), BoxGroup("BossSettings/Radial/Pattern"), SuffixLabel("deg", true), MinValue(0f)]
    public float radialMinAngleSeparationDeg = 8f;
}