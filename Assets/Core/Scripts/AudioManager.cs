using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : Singleton<AudioManager, GlobalScope>
{
    private const string MasterKey = "Volume_Master_Db";
    private const string BgmKey = "Volume_BGM_Db";
    private const string SfxKey = "Volume_SFX_Db";

    [Header("References")]
    [SerializeField] private AudioRegistry audioRegistry;

    [Header("Prefabs")]
    [SerializeField] private AudioSource sfxPrefab;

    [Header("BGM Sources")]
    [SerializeField] private AudioSource bgmSourceA;
    [SerializeField] private AudioSource bgmSourceB;

    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("Parameters")]
    [SerializeField] private string masterParameter = "Master";
    [SerializeField] private string bgmParameter = "BGM";
    [SerializeField] private string sfxParameter = "SFX";

    [Header("Default Volume (dB)")]
    [SerializeField] private float defaultMasterDb = 0.0f;
    [SerializeField] private float defaultBgmDb = 0.0f;
    [SerializeField] private float defaultSfxDb = 0.0f;

    private readonly List<AudioSource> sfxSourcePool = new();
    private readonly Tween[] bgmFadeTweens = new Tween[2];
    private readonly ulong[] bgmFadeOrders = new ulong[2];

    private float masterVolumeDb = 0.0f;
    private float bgmVolumeDb = 0.0f;
    private float sfxVolumeDb = 0.0f;

    private int activeBgmSourceIndex = -1;
    private ulong bgmFadeSequence;
    private string currentBgmName;

    private bool isVolumeLoaded;
    private bool isBgmInitialized;

    public float MasterVolumeDb
    {
        get
        {
            EnsureVolumeLoaded();
            return masterVolumeDb;
        }
        set
        {
            EnsureVolumeLoaded();
            masterVolumeDb = value;
            ApplyMixerVolume(masterParameter, masterVolumeDb);
            SaveDb(MasterKey, masterVolumeDb);
        }
    }

    public float BGMVolumeDb
    {
        get
        {
            EnsureVolumeLoaded();
            return bgmVolumeDb;
        }
        set
        {
            EnsureVolumeLoaded();
            bgmVolumeDb = value;
            ApplyMixerVolume(bgmParameter, bgmVolumeDb);
            SaveDb(BgmKey, bgmVolumeDb);
        }
    }

    public float SFXVolumeDb
    {
        get
        {
            EnsureVolumeLoaded();
            return sfxVolumeDb;
        }
        set
        {
            EnsureVolumeLoaded();
            sfxVolumeDb = value;
            ApplyMixerVolume(sfxParameter, sfxVolumeDb);
            SaveDb(SfxKey, sfxVolumeDb);
        }
    }

    private void Start()
    {
        InitializeBgmSources();
        LoadVolumeSettings();
    }

    public void LoadVolumeSettings()
    {
        masterVolumeDb = LoadDb(MasterKey, masterParameter, defaultMasterDb);
        bgmVolumeDb = LoadDb(BgmKey, bgmParameter, defaultBgmDb);
        sfxVolumeDb = LoadDb(SfxKey, sfxParameter, defaultSfxDb);

        ApplyAllMixerVolumes();
        isVolumeLoaded = true;
    }

    public void ResetVolumesToDefault()
    {
        masterVolumeDb = defaultMasterDb;
        bgmVolumeDb = defaultBgmDb;
        sfxVolumeDb = defaultSfxDb;

        ApplyAllMixerVolumes();
        SaveAll();
        isVolumeLoaded = true;
    }

    public void SetBGM(string audioName, float fadeDuration = 1f)
    {
        EnsureVolumeLoaded();
        InitializeBgmSources();

        if (string.IsNullOrEmpty(audioName))
        {
            StopBGM(fadeDuration);
            return;
        }

        if (audioName == currentBgmName && activeBgmSourceIndex >= 0 && GetBgmSource(activeBgmSourceIndex).isPlaying)
            return;

        AudioClip clip = audioRegistry.GetAudioClip(audioName);
        currentBgmName = audioName;

        if (HasAnyBgmFade())
        {
            int reuseSourceIndex = GetOldestFadingBgmSourceIndex();
            int otherSourceIndex = GetOtherBgmSourceIndex(reuseSourceIndex);

            StopBgmSourceImmediately(reuseSourceIndex);
            FadeOutBgmSource(otherSourceIndex, fadeDuration);
            PlayBgmSource(reuseSourceIndex, clip, fadeDuration);

            activeBgmSourceIndex = reuseSourceIndex;
            return;
        }

        if (activeBgmSourceIndex >= 0 && GetBgmSource(activeBgmSourceIndex).isPlaying)
        {
            int nextSourceIndex = GetOtherBgmSourceIndex(activeBgmSourceIndex);

            FadeOutBgmSource(activeBgmSourceIndex, fadeDuration);
            PlayBgmSource(nextSourceIndex, clip, fadeDuration);

            activeBgmSourceIndex = nextSourceIndex;
            return;
        }

        int sourceIndex = GetPreferredBgmSourceIndex();
        PlayBgmSource(sourceIndex, clip, fadeDuration);
        activeBgmSourceIndex = sourceIndex;
    }

    public void PlaySFX(string audioName)
    {
        EnsureVolumeLoaded();

        AudioSource audioSource = GetAvailableSFXSource();
        audioSource.clip = audioRegistry.GetAudioClip(audioName);
        audioSource.volume = 1f;
        audioSource.Play();
    }

    public void PlaySFX(string audioName, float duration)
    {
        EnsureVolumeLoaded();

        AudioSource audioSource = GetAvailableSFXSource();
        audioSource.clip = audioRegistry.GetAudioClip(audioName);
        audioSource.volume = 1f;
        audioSource.Play();

        StartCoroutine(StopPlay(audioSource, duration));
    }

    private IEnumerator StopPlay(AudioSource audioSource, float duration)
    {
        yield return new WaitForSeconds(duration);
        audioSource.Stop();
    }

    private void StopBGM(float fadeDuration)
    {
        currentBgmName = null;

        if (HasAnyBgmFade())
        {
            int oldestFadingSourceIndex = GetOldestFadingBgmSourceIndex();
            int otherSourceIndex = GetOtherBgmSourceIndex(oldestFadingSourceIndex);

            StopBgmSourceImmediately(oldestFadingSourceIndex);
            FadeOutBgmSource(otherSourceIndex, fadeDuration);
            activeBgmSourceIndex = -1;
            return;
        }

        if (activeBgmSourceIndex < 0)
            return;

        FadeOutBgmSource(activeBgmSourceIndex, fadeDuration);
        FadeOutBgmSource(GetOtherBgmSourceIndex(activeBgmSourceIndex), fadeDuration);
        activeBgmSourceIndex = -1;
    }

    private AudioSource GetAvailableSFXSource()
    {
        foreach (AudioSource sfxSource in sfxSourcePool)
        {
            if (!sfxSource.isPlaying)
                return sfxSource;
        }

        return CreateSFXSource();
    }

    private AudioSource CreateSFXSource()
    {
        AudioSource audioSource = Instantiate(sfxPrefab, transform);

        if (sfxMixerGroup != null)
            audioSource.outputAudioMixerGroup = sfxMixerGroup;

        sfxSourcePool.Add(audioSource);
        return audioSource;
    }

    private void InitializeBgmSources()
    {
        if (isBgmInitialized)
            return;

        ConfigureBgmSource(bgmSourceA);
        ConfigureBgmSource(bgmSourceB);
        isBgmInitialized = true;
    }

    private void ConfigureBgmSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = true;
        source.volume = 0f;

        if (bgmMixerGroup != null)
            source.outputAudioMixerGroup = bgmMixerGroup;
    }

    private AudioSource GetBgmSource(int index) => index == 0 ? bgmSourceA : bgmSourceB;

    private int GetOtherBgmSourceIndex(int index) => index == 0 ? 1 : 0;

    private int GetPreferredBgmSourceIndex()
    {
        if (!bgmSourceA.isPlaying)
            return 0;

        if (!bgmSourceB.isPlaying)
            return 1;

        return 0;
    }

    private bool HasAnyBgmFade() => IsBgmFadeActive(0) || IsBgmFadeActive(1);

    private bool IsBgmFadeActive(int index) => bgmFadeTweens[index] != null && bgmFadeTweens[index].IsActive();

    private int GetOldestFadingBgmSourceIndex()
    {
        bool isFirstFading = IsBgmFadeActive(0);
        bool isSecondFading = IsBgmFadeActive(1);

        if (isFirstFading && isSecondFading)
            return bgmFadeOrders[0] <= bgmFadeOrders[1] ? 0 : 1;

        return isFirstFading ? 0 : 1;
    }

    private void KillBgmFade(int index)
    {
        if (bgmFadeTweens[index] == null)
            return;

        bgmFadeTweens[index].Kill();
        bgmFadeTweens[index] = null;
        bgmFadeOrders[index] = 0;
    }

    private void PlayBgmSource(int index, AudioClip clip, float fadeDuration)
    {
        AudioSource source = GetBgmSource(index);

        KillBgmFade(index);
        source.clip = clip;
        source.loop = true;

        if (fadeDuration <= 0f)
        {
            source.volume = 1f;
            source.Play();
            return;
        }

        source.volume = 0f;
        source.Play();

        bgmFadeOrders[index] = ++bgmFadeSequence;
        bgmFadeTweens[index] = source.DOFade(1f, fadeDuration).OnComplete(() =>
        {
            bgmFadeTweens[index] = null;
            bgmFadeOrders[index] = 0;
        });
    }

    private void FadeOutBgmSource(int index, float fadeDuration)
    {
        AudioSource source = GetBgmSource(index);

        if (!source.isPlaying)
        {
            StopBgmSourceImmediately(index);
            return;
        }

        KillBgmFade(index);

        if (fadeDuration <= 0f)
        {
            StopBgmSourceImmediately(index);
            return;
        }

        bgmFadeOrders[index] = ++bgmFadeSequence;
        bgmFadeTweens[index] = source.DOFade(0f, fadeDuration).OnComplete(() =>
        {
            source.Stop();
            source.clip = null;
            source.volume = 0f;
            bgmFadeTweens[index] = null;
            bgmFadeOrders[index] = 0;
        });
    }

    private void StopBgmSourceImmediately(int index)
    {
        AudioSource source = GetBgmSource(index);

        KillBgmFade(index);
        source.Stop();
        source.clip = null;
        source.volume = 0f;
    }

    private void EnsureVolumeLoaded()
    {
        if (isVolumeLoaded)
            return;

        LoadVolumeSettings();
    }

    private void ApplyAllMixerVolumes()
    {
        ApplyMixerVolume(masterParameter, masterVolumeDb);
        ApplyMixerVolume(bgmParameter, bgmVolumeDb);
        ApplyMixerVolume(sfxParameter, sfxVolumeDb);
    }

    private void ApplyMixerVolume(string parameterName, float db) => audioMixer.SetFloat(parameterName, db);

    private void SaveAll()
    {
        SaveDb(MasterKey, masterVolumeDb);
        SaveDb(BgmKey, bgmVolumeDb);
        SaveDb(SfxKey, sfxVolumeDb);
    }

    private void SaveDb(string key, float db)
    {
        PlayerPrefs.SetFloat(key, db);
        PlayerPrefs.Save();
    }

    private float LoadDb(string key, string parameter, float fallback)
    {
        if (PlayerPrefs.HasKey(key))
            return PlayerPrefs.GetFloat(key);

        if (audioMixer.GetFloat(parameter, out float db))
            return db;

        return fallback;
    }
}