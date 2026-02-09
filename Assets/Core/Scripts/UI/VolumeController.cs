using UnityEngine;
using UnityEngine.Audio;
using Sirenix.OdinInspector;

public sealed class VolumeController : MonoBehaviour
{
    private const string MasterKey = "Volume_Master_Db";
    private const string BgmKey = "Volume_BGM_Db";
    private const string SfxKey = "Volume_SFX_Db";

    [Header("Mixer")]
    [SerializeField, Required] private AudioMixer audioMixer;

    [SerializeField] private string masterParameter = "Master";
    [SerializeField] private string bgmParameter = "BGM";
    [SerializeField] private string sfxParameter = "SFX";

    [Header("Sliders (Value should be dB)")]
    [SerializeField, Required] private CustomSlider masterSlider;
    [SerializeField, Required] private CustomSlider bgmSlider;
    [SerializeField, Required] private CustomSlider sfxSlider;

    [Header("Defaults (dB)")]
    [SerializeField] private float defaultMasterDb = 0.0f;
    [SerializeField] private float defaultBgmDb = 0.0f;
    [SerializeField] private float defaultSfxDb = 0.0f;

    public void Start()
    {
        LoadOrInitialize();
        ApplyAll();
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

    [Button]
    public void ResetToDefaults()
    {
        masterSlider.Value = defaultMasterDb;
        bgmSlider.Value = defaultBgmDb;
        sfxSlider.Value = defaultSfxDb;

        ApplyAll();
        SaveAll();
    }

    [Button]
    public void ClearSaved()
    {
        PlayerPrefs.DeleteKey(MasterKey);
        PlayerPrefs.DeleteKey(BgmKey);
        PlayerPrefs.DeleteKey(SfxKey);
        PlayerPrefs.Save();
    }

    private void LoadOrInitialize()
    {
        float masterDb = LoadDb(MasterKey, masterParameter, defaultMasterDb);
        float bgmDb = LoadDb(BgmKey, bgmParameter, defaultBgmDb);
        float sfxDb = LoadDb(SfxKey, sfxParameter, defaultSfxDb);

        masterSlider.Value = masterDb;
        bgmSlider.Value = bgmDb;
        sfxSlider.Value = sfxDb;
    }

    private float LoadDb(string key, string parameter, float fallback)
    {
        if (PlayerPrefs.HasKey(key))
            return PlayerPrefs.GetFloat(key);

        if (audioMixer.GetFloat(parameter, out float db))
            return db;

        return fallback;
    }

    private void OnMasterChanged(float _)
    {
        ApplySliderToMixer(masterSlider, masterParameter);
        SaveDb(MasterKey, masterSlider.Value);
    }

    private void OnBgmChanged(float _)
    {
        ApplySliderToMixer(bgmSlider, bgmParameter);
        SaveDb(BgmKey, bgmSlider.Value);
    }

    private void OnSfxChanged(float _)
    {
        ApplySliderToMixer(sfxSlider, sfxParameter);
        SaveDb(SfxKey, sfxSlider.Value);
    }

    private void ApplyAll()
    {
        ApplySliderToMixer(masterSlider, masterParameter);
        ApplySliderToMixer(bgmSlider, bgmParameter);
        ApplySliderToMixer(sfxSlider, sfxParameter);
    }

    private void ApplySliderToMixer(CustomSlider slider, string parameter) => audioMixer.SetFloat(parameter, slider.Value);

    private void SaveAll()
    {
        SaveDb(MasterKey, masterSlider.Value);
        SaveDb(BgmKey, bgmSlider.Value);
        SaveDb(SfxKey, sfxSlider.Value);
    }

    private void SaveDb(string key, float db)
    {
        PlayerPrefs.SetFloat(key, db);
        PlayerPrefs.Save();
    }
}