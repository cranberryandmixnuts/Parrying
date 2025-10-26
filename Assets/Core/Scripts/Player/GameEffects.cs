using UnityEngine;
using DG.Tweening;

public sealed class GameEffects : MonoBehaviour
{
    public static GameEffects Instance { get; private set; }

    [SerializeField] private Camera targetCamera;

    [SerializeField] private float perfectShakeAmplitude = 0.3f;
    [SerializeField] private float perfectShakeDuration = 0.15f;

    [SerializeField] private float imperfectShakeAmplitude = 0.15f;
    [SerializeField] private float imperfectShakeDuration = 0.15f;

    [SerializeField] private float counterFreezeDuration = 0.1f;
    [SerializeField] private float counterShakeAmplitude = 0.3f;
    [SerializeField] private float counterShakeDuration = 0.2f;

    [SerializeField] private float extremeSlowDuration = 0.1f;
    [SerializeField] private float extremeSlowScale = 0.1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (targetCamera == null) targetCamera = Camera.main;
    }

    public void DoPerfectParryImpact()
    {
        Shake(perfectShakeDuration, perfectShakeAmplitude);
    }

    public void DoImperfectParryImpact()
    {
        Shake(imperfectShakeDuration, imperfectShakeAmplitude);
    }

    public void DoCounterParryImpact()
    {
        float prevScale = Time.timeScale;

        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(true);
        seq.AppendCallback(() => Time.timeScale = 0f)
           .AppendInterval(counterFreezeDuration)
           .AppendCallback(() => Time.timeScale = prevScale)
           .AppendCallback(() => Shake(counterShakeDuration, counterShakeAmplitude));
    }

    public void DoExtremeDashImpact()
    {
        float prevScale = Time.timeScale;

        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(true);
        seq.AppendCallback(() => Time.timeScale = extremeSlowScale)
           .AppendInterval(extremeSlowDuration)
           .AppendCallback(() => Time.timeScale = prevScale);
    }

    private void Shake(float duration, float amplitude)
    {
        Transform camTr = targetCamera.transform;
        Vector3 strength = new Vector3(amplitude, amplitude, 0f);

        camTr.DOShakePosition(duration, strength, 20, 90f, false, true)
             .SetUpdate(true);
    }
}