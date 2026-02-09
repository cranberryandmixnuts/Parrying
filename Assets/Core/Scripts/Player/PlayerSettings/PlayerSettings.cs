using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "PlayerSettings", menuName = "Scriptable Objects/PlayerSettings")]
public sealed class PlayerSettings : ScriptableObject
{
    [TabGroup("PlayerSettings", "기본"), FoldoutGroup("PlayerSettings/기본/Stats", Expanded = true), SuffixLabel("HP", true), MinValue(1), MaxValue(99999)]
    public int maxHealth = 1000;

    [TabGroup("PlayerSettings", "기본"), FoldoutGroup("PlayerSettings/기본/Stats", Expanded = true), SuffixLabel("EN", true), MinValue(0), MaxValue(99999)]
    public int maxEnergy = 500;

    [TabGroup("PlayerSettings", "기본"), FoldoutGroup("PlayerSettings/기본/Stats", Expanded = true), SuffixLabel("HP", true), MinValue(0), MaxValue("@maxHealth")]
    public int currentHealth = 1000;

    [TabGroup("PlayerSettings", "기본"), FoldoutGroup("PlayerSettings/기본/Stats", Expanded = true), SuffixLabel("EN", true), MinValue(0), MaxValue("@maxEnergy")]
    public int currentEnergy = 500;

    [TabGroup("PlayerSettings", "이동"), FoldoutGroup("PlayerSettings/이동/Movement", Expanded = true), SuffixLabel("u/s", true), MinValue(0f), MaxValue(1000f)]
    public float moveSpeed = 10f;

    [TabGroup("PlayerSettings", "이동"), FoldoutGroup("PlayerSettings/이동/Movement", Expanded = true), SuffixLabel("s", true), MinValue(0f), MaxValue(60f)]
    public float groundAccelTime = 0.25f;

    [TabGroup("PlayerSettings", "이동"), FoldoutGroup("PlayerSettings/이동/Movement", Expanded = true), SuffixLabel("s", true), MinValue(0f), MaxValue(60f)]
    public float airAccelTime = 0.3f;

    [TabGroup("PlayerSettings", "이동"), FoldoutGroup("PlayerSettings/이동/Movement", Expanded = true), SuffixLabel("s", true), MinValue(0f), MaxValue(60f)]
    public float airReleaseDecelTime = 0.3f;

    [TabGroup("PlayerSettings", "이동"), FoldoutGroup("PlayerSettings/이동/Movement", Expanded = true), PropertyRange(0f, 1f)]
    public float startSpeedRatio = 0.15f;

    [TabGroup("PlayerSettings", "이동"), FoldoutGroup("PlayerSettings/이동/Movement", Expanded = true), SuffixLabel("s", true), MinValue(0f), MaxValue(60f)]
    public float postDashCarryWindow = 0.1f;

    [TabGroup("PlayerSettings", "이동"), FoldoutGroup("PlayerSettings/이동/Jump"), Required]
    public AnimationCurve jumpForceCurve;

    [TabGroup("PlayerSettings", "이동"), FoldoutGroup("PlayerSettings/이동/Jump"), SuffixLabel("s", true), MinValue(0f), MaxValue(60f)]
    public float maxJumpTime = 0.3f;

    [TabGroup("PlayerSettings", "이동"), FoldoutGroup("PlayerSettings/이동/Jump"), SuffixLabel("u", true), MinValue(0f), MaxValue(1000f)]
    public float maxJumpForce = 20f;

    [TabGroup("PlayerSettings", "이동"), FoldoutGroup("PlayerSettings/이동/Jump"), SuffixLabel("s", true), MinValue(0f), MaxValue(60f)]
    public float coyoteTime = 0.1f;

    [TabGroup("PlayerSettings", "이동"), FoldoutGroup("PlayerSettings/이동/Dash"), SuffixLabel("u", true), MinValue(0f), MaxValue(1000f)]
    public float dashDistance = 6f;

    [TabGroup("PlayerSettings", "이동"), FoldoutGroup("PlayerSettings/이동/Dash"), SuffixLabel("s", true), MinValue(0f), MaxValue(60f)]
    public float dashCooldown = 0.8f;

    [TabGroup("PlayerSettings", "이동"), FoldoutGroup("PlayerSettings/이동/Dash"), SuffixLabel("s", true), MinValue(0f), MaxValue(600f)]
    public float extremeDashCooldown = 3f;

    [TabGroup("PlayerSettings", "이동"), FoldoutGroup("PlayerSettings/이동/Dash"), SuffixLabel("s", true), MinValue(0f), MaxValue(60f)]
    public float extremeDashExtraInvincibility = 0.3f;

    [TabGroup("PlayerSettings", "이동"), FoldoutGroup("PlayerSettings/이동/Dash"), SuffixLabel("u", true), MinValue(0f), MaxValue(1000f)]
    public float extremeDashExtraDistance = 1f;

    [TabGroup("PlayerSettings", "이동"), FoldoutGroup("PlayerSettings/이동/Dash"), SuffixLabel("x", true), MinValue(0f), MaxValue(1000f)]
    public float fadePower = 5f;

    [TabGroup("PlayerSettings", "이동"), FoldoutGroup("PlayerSettings/이동/Dash"), PropertyRange(0f, 1f)]
    public float minLinearBlend = 0.1f;

    [TabGroup("PlayerSettings", "전투"), FoldoutGroup("PlayerSettings/전투/Normal Parry", Expanded = true), SuffixLabel("EN", true), MinValue(0), MaxValue(99999)]
    public int perfectParryEnergyGain = 100;

    [TabGroup("PlayerSettings", "전투"), FoldoutGroup("PlayerSettings/전투/Normal Parry", Expanded = true), SuffixLabel("EN", true), MinValue(0), MaxValue(99999)]
    public int imperfectParryEnergyGain = 50;

    [TabGroup("PlayerSettings", "전투"), FoldoutGroup("PlayerSettings/전투/Normal Parry", Expanded = true), SuffixLabel("u", true), MinValue(0f), MaxValue(1000f)]
    public float airParryKnockbackForce = 5f;

    [TabGroup("PlayerSettings", "전투"), FoldoutGroup("PlayerSettings/전투/Normal Parry", Expanded = true), SuffixLabel("s", true), MinValue(0f), MaxValue(60f)]
    public float airParryKnockbackDuration = 0.2f;

    [TabGroup("PlayerSettings", "전투"), FoldoutGroup("PlayerSettings/전투/Normal Parry", Expanded = true), SuffixLabel("s", true), MinValue(0f), MaxValue(60f)]
    public float airParryKnockbackSlowDuration = 0.3f;

    [TabGroup("PlayerSettings", "전투"), FoldoutGroup("PlayerSettings/전투/Normal Parry", Expanded = true), PropertyRange(0f, 1f), SuffixLabel("배", true)]
    public float airParryKnockbackSlowScale = 0.6f;

    [TabGroup("PlayerSettings", "전투"), FoldoutGroup("PlayerSettings/전투/Normal Parry", Expanded = true), SuffixLabel("s", true), MinValue(0f), MaxValue(60f)]
    public float parryExtraInvincibility = 0.1f;

    [TabGroup("PlayerSettings", "전투"), FoldoutGroup("PlayerSettings/전투/Counter Parry", Expanded = true), SuffixLabel("s", true), MinValue(0f), MaxValue(60f)]
    public float counterParryHoldTime = 0.1f;

    [TabGroup("PlayerSettings", "전투"), FoldoutGroup("PlayerSettings/전투/Counter Parry", Expanded = true), SuffixLabel("EN", true), MinValue(0), MaxValue(99999)]
    public int counterParryEnterCost = 300;

    [TabGroup("PlayerSettings", "전투"), FoldoutGroup("PlayerSettings/전투/Counter Parry", Expanded = true), SuffixLabel("s", true), MinValue(0f), MaxValue(60f)]
    public float counterParryDrainTick = 0.1f;

    [TabGroup("PlayerSettings", "전투"), FoldoutGroup("PlayerSettings/전투/Counter Parry", Expanded = true), SuffixLabel("EN", true), MinValue(0), MaxValue(99999)]
    public int counterParryDrainCost = 5;

    [TabGroup("PlayerSettings", "전투"), FoldoutGroup("PlayerSettings/전투/Counter Parry", Expanded = true), SuffixLabel("s", true), MinValue(0f), MaxValue(60f)]
    public float counterParryNoDrainTime = 0.6f;

    [TabGroup("PlayerSettings", "전투"), FoldoutGroup("PlayerSettings/전투/Counter Parry", Expanded = true), SuffixLabel("s", true), MinValue(0f), MaxValue(60f)]
    public float counterParryExtraInvincibility = 0.5f;

    [TabGroup("PlayerSettings", "전투"), FoldoutGroup("PlayerSettings/전투/Hit", Expanded = true), SuffixLabel("s", true), MinValue(0f), MaxValue(60f)]
    public float hitInvincibleTime = 1f;

    [TabGroup("PlayerSettings", "전투"), FoldoutGroup("PlayerSettings/전투/Hit", Expanded = true), SuffixLabel("u", true), MinValue(0f), MaxValue(1000f)]
    public float knockbackForce = 10f;

    [TabGroup("PlayerSettings", "전투"), FoldoutGroup("PlayerSettings/전투/Heal", Expanded = true), SuffixLabel("s", true), MinValue(0f), MaxValue(60f)]
    public float healTickInterval = 0.1f;

    [TabGroup("PlayerSettings", "전투"), FoldoutGroup("PlayerSettings/전투/Heal", Expanded = true), SuffixLabel("EN", true), MinValue(0), MaxValue(99999)]
    public int healEnergyPerTick = 10;

    [TabGroup("PlayerSettings", "전투"), FoldoutGroup("PlayerSettings/전투/Heal", Expanded = true), SuffixLabel("HP", true), MinValue(0), MaxValue(99999)]
    public int healHealthPerTick = 10;

    [TabGroup("PlayerSettings", "기본"), FoldoutGroup("PlayerSettings/기본/Buffer Time"), SuffixLabel("s", true), MinValue(0f), MaxValue(5f)]
    public float jumpBufferTime = 0.1f;

    [TabGroup("PlayerSettings", "기본"), FoldoutGroup("PlayerSettings/기본/Buffer Time"), SuffixLabel("s", true), MinValue(0f), MaxValue(5f)]
    public float parryBufferTime = 0.06f;

    [TabGroup("PlayerSettings", "기본"), FoldoutGroup("PlayerSettings/기본/Buffer Time"), SuffixLabel("s", true), MinValue(0f), MaxValue(5f)]
    public float dashBufferTime = 0.1f;
}