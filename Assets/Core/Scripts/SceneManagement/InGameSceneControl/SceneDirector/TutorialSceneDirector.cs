using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class TutorialSceneDirector : Singleton<TutorialSceneDirector, SceneScope>
{
    [System.Serializable]
    private sealed class UIInfoStep
    {
        [SerializeField, Required]
        public GameObject Root;

        [SerializeField, Required]
        public Transform Highlight;

        [SerializeField, Required]
        public TMP_Text Text;

        [SerializeField, Required]
        public Button OkButton;

        [SerializeField, Required]
        public CanvasGroup OkButtonCanvasGroup;

        [SerializeField, Min(0f)]
        public float HighlightDefaultScale = 18f;
    }

    [TabGroup("Tutorial", "Refs"), BoxGroup("Tutorial/Refs/Scene"), SerializeField, Required]
    private TutorialSeeker seeker;

    [TabGroup("Tutorial", "Refs"), BoxGroup("Tutorial/Refs/Scene"), SerializeField, Required]
    private CanvasGroup blackOverlay;

    [TabGroup("Tutorial", "Refs"), BoxGroup("Tutorial/Refs/Panels"), SerializeField, Required]
    private TutorialPanel parryPanel;

    [TabGroup("Tutorial", "Refs"), BoxGroup("Tutorial/Refs/Panels"), SerializeField, Required]
    private TutorialPanel imperfectParryPanel;

    [TabGroup("Tutorial", "Refs"), BoxGroup("Tutorial/Refs/Panels"), SerializeField, Required]
    private TutorialPanel dodgePanel;

    [TabGroup("Tutorial", "Refs"), BoxGroup("Tutorial/Refs/Panels"), SerializeField, Required]
    private TutorialPanel counterChargePanel;

    [TabGroup("Tutorial", "Refs"), BoxGroup("Tutorial/Refs/Panels"), SerializeField, Required]
    private TutorialPanel counterParryPanel;

    [TabGroup("Tutorial", "Refs"), BoxGroup("Tutorial/Refs/Panels"), SerializeField, Required]
    private TutorialPanel healPanel;

    [TabGroup("Tutorial", "Refs"), BoxGroup("Tutorial/Refs/Info Steps"), SerializeField, Required]
    private UIInfoStep imperfectParryEffectStep;

    [TabGroup("Tutorial", "Refs"), BoxGroup("Tutorial/Refs/Info Steps"), SerializeField, Required]
    private UIInfoStep energySystemStep;

    [TabGroup("Tutorial", "Refs"), BoxGroup("Tutorial/Refs/Info Steps"), SerializeField, Required]
    private UIInfoStep hpSystemStep;

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

    [TabGroup("Tutorial", "Timing"), BoxGroup("Tutorial/Timing/Imperfect Parry"), SerializeField, Min(0f)]
    private float imperfectParryFireDelaySeconds = 0.3f;

    [TabGroup("Tutorial", "Timing"), BoxGroup("Tutorial/Timing/Imperfect Parry"), SerializeField, Min(0f)]
    private float imperfectParryPreSlowDelaySeconds = 0.35f;

    [TabGroup("Tutorial", "Timing"), BoxGroup("Tutorial/Timing/Imperfect Parry"), SerializeField, Min(0f)]
    private float imperfectParrySlowSeconds = 1f;

    [TabGroup("Tutorial", "Timing"), BoxGroup("Tutorial/Timing/Imperfect Parry"), SerializeField, Min(0f)]
    private float afterImperfectParryDelaySeconds = 0.25f;

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

    [TabGroup("Tutorial", "UI"), BoxGroup("Tutorial/UI/Info Steps"), SerializeField, Min(0f)]
    private float infoHighlightScaleSeconds = 0.3f;

    [TabGroup("Tutorial", "UI"), BoxGroup("Tutorial/UI/Info Steps"), SerializeField, Min(0f)]
    private float infoTextFadeInSeconds = 0.5f;

    [TabGroup("Tutorial", "UI"), BoxGroup("Tutorial/UI/Info Steps"), SerializeField, Min(0f)]
    private float infoOkButtonFadeInSeconds = 0.5f;

    private PlayerController player;
    private InputManager input;

    private float baseFixedDeltaTime;

    private Coroutine routine;

    private Tween timeScaleTween;
    private Tween overlayTween;

    private Button currentOkButton;
    private UnityAction currentOkListener;

    private void Start()
    {
        player = PlayerController.Instance;
        input = InputManager.Instance;

        baseFixedDeltaTime = Time.fixedDeltaTime;

        Time.timeScale = 1f;
        Time.fixedDeltaTime = baseFixedDeltaTime;

        HideOverlayImmediate();
        HideAllPanelsImmediate();
        HideAllInfoStepsImmediate();

        routine = StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        input.SetAllModes(InputMode.Auto);
        InputModeState newState = input.GetModes();

        yield return new WaitForSecondsRealtime(firstFireDelaySeconds);
        seeker.FireAtPlayer();

        yield return new WaitForSecondsRealtime(firstHitRecoverDelaySeconds);

        yield return new WaitForSecondsRealtime(parryFireDelaySeconds);

        input.SetAllModes(InputMode.Auto);
        newState = input.GetModes();
        newState.ParryDown = InputMode.Manual;

        yield return RunTimeStopInputStep(
            parryPanel,
            newState,
            parryPreSlowDelaySeconds,
            parrySlowSeconds,
            () => input.ParryDown
        );

        yield return new WaitForSecondsRealtime(afterParryDelaySeconds);

        yield return new WaitForSecondsRealtime(imperfectParryFireDelaySeconds);

        input.SetAllModes(InputMode.Auto);
        newState = input.GetModes();
        newState.ParryDown = InputMode.Manual;

        yield return RunTimeStopInputStep(
            imperfectParryPanel,
            newState,
            imperfectParryPreSlowDelaySeconds,
            imperfectParrySlowSeconds,
            () => input.ParryDown
        );

        yield return new WaitForSecondsRealtime(afterImperfectParryDelaySeconds);

        yield return RunUIInfoStep(imperfectParryEffectStep);

        yield return new WaitForSecondsRealtime(dodgeFireDelaySeconds);

        input.SetAllModes(InputMode.Auto);
        newState = input.GetModes();
        newState.DashDown = InputMode.Manual;

        yield return RunTimeStopInputStep(
            dodgePanel,
            newState,
            dodgePreSlowDelaySeconds,
            dodgeSlowSeconds,
            () => input.DashDown
        );

        yield return new WaitForSecondsRealtime(afterDodgeDelaySeconds);

        yield return RunUIInfoStep(energySystemStep);

        yield return new WaitForSecondsRealtime(counterChargePanelDelaySeconds);
        yield return ShowPanel(counterChargePanel);

        input.SetAllModes(InputMode.Auto);
        newState = input.GetModes();
        newState.ParryHeld = InputMode.Manual;
        input.SetModes(newState);

        yield return new WaitUntil(() => player.inCounterParryPrep);

        input.SetAllModes(InputMode.Auto);
        input.SetAutoHeld(ActionKey.Parry, true);
        counterChargePanel.HideImmediate();

        yield return new WaitForSecondsRealtime(afterCounterChargeDelaySeconds);

        yield return new WaitForSecondsRealtime(counterParryFireDelaySeconds);

        newState = input.GetModes();
        newState.ParryHeld = InputMode.Manual;

        yield return RunTimeStopInputStep(
            counterParryPanel,
            newState,
            counterParryPreSlowDelaySeconds,
            counterParrySlowSeconds,
            () => !input.ParryHeld
        );

        yield return new WaitForSecondsRealtime(afterCounterParryDelaySeconds);

        yield return RunUIInfoStep(hpSystemStep);

        yield return new WaitForSecondsRealtime(healPanelDelaySeconds);
        yield return ShowPanel(healPanel);

        input.SetMode(ActionKey.Heal, InputMode.Manual);

        yield return new WaitUntil(() => player.Vitals.Health >= player.Vitals.MaxHealth);

        healPanel.HideImmediate();
        input.SetAllModes(InputMode.Manual);

        Debug.Log("Scene End");
    }

    private IEnumerator RunUIInfoStep(UIInfoStep step)
    {
        input.SetCusorMode(true);
        input.SetAllModes(InputMode.Auto);

        step.Root.SetActive(true);

        step.Highlight.localScale = Vector3.one * step.HighlightDefaultScale;

        step.Text.alpha = 0f;

        step.OkButtonCanvasGroup.alpha = 0f;
        step.OkButtonCanvasGroup.interactable = false;
        step.OkButtonCanvasGroup.blocksRaycasts = false;
        step.OkButton.interactable = false;
        step.OkButton.gameObject.SetActive(false);

        Tween highlightTween = step.Highlight
            .DOScale(Vector3.one, infoHighlightScaleSeconds)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        yield return highlightTween.WaitForCompletion();

        Tween textTween = step.Text
            .DOFade(1f, infoTextFadeInSeconds)
            .SetEase(Ease.Linear)
            .SetUpdate(true);

        yield return textTween.WaitForCompletion();

        step.OkButton.gameObject.SetActive(true);

        Tween buttonTween = step.OkButtonCanvasGroup
            .DOFade(1f, infoOkButtonFadeInSeconds)
            .SetEase(Ease.Linear)
            .SetUpdate(true);

        yield return buttonTween.WaitForCompletion();

        step.OkButtonCanvasGroup.interactable = true;
        step.OkButtonCanvasGroup.blocksRaycasts = true;
        step.OkButton.interactable = true;

        bool pressed = false;

        ClearCurrentOkListener();
        currentOkButton = step.OkButton;
        currentOkListener = () => pressed = true;
        currentOkButton.onClick.AddListener(currentOkListener);

        yield return new WaitUntil(() => pressed);

        input.SetCusorMode(false);
        ClearCurrentOkListener();
        ResetInfoStepImmediate(step);
    }

    private IEnumerator RunTimeStopInputStep(
        TutorialPanel panel,
        InputModeState state,
        float preSlowDelaySeconds,
        float slowSeconds,
        System.Func<bool> triggerCondition
    )
    {
        seeker.FireAtPlayer();

        yield return new WaitForSecondsRealtime(preSlowDelaySeconds);
        yield return SlowToStop(slowSeconds);

        yield return ShowPanel(panel);

        input.SetModes(state);

        yield return new WaitUntil(triggerCondition);

        input.SetAllModes(InputMode.Auto);

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

    private IEnumerator ShowPanel(TutorialPanel panel)
    {
        Tween t = panel.ShowFadeIn(panelFadeInSeconds);
        yield return t.WaitForCompletion();
    }

    private void HideAllPanelsImmediate()
    {
        parryPanel.HideImmediate();
        imperfectParryPanel.HideImmediate();
        dodgePanel.HideImmediate();
        counterChargePanel.HideImmediate();
        counterParryPanel.HideImmediate();
        healPanel.HideImmediate();
    }

    private void HideAllInfoStepsImmediate()
    {
        ResetInfoStepImmediate(imperfectParryEffectStep);
        ResetInfoStepImmediate(energySystemStep);
        ResetInfoStepImmediate(hpSystemStep);
    }

    private void ResetInfoStepImmediate(UIInfoStep step)
    {
        step.Root.SetActive(false);

        step.Highlight.localScale = Vector3.one * step.HighlightDefaultScale;
        step.Text.alpha = 0f;

        step.OkButtonCanvasGroup.alpha = 0f;
        step.OkButtonCanvasGroup.interactable = false;
        step.OkButtonCanvasGroup.blocksRaycasts = false;
        step.OkButton.interactable = false;
        step.OkButton.gameObject.SetActive(false);
    }

    private void HideOverlayImmediate()
    {
        blackOverlay.alpha = 0f;
        blackOverlay.gameObject.SetActive(false);
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

    private void ClearCurrentOkListener()
    {
        if (currentOkButton == null || currentOkListener == null)
            return;

        currentOkButton.onClick.RemoveListener(currentOkListener);
        currentOkButton = null;
        currentOkListener = null;
    }

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);

        ClearCurrentOkListener();
    }
}