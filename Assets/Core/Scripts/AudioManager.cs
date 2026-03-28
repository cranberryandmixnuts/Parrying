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
    [SerializeField] private AudioSource bgmPrefab;

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
    private readonly LinkedList<AudioSource> bgmSourcePool = new();

    private float masterVolumeDb = 0.0f;
    private float bgmVolumeDb = 0.0f;
    private float sfxVolumeDb = 0.0f;

    private bool isVolumeLoaded;

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

    public void Start() => LoadVolumeSettings();

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

        if (string.IsNullOrEmpty(audioName))
        {
            if (bgmSourcePool.Count <= 0)
                return;

            AudioSource currentSource = bgmSourcePool.First.Value;
            currentSource.DOKill();
            currentSource.DOFade(0f, fadeDuration).OnComplete(currentSource.Stop);
            bgmSourcePool.AddLast(currentSource);
            bgmSourcePool.RemoveFirst();
            return;
        }

        if (bgmSourcePool.Count <= 0)
        {
            AudioSource firstSource = CreateBGMSource();
            firstSource.clip = audioRegistry.GetAudioClip(audioName);
            firstSource.volume = 1f;
            firstSource.Play();
            bgmSourcePool.AddFirst(firstSource);
            return;
        }

        AudioSource playingSource = bgmSourcePool.First.Value;
        playingSource.DOKill();
        playingSource.DOFade(0f, fadeDuration).OnComplete(playingSource.Stop);
        bgmSourcePool.AddLast(playingSource);
        bgmSourcePool.RemoveFirst();

        if (bgmSourcePool.First.Value.isPlaying)
            bgmSourcePool.AddFirst(CreateBGMSource());

        AudioSource nextSource = bgmSourcePool.First.Value;
        nextSource.DOKill();
        nextSource.clip = audioRegistry.GetAudioClip(audioName);
        nextSource.volume = 0f;
        nextSource.Play();
        nextSource.DOFade(1f, fadeDuration);
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

    private AudioSource CreateBGMSource()
    {
        AudioSource audioSource = Instantiate(bgmPrefab, transform);

        if (bgmMixerGroup != null)
            audioSource.outputAudioMixerGroup = bgmMixerGroup;

        return audioSource;
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