using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialSceneDirector : MonoBehaviour
{
    [TabGroup("Tutorial", "Refs"), BoxGroup("Tutorial/Refs/Scene"), SerializeField, Required]
    private TutorialSeeker seeker;

    [TabGroup("Tutorial", "Refs"), BoxGroup("Tutorial/Refs/Scene"), SerializeField, Required]
    private Camera worldCamera;

    [TabGroup("Tutorial", "Refs"), BoxGroup("Tutorial/Refs/Scene"), SerializeField, Required]
    private CanvasGroup blackOverlay;

    [TabGroup("Tutorial", "Refs"), BoxGroup("Tutorial/Refs/Panels"), SerializeField, Required]
    private TutorialPanel parryPanel;

    [TabGroup("Tutorial", "Refs"), BoxGroup("Tutorial/Refs/Panels"), SerializeField, Required]
    private TutorialPanel dodgePanel;

    [TabGroup("Tutorial", "Refs"), BoxGroup("Tutorial/Refs/Panels"), SerializeField, Required]
    private TutorialPanel counterChargePanel;

    [TabGroup("Tutorial", "Refs"), BoxGroup("Tutorial/Refs/Panels"), SerializeField, Required]
    private TutorialPanel counterParryPanel;

    [TabGroup("Tutorial", "Refs"), BoxGroup("Tutorial/Refs/Panels"), SerializeField, Required]
    private TutorialPanel healPanel;

    [TabGroup("Tutorial", "Camera"), SerializeField, Min(0f)]
    private float entryOrthoSize = 2f;

    [TabGroup("Tutorial", "Camera"), SerializeField, Min(0f)]
    private float defaultOrthoSize = 5f;

    [TabGroup("Tutorial", "Camera"), SerializeField]
    private Vector2 defaultCameraXY = Vector2.zero;

    [TabGroup("Tutorial", "Camera"), SerializeField, Min(0f)]
    private float cameraResetSeconds = 0.6f;

    [TabGroup("Tutorial", "Timing"), BoxGroup("Tutorial/Timing/Entry"), SerializeField, Min(0f)]
    private float firstFireDelaySeconds = 0.4f;

    [TabGroup("Tutorial", "Timing"), BoxGroup("Tutorial/Timing/Entry"), SerializeField, Min(0f)]
    private float firstHitRecoverDelaySeconds = 0.8f;

    [TabGroup("Tutorial", "Timing"), BoxGroup("Tutorial/Timing/Parry"), SerializeField, Min(0f)]
    private float parryFireDelaySeconds = 0.3f;

    [TabGroup("Tutorial", "Timing"), BoxGroup("Tutorial/Timing/Parry"), SerializeField, Min(0f)]
    private float parryPreSlowDelaySeconds = 0.35f;

    [TabGroup("Tutorial", "Timing"), BoxGroup("Tutorial/Timing/Parry"), SerializeField, Min(0f)]
    private float parrySlowSeconds = 1f;

    [TabGroup("Tutorial", "Timing"), BoxGroup("Tutorial/Timing/Parry"), SerializeField, Min(0f)]
    private float afterParryDelaySeconds = 0.25f;

    [TabGroup("Tutorial", "Timing"), BoxGroup("Tutorial/Timing/Dodge"), SerializeField, Min(0f)]
    private float dodgeFireDelaySeconds = 0.3f;

    [TabGroup("Tutorial", "Timing"), BoxGroup("Tutorial/Timing/Dodge"), SerializeField, Min(0f)]
    private float dodgePreSlowDelaySeconds = 0.35f;

    [TabGroup("Tutorial", "Timing"), BoxGroup("Tutorial/Timing/Dodge"), SerializeField, Min(0f)]
    private float dodgeSlowSeconds = 1f;

    [TabGroup("Tutorial", "Timing"), BoxGroup("Tutorial/Timing/Dodge"), SerializeField, Min(0f)]
    private float afterDodgeDelaySeconds = 0.25f;

    [TabGroup("Tutorial", "Timing"), BoxGroup("Tutorial/Timing/Counter Charge"), SerializeField, Min(0f)]
    private float counterChargePanelDelaySeconds = 0.2f;

    [TabGroup("Tutorial", "Timing"), BoxGroup("Tutorial/Timing/Counter Charge"), SerializeField, Min(0f)]
    private float afterCounterChargeDelaySeconds = 0.2f;

    [TabGroup("Tutorial", "Timing"), BoxGroup("Tutorial/Timing/Counter Parry"), SerializeField, Min(0f)]
    private float counterParryFireDelaySeconds = 0.2f;

    [TabGroup("Tutorial", "Timing"), BoxGroup("Tutorial/Timing/Counter Parry"), SerializeField, Min(0f)]
    private float counterParryPreSlowDelaySeconds = 0.35f;

    [TabGroup("Tutorial", "Timing"), BoxGroup("Tutorial/Timing/Counter Parry"), SerializeField, Min(0f)]
    private float counterParrySlowSeconds = 1f;

    [TabGroup("Tutorial", "Timing"), BoxGroup("Tutorial/Timing/Counter Parry"), SerializeField, Min(0f)]
    private float afterCounterParryDelaySeconds = 0.25f;

    [TabGroup("Tutorial", "Timing"), BoxGroup("Tutorial/Timing/Heal"), SerializeField, Min(0f)]
    private float healPanelDelaySeconds = 0.2f;

    [TabGroup("Tutorial", "UI"), SerializeField, Min(0f)]
    private float panelFadeInSeconds = 0.5f;

    private float baseFixedDeltaTime;
    private bool followPlayer;
    private float camZ;

    private Coroutine routine;

    private Tween timeScaleTween;
    private Tween overlayTween;
    private Tween cameraTween;

    private void Start()
    {
        baseFixedDeltaTime = Time.fixedDeltaTime;
        camZ = worldCamera.transform.position.z;

        Time.timeScale = 1f;
        Time.fixedDeltaTime = baseFixedDeltaTime;

        HideOverlayImmediate();
        HideAllPanelsImmediate();

        worldCamera.orthographicSize = entryOrthoSize;
        followPlayer = true;

        routine = StartCoroutine(Run());
    }

    private void LateUpdate()
    {
        if (!followPlayer) return;

        Transform p = PlayerController.Instance.transform;
        worldCamera.transform.position = new Vector3(p.position.x, p.position.y, camZ);
    }

    private IEnumerator Run()
    {
        InputManager.Instance.SetAllModes(InputMode.Auto);

        yield return new WaitForSecondsRealtime(firstFireDelaySeconds);
        seeker.FireAtPlayer();

        yield return new WaitForSecondsRealtime(firstHitRecoverDelaySeconds);

        followPlayer = false;

        yield return TweenCameraToDefault().WaitForCompletion();

        yield return new WaitForSecondsRealtime(parryFireDelaySeconds);
        yield return RunTimeStopInputStep(
            parryPanel,
            ActionKey.Parry,
            ActionSignal.Down,
            parryPreSlowDelaySeconds,
            parrySlowSeconds,
            () => InputManager.Instance.ParryDown
        );

        yield return new WaitForSecondsRealtime(afterParryDelaySeconds);

        yield return new WaitForSecondsRealtime(dodgeFireDelaySeconds);
        yield return RunTimeStopInputStep(
            dodgePanel,
            ActionKey.Dash,
            ActionSignal.Down,
            dodgePreSlowDelaySeconds,
            dodgeSlowSeconds,
            () => InputManager.Instance.DashDown
        );

        yield return new WaitForSecondsRealtime(afterDodgeDelaySeconds);

        yield return new WaitForSecondsRealtime(counterChargePanelDelaySeconds);
        yield return ShowPanel(counterChargePanel);

        InputManager.Instance.SetMode(ActionKey.Parry, ActionSignal.Held, InputMode.Manual);

        while (!PlayerController.Instance.inCounterParryPrep)
            yield return null;

        InputManager.Instance.SetMode(ActionKey.Parry, ActionSignal.Held, InputMode.Auto);
        InputManager.Instance.SetAutoHeld(ActionKey.Parry, true);
        counterChargePanel.HideImmediate();

        yield return new WaitForSecondsRealtime(afterCounterChargeDelaySeconds);

        yield return new WaitForSecondsRealtime(counterParryFireDelaySeconds);
        yield return RunTimeStopInputStep(
            counterParryPanel,
            ActionKey.Parry,
            ActionSignal.Held,
            counterParryPreSlowDelaySeconds,
            counterParrySlowSeconds,
            () => !InputManager.Instance.ParryHeld
        );

        yield return new WaitForSecondsRealtime(afterCounterParryDelaySeconds);

        yield return new WaitForSecondsRealtime(healPanelDelaySeconds);
        yield return ShowPanel(healPanel);

        InputManager.Instance.SetMode(ActionKey.Heal, ActionSignal.Held, InputMode.Manual);

        PlayerVitals vitals = PlayerController.Instance.Vitals;
        while (vitals.Health < vitals.MaxHealth)
            yield return null;

        healPanel.HideImmediate();
        InputManager.Instance.SetAllModes(InputMode.Manual);

        gameObject.SetActive(false);
    }

    private IEnumerator RunTimeStopInputStep(
        TutorialPanel panel,
        ActionKey key,
        ActionSignal signal,
        float preSlowDelaySeconds,
        float slowSeconds,
        System.Func<bool> triggerCondition
    )
    {
        seeker.FireAtPlayer();

        yield return new WaitForSecondsRealtime(preSlowDelaySeconds);
        yield return SlowToStop(slowSeconds);

        yield return ShowPanel(panel);

        InputManager.Instance.SetMode(key, signal, InputMode.Manual);

        while (!triggerCondition())
            yield return null;

        InputManager.Instance.SetMode(key, signal, InputMode.Auto);

        ResumeTimeImmediate();

        panel.HideImmediate();
        HideOverlayImmediate();
    }

    private IEnumerator SlowToStop(float seconds)
    {
        KillTimeTweens();

        blackOverlay.gameObject.SetActive(true);
        blackOverlay.alpha = 0f;

        float start = 1f;
        SetTimeScale(start);

        overlayTween = blackOverlay
            .DOFade(1f, seconds)
            .SetEase(Ease.Linear)
            .SetUpdate(true);

        timeScaleTween = DOTween
            .To(() => Time.timeScale, v => SetTimeScale(v), 0f, seconds)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        yield return timeScaleTween.WaitForCompletion();

        SetTimeScale(0f);
    }

    private void ResumeTimeImmediate()
    {
        KillTimeTweens();
        SetTimeScale(1f);
    }

    private void SetTimeScale(float value)
    {
        Time.timeScale = Mathf.Clamp01(value);
        Time.fixedDeltaTime = baseFixedDeltaTime * Time.timeScale;
    }

    private Tween TweenCameraToDefault()
    {
        if (cameraTween != null && cameraTween.IsActive())
            cameraTween.Kill(false);

        Vector3 targetPos = new(defaultCameraXY.x, defaultCameraXY.y, camZ);

        cameraTween = DOTween.Sequence()
            .SetUpdate(true)
            .Join(worldCamera.DOOrthoSize(defaultOrthoSize, cameraResetSeconds).SetEase(Ease.OutQuad).SetUpdate(true))
            .Join(worldCamera.transform.DOMove(targetPos, cameraResetSeconds).SetEase(Ease.OutQuad).SetUpdate(true));

        return cameraTween;
    }

    private IEnumerator ShowPanel(TutorialPanel panel)
    {
        Tween t = panel.ShowFadeIn(panelFadeInSeconds);
        yield return t.WaitForCompletion();
    }

    private void HideAllPanelsImmediate()
    {
        parryPanel.HideImmediate();
        dodgePanel.HideImmediate();
        counterChargePanel.HideImmediate();
        counterParryPanel.HideImmediate();
        healPanel.HideImmediate();
    }

    private void HideOverlayImmediate()
    {
        if (blackOverlay.gameObject.activeSelf)
            blackOverlay.gameObject.SetActive(false);

        blackOverlay.alpha = 0f;
    }

    private void KillTimeTweens()
    {
        if (timeScaleTween != null && timeScaleTween.IsActive())
            timeScaleTween.Kill(false);

        if (overlayTween != null && overlayTween.IsActive())
            overlayTween.Kill(false);

        timeScaleTween = null;
        overlayTween = null;
    }

    private void OnDisable()
    {
        ResumeTimeImmediate();
        HideOverlayImmediate();
        HideAllPanelsImmediate();
        if (routine != null) StopCoroutine(routine);
    }
}