using UnityEngine;
using Sirenix.OdinInspector;

public sealed class VolumeController : MonoBehaviour
{
    private const float MinDb = -80.0f;
    private static readonly Vector2 SliderRange = new(0.0f, 1.0f);

    [Header("Sliders (Value should be 0.0 ~ 1.0)")]
    [SerializeField, Required] private CustomSlider masterSlider;
    [SerializeField, Required] private CustomSlider bgmSlider;
    [SerializeField, Required] private CustomSlider sfxSlider;

    private AudioManager audioManager;
    private bool isInitialized;
    private bool isRefreshingUi;

    public void OnEnable()
    {
        Initialize();

        masterSlider.OnValueChanged.AddListener(OnMasterChanged);
        bgmSlider.OnValueChanged.AddListener(OnBgmChanged);
        sfxSlider.OnValueChanged.AddListener(OnSfxChanged);
        RefreshUI();
    }

    public void OnDisable()
    {
        masterSlider.OnValueChanged.RemoveListener(OnMasterChanged);
        bgmSlider.OnValueChanged.RemoveListener(OnBgmChanged);
        sfxSlider.OnValueChanged.RemoveListener(OnSfxChanged);
    }

    private void Initialize()
    {
        if (isInitialized)
            return;

        audioManager = AudioManager.Instance;
        audioManager.LoadVolumeSettings();

        ConfigureSlider(masterSlider);
        ConfigureSlider(bgmSlider);
        ConfigureSlider(sfxSlider);

        isInitialized = true;
    }

    private static void ConfigureSlider(CustomSlider slider) => slider.ValueRange = SliderRange;

    private void RefreshUI()
    {
        isRefreshingUi = true;
        masterSlider.Value = DbToLinear(audioManager.MasterVolumeDb);
        bgmSlider.Value = DbToLinear(audioManager.BGMVolumeDb);
        sfxSlider.Value = DbToLinear(audioManager.SFXVolumeDb);
        isRefreshingUi = false;
    }

    private void OnMasterChanged(float value)
    {
        if (isRefreshingUi)
            return;

        audioManager.MasterVolumeDb = LinearToDb(value);
    }

    private void OnBgmChanged(float value)
    {
        if (isRefreshingUi)
            return;

        audioManager.BGMVolumeDb = LinearToDb(value);
    }

    private void OnSfxChanged(float value)
    {
        if (isRefreshingUi)
            return;

        audioManager.SFXVolumeDb = LinearToDb(value);
    }

    private static float LinearToDb(float linear)
    {
        if (linear <= 0.0f)
            return MinDb;

        return Mathf.Clamp(20.0f * Mathf.Log10(linear), MinDb, 0.0f);
    }

    private static float DbToLinear(float db)
    {
        if (db <= MinDb)
            return 0.0f;

        return Mathf.Clamp01(Mathf.Pow(10.0f, db / 20.0f));
    }
}