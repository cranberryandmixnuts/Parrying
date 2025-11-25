using UnityEngine;
using DG.Tweening;

public sealed class GameEffects : MonoBehaviour
{
    public static GameEffects Instance { get; private set; }

    [SerializeField] private Camera targetCamera;

    [SerializeField] private float perfectShakeAmplitude = 0.3f;
    [SerializeField] private float perfectShakeDuration = 0.15f;

    [SerializeField] private float counterFreezeDuration = 0.1f;
    [SerializeField] private float counterShakeAmplitude = 0.3f;
    [SerializeField] private float counterShakeDuration = 0.2f;

    [SerializeField] private float extremeSlowFadeTime = 0.1f;
    [SerializeField] private float extremeSlowScale = 0.1f;

    [SerializeField] private GameObject counterFlashOverlay;
    [SerializeField] private GameObject slowmoOverlay;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (targetCamera == null) targetCamera = Camera.main;
    }

    public void DoPerfectParryImpact()
    {
        Shake(perfectShakeDuration, perfectShakeAmplitude);
    }

    public void DoCounterParryImpact()
    {
        float prevScale = Time.timeScale;

        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(true);
        seq.AppendCallback(() =>
        {
            counterFlashOverlay.SetActive(true);
            Time.timeScale = 0f;
        })
        .AppendInterval(counterFreezeDuration)
        .AppendCallback(() =>
        {
            Time.timeScale = prevScale;
            counterFlashOverlay.SetActive(false);
        })
        .AppendCallback(() =>
        {
            Shake(counterShakeDuration, counterShakeAmplitude);
        });
    }

    public void DoExtremeDashImpact()
    {
        float prevScale = Time.timeScale;

        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(true);

        seq.AppendCallback(() =>
        {
            slowmoOverlay.SetActive(true);
        });

        seq.Append
        (
            DOTween.To(() => Time.timeScale, x => Time.timeScale = x, extremeSlowScale, extremeSlowFadeTime)
                   .SetUpdate(true)
        );

        seq.Append
        (
            DOTween.To(() => Time.timeScale, x => Time.timeScale = x, prevScale, extremeSlowFadeTime)
                   .SetUpdate(true)
                   .OnComplete(() =>
                   {
                       slowmoOverlay.SetActive(false);
                   })
        );
    }

    private void Shake(float duration, float amplitude)
    {
        Transform camTr = targetCamera.transform;
        Vector3 strength = new(amplitude, amplitude, 0f);

        camTr.DOShakePosition(duration, strength, 20, 90f, false, true)
             .SetUpdate(true);
    }
}