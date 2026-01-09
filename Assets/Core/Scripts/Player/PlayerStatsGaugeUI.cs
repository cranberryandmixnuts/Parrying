using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerStatsGaugeUI : MonoBehaviour
{
    [Header("Gauges")]
    [SerializeField] private Image healthGauge;
    [SerializeField] private Image energyGauge;
    [SerializeField] private Image healDelayGauge;
    [SerializeField] private Image counterParryHoldGauge;

    [Header("Counter Parry Graduation")]
    [SerializeField] private Image counterParryReadyImage;
    [SerializeField] private Sprite graduationSprite;
    [SerializeField] private Sprite disableGraduationSprite;

    private PlayerController player;
    private bool lastCounterParryReady;

    private void Start()
    {
        player = PlayerController.Instance;
        lastCounterParryReady = false;
        counterParryReadyImage.sprite = graduationSprite;
    }

    private void Update()
    {
        healthGauge.fillAmount = Mathf.Clamp01((float)player.Vitals.Health / player.Vitals.MaxHealth);
        energyGauge.fillAmount = Mathf.Clamp01((float)player.Vitals.Energy / player.Vitals.MaxEnergy);

        healDelayGauge.fillAmount = Mathf.Clamp01(player.healDelayGauge);

        if (player.inCounterParryPrep)
        {
            float holdTime = player.Settings.counterParryHoldTime;

            if (holdTime > 0f)
                counterParryHoldGauge.fillAmount = Mathf.Clamp01(player.counterParryPrepElapsed / holdTime);
            else
                counterParryHoldGauge.fillAmount = 1f;
        }
        else
            counterParryHoldGauge.fillAmount = 0f;

        bool counterParryReady = player.Vitals.Energy >= player.Settings.counterParryEnterCost;

        if (counterParryReady != lastCounterParryReady)
        {
            counterParryReadyImage.sprite = counterParryReady ? graduationSprite : disableGraduationSprite;
            lastCounterParryReady = counterParryReady;
        }
    }
}