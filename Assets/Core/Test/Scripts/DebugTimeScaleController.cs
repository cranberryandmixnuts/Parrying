using UnityEngine;
using UnityEngine.UI;

public sealed class DebugTimeScaleController : MonoBehaviour
{
    [SerializeField] private Slider timeScaleSlider;
    [SerializeField] private float slowTimeScale = 0.1f;
    [SerializeField] private float normalTimeScale = 1f;

    private float baseFixedDeltaTime;

    private void Awake()
    {
        baseFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void OnEnable()
    {
        timeScaleSlider.onValueChanged.AddListener(OnSliderValueChanged);
        OnSliderValueChanged(timeScaleSlider.value);
    }

    private void OnDisable()
    {
        timeScaleSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        float t = Mathf.InverseLerp(timeScaleSlider.minValue, timeScaleSlider.maxValue, value);
        float ts = Mathf.Lerp(slowTimeScale, normalTimeScale, t);

        Time.timeScale = ts;
        Time.fixedDeltaTime = baseFixedDeltaTime * ts;
    }
}