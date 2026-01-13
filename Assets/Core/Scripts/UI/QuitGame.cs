using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;

public sealed class QuitGame : MonoBehaviour
{
    [Header("Fade UI")]
    [SerializeField, Required] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.0f;

    private bool isQuitting;
    private Tween fadeTween;

    private float cachedTimeScale = 1f;
    private bool timePausedByThis;

    private void Awake()
    {
        Color imageColor = fadeImage.color;
        imageColor.a = 0f;
        fadeImage.color = imageColor;

        fadeImage.gameObject.SetActive(false);
    }

    public void Quit()
    {
        if (isQuitting) return;
        SoundManager.Instance.ChangeBgm(BgmId.None, fadeDuration);
        StartCoroutine(QuitSequence());
    }

    private IEnumerator QuitSequence()
    {
        isQuitting = true;
        PauseTime();

        if (!fadeImage.gameObject.activeSelf)
            fadeImage.gameObject.SetActive(true);

        if (fadeTween != null && fadeTween.IsActive())
            fadeTween.Kill(false);

        fadeTween = fadeImage
            .DOFade(1f, fadeDuration)
            .SetEase(Ease.Linear)
            .SetUpdate(true);

        yield return fadeTween.WaitForCompletion();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
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
}