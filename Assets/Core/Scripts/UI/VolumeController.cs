using UnityEngine;
using Sirenix.OdinInspector;

public sealed class VolumeController : MonoBehaviour
{
    [Header("Sliders (Value should be dB)")]
    [SerializeField, Required] private CustomSlider masterSlider;
    [SerializeField, Required] private CustomSlider bgmSlider;
    [SerializeField, Required] private CustomSlider sfxSlider;

    private AudioManager audioManager;
    private bool isRefreshingUi;

    public void Start()
    {
        audioManager = AudioManager.Instance;
        audioManager.LoadVolumeSettings();
        RefreshUI();
    }

    public void OnEnable()
    {
        masterSlider.OnValueChanged.AddListener(OnMasterChanged);
        bgmSlider.OnValueChanged.AddListener(OnBgmChanged);
        sfxSlider.OnValueChanged.AddListener(OnSfxChanged);
    }

    public void OnDisable()
    {
        masterSlider.OnValueChanged.RemoveListener(OnMasterChanged);
        bgmSlider.OnValueChanged.RemoveListener(OnBgmChanged);
        sfxSlider.OnValueChanged.RemoveListener(OnSfxChanged);
    }

    public void ResetToDefaults()
    {
        audioManager.ResetVolumesToDefault();
        RefreshUI();
    }

    private void RefreshUI()
    {
        isRefreshingUi = true;
        masterSlider.Value = audioManager.MasterVolumeDb;
        bgmSlider.Value = audioManager.BGMVolumeDb;
        sfxSlider.Value = audioManager.SFXVolumeDb;
        isRefreshingUi = false;
    }

    private void OnMasterChanged(float value)
    {
        if (isRefreshingUi)
            return;

        audioManager.MasterVolumeDb = value;
    }

    private void OnBgmChanged(float value)
    {
        if (isRefreshingUi)
            return;

        bgmSlider.Value = audioManager.BGMVolumeDb = value;
    }

    private void OnSfxChanged(float value)
    {
        if (isRefreshingUi)
            return;

        sfxSlider.Value = audioManager.SFXVolumeDb = value;
    }
}