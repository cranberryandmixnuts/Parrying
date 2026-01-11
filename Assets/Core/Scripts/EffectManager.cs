using DG.Tweening;
using UnityEngine;
using UnityEngine.VFX;
using Sirenix.OdinInspector;

public sealed class EffectManager : MonoBehaviour
{
    [TabGroup("Effect Manager", "Setup"), BoxGroup("Effect Manager/Setup/References"), SerializeField, Required]
    private Camera targetCamera;

    [TabGroup("Effect Manager", "Parry"), BoxGroup("Effect Manager/Parry/Shake"), SerializeField, MinValue(0f), SuffixLabel("u", true)]
    private float perfectShakeAmplitude = 0.1f;

    [TabGroup("Effect Manager", "Parry"), BoxGroup("Effect Manager/Parry/Shake"), SerializeField, MinValue(0f), SuffixLabel("s", true)]
    private float perfectShakeDuration = 0.1f;

    [TabGroup("Effect Manager", "Counter Parry"), BoxGroup("Effect Manager/Counter Parry/Freeze"), SerializeField, MinValue(0f), SuffixLabel("s", true)]
    private float counterFreezeDuration = 0.15f;

    [TabGroup("Effect Manager", "Counter Parry"), BoxGroup("Effect Manager/Counter Parry/Shake"), SerializeField, MinValue(0f), SuffixLabel("u", true)]
    private float counterShakeAmplitude = 0.15f;

    [TabGroup("Effect Manager", "Counter Parry"), BoxGroup("Effect Manager/Counter Parry/Shake"), SerializeField, MinValue(0f), SuffixLabel("s", true)]
    private float counterShakeDuration = 0.2f;

    [TabGroup("Effect Manager", "Counter Parry"), BoxGroup("Effect Manager/Counter Parry/Overlay"), SerializeField, Required]
    private GameObject counterFlashOverlay;

    [TabGroup("Effect Manager", "Dash"), BoxGroup("Effect Manager/Dash/Slowmo"), SerializeField, MinValue(0f), SuffixLabel("s", true)]
    private float extremeSlowFadeTime = 0.1f;

    [TabGroup("Effect Manager", "Dash"), BoxGroup("Effect Manager/Dash/Slowmo"), SerializeField, PropertyRange(0f, 1f), SuffixLabel("น่", true)]
    private float extremeSlowScale = 0.3f;

    [TabGroup("Effect Manager", "Dash"), BoxGroup("Effect Manager/Dash/Overlay"), SerializeField, Required]
    private GameObject slowmoOverlay;

    [TabGroup("Effect Manager", "Setup"), BoxGroup("Effect Manager/Setup/Visual Effects"), SerializeField, Required]
    private VisualEffect dash;

    [TabGroup("Effect Manager", "Setup"), BoxGroup("Effect Manager/Setup/Visual Effects"), SerializeField, Required]
    private VisualEffect healing;

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