using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class TutorialPanel : MonoBehaviour
{
    [SerializeField, Required] private CanvasGroup canvasGroup;

    [BoxGroup("UI"), SerializeField, Required]
    private KeyBindingVisual visual;

    [BoxGroup("Rebind Target"), SerializeField]
    private string mapName;

    [BoxGroup("Rebind Target"), SerializeField]
    private string actionName;

    [BoxGroup("Rebind Target"), SerializeField, Min(0)]
    private int bindingIndex = 0;

    private Tween fadeTween;

    private void OnEnable() => visual.Apply(InputManager.Instance.Actions.FindAction(mapName + "/" + actionName), bindingIndex);
    private void Update() => visual.Apply(InputManager.Instance.Actions.FindAction(mapName + "/" + actionName), bindingIndex);

    public Tween ShowFadeIn(float seconds)
    {
        KillFade();

        if (!gameObject.activeSelf) gameObject.SetActive(true);

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        fadeTween = canvasGroup
            .DOFade(1f, seconds)
            .SetEase(Ease.Linear)
            .SetUpdate(true);

        return fadeTween;
    }

    public void ShowImmediate()
    {
        KillFade();

        if (!gameObject.activeSelf) gameObject.SetActive(true);

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public Tween HideFadeOut(float seconds)
    {
        KillFade();

        if (!gameObject.activeSelf) gameObject.SetActive(true);

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        fadeTween = canvasGroup
            .DOFade(0f, seconds)
            .SetEase(Ease.Linear)
            .SetUpdate(true);

        return fadeTween;
    }

    public void HideImmediate()
    {
        KillFade();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    private void KillFade()
    {
        if (fadeTween == null) return;
        if (!fadeTween.IsActive()) return;

        fadeTween.Kill(false);
        fadeTween = null;
    }

    private void OnDisable() => KillFade();
}