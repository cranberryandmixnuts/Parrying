using DG.Tweening;
using UnityEngine;
using UnityEngine.VFX;

public sealed class PlayerEffects : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    [Header("Parry")]
    [SerializeField] private float perfectShakeAmplitude = 0.1f;
    [SerializeField] private float perfectShakeDuration = 0.1f;

    [Header("Counter Parry")]
    [SerializeField] private float counterFreezeDuration = 0.15f;
    [SerializeField] private float counterShakeAmplitude = 0.15f;
    [SerializeField] private float counterShakeDuration = 0.2f;
    [SerializeField] private GameObject counterFlashOverlay;

    [Header("Dash")]
    [SerializeField] private float extremeSlowFadeTime = 0.1f;
    [SerializeField] private float extremeSlowScale = 0.3f;
    [SerializeField] private GameObject slowmoOverlay;

    [Header("VFX")]
    [SerializeField] private VisualEffect dash;
    [SerializeField] private VisualEffect healing;
    public VisualEffect Dash => dash;
    public VisualEffect Healing => healing;

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