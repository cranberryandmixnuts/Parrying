using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;

public sealed class EffectManager : Singleton<EffectManager, SceneScope>
{
    [TabGroup("Effect Manager", "Setup"), BoxGroup("Effect Manager/Setup/References"), SerializeField, Required]
    private Camera targetCamera;

    [TabGroup("Effect Manager", "Setup"), BoxGroup("Effect Manager/Setup/References"), SerializeField, Required]
    private GameObject GameOverOptionButtons;

    [TabGroup("Effect Manager", "Setup"), BoxGroup("Effect Manager/Setup/Scene Roots"), SerializeField, Required]
    private Transform followPlayerRoot;

    [TabGroup("Effect Manager", "Setup"), BoxGroup("Effect Manager/Setup/Scene Roots"), SerializeField, Required]
    private Transform followEnemyRoot;

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

    [TabGroup("Effect Manager", "Dash"), BoxGroup("Effect Manager/Dash/Slowmo"), SerializeField, MinValue(0f), SuffixLabel("s", true)]
    private float extremeSlowFadeTime = 0.1f;

    [TabGroup("Effect Manager", "Dash"), BoxGroup("Effect Manager/Dash/Slowmo"), SerializeField, PropertyRange(0f, 1f), SuffixLabel("x", true)]
    private float extremeSlowScale = 0.3f;

    [TabGroup("Effect Manager", "Overlays"), BoxGroup("Effect Manager/Overlays/References"), SerializeField, Required]
    private GameObject counterFlashOverlay;

    [TabGroup("Effect Manager", "Overlays"), BoxGroup("Effect Manager/Overlays/References"), SerializeField, Required]
    private GameObject slowmoOverlay;

    [TabGroup("Effect Manager", "Overlays"), BoxGroup("Effect Manager/Overlays/References"), SerializeField, Required]
    private GameObject gameOverOverlay;

    [TabGroup("Effect Manager", "VFX - World"), BoxGroup("Effect Manager/VFX - World/References"), SerializeField, Required]
    private VisualEffect dash;

    [TabGroup("Effect Manager", "VFX - FollowPlayer"), BoxGroup("Effect Manager/VFX - FollowPlayer/References"), SerializeField, Required]
    private VisualEffect healing;

    [TabGroup("Effect Manager", "VFX - FollowPlayer"), BoxGroup("Effect Manager/VFX - FollowPlayer/References"), SerializeField, Required]
    private VisualEffect healLine;

    [TabGroup("Effect Manager", "VFX - FollowPlayer"), BoxGroup("Effect Manager/VFX - FollowPlayer/References"), SerializeField, Required]
    private VisualEffect healingUI;

    [TabGroup("Effect Manager", "VFX - FollowPlayer"), BoxGroup("Effect Manager/VFX - FollowPlayer/References"), SerializeField, Required]
    private GameObject counterParryCharge;

    [TabGroup("Effect Manager", "VFX - FollowPlayer"), BoxGroup("Effect Manager/VFX - FollowPlayer/References"), SerializeField, Required]
    private GameObject counterParryChargeUI;

    [TabGroup("Effect Manager", "VFX - FollowPlayer"), BoxGroup("Effect Manager/VFX - FollowPlayer/References"), SerializeField, Required]
    private VisualEffect counterParry;

    [TabGroup("Effect Manager", "VFX - FollowPlayer"), BoxGroup("Effect Manager/VFX - FollowPlayer/References"), SerializeField, Required]
    private VisualEffect counterParrySuccess;

    [TabGroup("Effect Manager", "VFX - FollowPlayer"), BoxGroup("Effect Manager/VFX - FollowPlayer/References"), SerializeField, Required]
    private VisualEffect hit;

    [TabGroup("Effect Manager", "VFX - FollowPlayer"), BoxGroup("Effect Manager/VFX - FollowPlayer/Parry"), SerializeField, Required]
    private VisualEffect parryTemplate;

    [TabGroup("Effect Manager", "VFX - FollowPlayer"), BoxGroup("Effect Manager/VFX - FollowPlayer/Parry"), SerializeField, MinValue(0)]
    private int parryPrewarmCount = 8;

    [TabGroup("Effect Manager", "VFX - FollowPlayer"), BoxGroup("Effect Manager/VFX - FollowPlayer/Parry"), SerializeField, MinValue(0f), SuffixLabel("s", true)]
    private float parryAutoReleaseTimeout = 2f;

    [TabGroup("Effect Manager", "VFX - FollowEnemy"), BoxGroup("Effect Manager/VFX - FollowEnemy/Laser"), SerializeField, Required]
    private VisualEffect laserTemplate;

    [TabGroup("Effect Manager", "VFX - FollowEnemy"), BoxGroup("Effect Manager/VFX - FollowEnemy/Laser"), SerializeField]
    private Vector3 laserLocalOffset;

    [TabGroup("Effect Manager", "VFX - FollowEnemy"), BoxGroup("Effect Manager/VFX - FollowEnemy/Laser"), SerializeField, MinValue(0)]
    private int laserPrewarmCount = 20;

    [TabGroup("Effect Manager", "VFX - FollowEnemy"), BoxGroup("Effect Manager/VFX - FollowEnemy/Laser"), SerializeField, MinValue(0f), SuffixLabel("s", true)]
    private float laserAutoReleaseTimeout = 3f;

    private readonly Queue<VisualEffect> parryPool = new();
    private readonly List<VisualEffect> parryActive = new();
    private readonly Dictionary<VisualEffect, Coroutine> parryReleaseCoroutines = new();

    private readonly Queue<VisualEffect> laserPool = new();
    private readonly List<VisualEffect> laserActive = new();
    private readonly Dictionary<VisualEffect, Coroutine> laserReleaseCoroutines = new();

    private VFXRenderer DashRenderer;

    protected override void SingletonAwake()
    {
        parryTemplate.gameObject.SetActive(false);
        laserTemplate.gameObject.SetActive(false);
        ControlCounterParryCharge(false);

        for (int i = 0; i < parryPrewarmCount; i++)
        {
            VisualEffect vfx = CreateParryInstance();
            ReturnParryInstance(vfx);
        }

        for (int i = 0; i < laserPrewarmCount; i++)
        {
            VisualEffect vfx = CreateLaserInstance();
            ReturnLaserInstance(vfx);
        }

        DashRenderer = dash.GetComponent<VFXRenderer>();
        StopDash();
    }

    #region VFX Controls

    public void PlayParry()
    {
        VisualEffect vfx = parryPool.Count > 0 ? parryPool.Dequeue() : CreateParryInstance();
        parryActive.Add(vfx);

        Transform tr = vfx.transform;
        tr.SetParent(followPlayerRoot, false);
        vfx.gameObject.SetActive(true);
        vfx.Reinit();
        vfx.Play();

        Coroutine co = StartCoroutine(CoAutoReleaseParry(vfx));
        parryReleaseCoroutines[vfx] = co;
    }

    public void StopAllParry()
    {
        for (int i = parryActive.Count - 1; i >= 0; i--)
        {
            VisualEffect vfx = parryActive[i];
            ReturnParryInstance(vfx);
        }

        parryActive.Clear();
    }

    public void PlayDash() => Restart(dash);
    //public void PlayDash() => DashRenderer.enabled = true;

    public void StopDash() => dash.Stop();
    //public void StopDash() => DashRenderer.enabled = false;

    public void PlayHeal()
    {
        Restart(healing);
        Restart(healLine);
        Restart(healingUI);
    }

    public void StopHeal()
    {
        healing.Stop();
        healLine.Stop();
        healingUI.Stop();
    }

    public void ControlCounterParryCharge(bool play)
    {
        if (counterParryCharge.activeSelf == play) return;

        counterParryCharge.SetActive(play);
        counterParryChargeUI.SetActive(play);
    }

    public void PlayCounterParry() => Restart(counterParry);

    public void PlayCounterParrySuccess() => Restart(counterParrySuccess);

    public void PlayHit() => Restart(hit);

    public void PlayLaser(float zRotationDegrees)
    {
        VisualEffect vfx = laserPool.Count > 0 ? laserPool.Dequeue() : CreateLaserInstance();
        laserActive.Add(vfx);

        Transform tr = vfx.transform;
        tr.SetParent(followEnemyRoot, false);
        tr.SetLocalPositionAndRotation(laserLocalOffset, Quaternion.Euler(0f, 0f, zRotationDegrees));
        vfx.gameObject.SetActive(true);
        vfx.Reinit();
        vfx.Play();

        Coroutine co = StartCoroutine(CoAutoReleaseLaser(vfx));
        laserReleaseCoroutines[vfx] = co;
    }

    public VisualEffect BeginLaser(float zRotationDegrees)
    {
        VisualEffect vfx = laserPool.Count > 0 ? laserPool.Dequeue() : CreateLaserInstance();
        laserActive.Add(vfx);

        Transform tr = vfx.transform;
        tr.SetParent(followEnemyRoot, false);
        tr.SetLocalPositionAndRotation(laserLocalOffset, Quaternion.Euler(0f, 0f, zRotationDegrees));
        vfx.gameObject.SetActive(true);
        vfx.Reinit();
        vfx.Play();

        return vfx;
    }

    public void UpdateLaser(VisualEffect vfx, float zRotationDegrees)
    {
        Transform tr = vfx.transform;
        tr.localRotation = Quaternion.Euler(0f, 0f, zRotationDegrees);
    }

    public void EndLaser(VisualEffect vfx)
    {
        laserActive.Remove(vfx);
        ReturnLaserInstance(vfx);
    }

    public void StopAllLaser()
    {
        for (int i = laserActive.Count - 1; i >= 0; i--)
        {
            VisualEffect vfx = laserActive[i];
            ReturnLaserInstance(vfx);
        }

        laserActive.Clear();
    }

    public void StopAllEffects()
    {
        StopAllParry();
        StopAllLaser();
        dash.Stop();
        healing.Stop();
        healLine.Stop();
        healingUI.Stop();
    }
    #endregion

    public void DoPerfectParryImpact() => Shake(perfectShakeDuration, perfectShakeAmplitude);

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
        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(true);

        seq.AppendCallback(() =>
        {
            slowmoOverlay.SetActive(true);
        });

        seq.Append(DOTween.To(() => Time.timeScale, x => Time.timeScale = x, extremeSlowScale, extremeSlowFadeTime).SetUpdate(true));

        seq.Append
        (
            DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 1f, extremeSlowFadeTime)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    slowmoOverlay.SetActive(false);
                })
        );
    }

    public void DoGameOverEffect()
    {
        InputManager.Instance.SetAllModes(InputMode.Auto);
        StopAllEffects();
        PlayerController.Instance.Sprite.sortingOrder = 10;
        gameOverOverlay.GetComponentInParent<Canvas>().sortingOrder = 9;

        StartCoroutine(CoGameOverEffect());
    }

    private IEnumerator CoGameOverEffect()
    {
        PlayHit();
        yield return null;

        Transform camTr = targetCamera.transform;
        camTr.DOKill(true);

        CanvasGroup overlayGroup = gameOverOverlay.GetComponent<CanvasGroup>();
        CanvasGroup buttonsGroup = GameOverOptionButtons.GetComponent<CanvasGroup>();

        gameOverOverlay.SetActive(true);
        overlayGroup.alpha = 0f;

        GameOverOptionButtons.SetActive(false);
        buttonsGroup.alpha = 0f;

        Vector3 camPos = camTr.position;
        Vector3 playerPos = PlayerController.Instance.transform.position;
        Vector3 targetPos = new(playerPos.x, playerPos.y - 0.65f, camPos.z);

        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(true);

        seq.AppendCallback(() =>
        {
            Time.timeScale = 0f;
        });

        seq.AppendInterval(0.5f);

        seq.AppendCallback(() =>
        {
            Time.timeScale = 1f;
        });

        seq.Append(overlayGroup.DOFade(1f, 1f).SetEase(Ease.Linear).SetUpdate(true));
        seq.Join(camTr.DOMove(targetPos, 1f).SetEase(Ease.OutQuad).SetUpdate(true));
        seq.Join(targetCamera.DOOrthoSize(2f, 1f).SetEase(Ease.OutQuad).SetUpdate(true));

        seq.AppendCallback(() =>
        {
            GameOverOptionButtons.SetActive(true);
            buttonsGroup.alpha = 0f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        });

        seq.Append(buttonsGroup.DOFade(1f, 0.5f).SetEase(Ease.Linear).SetUpdate(true));
    }

    private void Restart(VisualEffect vfx)
    {
        vfx.Stop();
        vfx.Reinit();
        vfx.Play();
    }

    private VisualEffect CreateParryInstance()
    {
        VisualEffect vfx = Instantiate(parryTemplate, followPlayerRoot, false);
        vfx.gameObject.SetActive(false);
        return vfx;
    }

    private IEnumerator CoAutoReleaseParry(VisualEffect vfx)
    {
        float elapsed = 0f;
        bool hadAlive = false;

        while (elapsed < parryAutoReleaseTimeout)
        {
            int alive = vfx.aliveParticleCount;

            if (alive > 0)
                hadAlive = true;

            if (hadAlive && alive == 0)
                break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        parryReleaseCoroutines.Remove(vfx);
        parryActive.Remove(vfx);
        ReturnParryInstance(vfx);
    }

    private void ReturnParryInstance(VisualEffect vfx)
    {
        if (parryReleaseCoroutines.TryGetValue(vfx, out Coroutine co))
        {
            StopCoroutine(co);
            parryReleaseCoroutines.Remove(vfx);
        }

        vfx.Stop();
        vfx.gameObject.SetActive(false);
        parryPool.Enqueue(vfx);
    }

    private VisualEffect CreateLaserInstance()
    {
        VisualEffect vfx = Instantiate(laserTemplate, followEnemyRoot, false);
        vfx.gameObject.SetActive(false);
        return vfx;
    }

    private IEnumerator CoAutoReleaseLaser(VisualEffect vfx)
    {
        float elapsed = 0f;
        bool hadAlive = false;

        while (elapsed < laserAutoReleaseTimeout)
        {
            int alive = vfx.aliveParticleCount;

            if (alive > 0)
                hadAlive = true;

            if (hadAlive && alive == 0)
                break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        laserReleaseCoroutines.Remove(vfx);
        laserActive.Remove(vfx);
        ReturnLaserInstance(vfx);
    }

    private void ReturnLaserInstance(VisualEffect vfx)
    {
        if (laserReleaseCoroutines.TryGetValue(vfx, out Coroutine co))
        {
            StopCoroutine(co);
            laserReleaseCoroutines.Remove(vfx);
        }

        vfx.Stop();
        vfx.gameObject.SetActive(false);
        laserPool.Enqueue(vfx);
    }

    private void Shake(float duration, float amplitude)
    {
        Transform camTr = targetCamera.transform;
        Vector3 strength = new(amplitude, amplitude, 0f);

        camTr.DOShakePosition(duration, strength, 20, 90f, false, true).SetUpdate(true);
    }
}