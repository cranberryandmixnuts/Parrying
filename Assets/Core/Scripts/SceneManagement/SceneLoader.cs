using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;

public sealed class SceneLoader : Singleton<SceneLoader, GlobalScope>
{
    [Header("Fade UI")]
    [SerializeField, Required] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.0f;

    public bool IsTransitioning { get; private set; } = false;

    public SceneType CurrentSceneType { get; private set; } = SceneType.None;

    private Tween fadeTween;
    private Tween pendingRequestTween;
    private SceneType pendingScene = SceneType.None;

    private float cachedTimeScale = 1f;
    private bool timePausedByThis;

    private void Start()
    {
        CurrentSceneType = GetCurrentSceneType();

        BgmId bgm = GetBgmForScene(CurrentSceneType);
        SoundManager.Instance.ChangeBgm(bgm, fadeDuration);

        Color imageColor = fadeImage.color;
        imageColor.a = 0f;
        fadeImage.color = imageColor;

        fadeImage.gameObject.SetActive(false);
    }

    public void LoadScene(SceneType scene)
    {
        if (!TryResolveSceneName(scene, out string sceneName))
        {
            Debug.LogError($"씬 로드 거부: SceneType '{scene}' 에 대한 씬 이름을 해석할 수 없습니다. SceneTypeMap 생성/설정/Build Settings를 확인하세요.");
            return;
        }

        if (IsTransitioning)
        {
            ReserveSceneLoad(scene);
            return;
        }

        StartCoroutine(LoadSceneSequence(scene));
    }

    private void ReserveSceneLoad(SceneType scene)
    {
        pendingScene = scene;

        if (pendingRequestTween != null && pendingRequestTween.IsActive())
            pendingRequestTween.Kill(false);

        float delay = GetRemainingFadeTime();
        pendingRequestTween = DOVirtual.DelayedCall(delay, TryExecutePending, true).SetUpdate(true);
    }

    private void TryExecutePending()
    {
        if (IsTransitioning) return;
        if (pendingScene == SceneType.None) return;

        SceneType next = pendingScene;
        pendingScene = SceneType.None;
        LoadScene(next);
    }

    private IEnumerator LoadSceneSequence(SceneType scene)
    {
        IsTransitioning = true;
        PauseTime();

        fadeImage.gameObject.SetActive(true);

        BgmId bgm = GetBgmForScene(scene);
        SoundManager.Instance.ChangeBgm(bgm, fadeDuration);

        yield return FadeTo(1f).WaitForCompletion();

        if (!TryResolveSceneName(scene, out string sceneName))
        {
            Debug.LogError($"씬 로드 실패: SceneType '{scene}' 에 대한 씬 이름을 해석할 수 없습니다. SceneTypeMap 생성/설정/Build Settings를 확인하세요.");
            IsTransitioning = false;
            fadeImage.gameObject.SetActive(false);
            ResumeTime();
            yield break;
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
            yield return null;

        CurrentSceneType = GetCurrentSceneType();

        yield return FadeTo(0f).WaitForCompletion();

        IsTransitioning = false;
        fadeImage.gameObject.SetActive(false);

        ResumeTime();

        if (pendingScene != SceneType.None)
        {
            SceneType next = pendingScene;
            pendingScene = SceneType.None;
            LoadScene(next);
        }
    }

    private Tween FadeTo(float targetAlpha)
    {
        if (fadeTween != null && fadeTween.IsActive())
            fadeTween.Kill(false);

        fadeTween = fadeImage
            .DOFade(targetAlpha, fadeDuration)
            .SetEase(Ease.Linear)
            .SetUpdate(true);

        return fadeTween;
    }

    private float GetRemainingFadeTime()
    {
        if (fadeTween == null) return 0f;
        if (!fadeTween.IsActive()) return 0f;
        if (!fadeTween.IsPlaying()) return 0f;

        float remaining = fadeTween.Duration(false) - fadeTween.Elapsed(false);
        if (remaining < 0f) remaining = 0f;
        return remaining;
    }

    private void PauseTime()
    {
        if (timePausedByThis) return;
        if (Time.timeScale == 0f) return;

        cachedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        timePausedByThis = true;
    }

    private void ResumeTime()
    {
        if (!timePausedByThis) return;

        Time.timeScale = cachedTimeScale;
        timePausedByThis = false;
    }

    private void OnDisable()
    {
        if (timePausedByThis)
            ResumeTime();
    }

    private SceneType GetCurrentSceneType()
    {
        string activeName = SceneManager.GetActiveScene().name;

        SceneType[] values = (SceneType[])Enum.GetValues(typeof(SceneType));
        for (int i = 0; i < values.Length; ++i)
        {
            SceneType t = values[i];
            if (t == SceneType.None)
                continue;

            string mappedName = SceneTypeMap.GetName(t);
            if (string.Equals(mappedName, activeName, StringComparison.Ordinal))
                return t;
        }

        Debug.LogError($"현재 씬 이름 '{activeName}' 이 SceneTypeMap과 일치하지 않습니다.");
        return SceneType.None;
    }

    private bool TryResolveSceneName(SceneType scene, out string sceneName)
    {
        if (scene == SceneType.None)
        {
            sceneName = "";
            return false;
        }

        sceneName = SceneTypeMap.GetName(scene);
        if (string.IsNullOrEmpty(sceneName))
            return false;

        return true;
    }

    private BgmId GetBgmForScene(SceneType scene)
    {
        return scene switch
        {
            SceneType.TitleScene => BgmId.Title,
            _ => BgmId.None,
        };
    }
}