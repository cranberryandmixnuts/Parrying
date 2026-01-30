using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerStatsUI : Singleton<PlayerStatsUI, SceneScope>
{
    [Header("Gauges")]
    [SerializeField, Required] private Image healthGauge;
    [SerializeField, Required] private Image energyGauge;
    [SerializeField, Required] private Image healDelayGauge;
    [SerializeField, Required] private Image counterParryHoldGauge;

    [Header("Counter Parry Graduation")]
    [SerializeField, Required] private Image counterParryReadyImage;
    [SerializeField] private Sprite graduationSprite;
    [SerializeField] private Sprite disableGraduationSprite;

    private PlayerController player;

    private void Start()
    {
        player = PlayerController.Instance;
        counterParryReadyImage.sprite = graduationSprite;
    }

    private void Update()
    {
        healthGauge.fillAmount = Mathf.Clamp01((float)player.Vitals.Health / player.Vitals.MaxHealth);
        energyGauge.fillAmount = Mathf.Clamp01((float)player.Vitals.Energy / player.Vitals.MaxEnergy);

        healDelayGauge.fillAmount = Mathf.Clamp01(player.healDelayGauge);
        counterParryHoldGauge.fillAmount = Mathf.Clamp01(player.parryHoldTimer / player.Settings.counterParryHoldTime);
    }
}