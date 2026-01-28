using System.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class GameStartInputTutorialDirector : MonoBehaviour
{
    [BoxGroup("Panels"), SerializeField, Required]
    private TutorialPanel moveTutorialLeft;

    [BoxGroup("Panels"), SerializeField, Required]
    private TutorialPanel moveTutorialRight;

    [BoxGroup("Panels"), SerializeField, Required]
    private TutorialPanel jumpTutorial;

    [BoxGroup("Timings"), SerializeField, Min(0f)]
    private float moveFadeInSeconds = 0.5f;

    [BoxGroup("Timings"), SerializeField, Min(0f)]
    private float jumpFadeOutSeconds = 0.5f;

    [BoxGroup("Input"), SerializeField, Min(0f)]
    private float moveDetectThreshold = 0.01f;

    private Coroutine routine;

    private void Start()
    {
        moveTutorialLeft.HideImmediate();
        moveTutorialRight.HideImmediate();
        jumpTutorial.HideImmediate();

        routine = StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        while (SceneLoader.Instance.IsTransitioning)
            yield return null;

        Tween showLeft = moveTutorialLeft.ShowFadeIn(moveFadeInSeconds);
        Tween showRight = moveTutorialRight.ShowFadeIn(moveFadeInSeconds);
        yield return DOTween.Sequence().Join(showLeft).Join(showRight).WaitForCompletion();

        while (Mathf.Abs(InputManager.Instance.MoveAxis) <= moveDetectThreshold)
            yield return null;

        moveTutorialLeft.HideImmediate();
        moveTutorialRight.HideImmediate();

        jumpTutorial.ShowImmediate();

        while (!InputManager.Instance.JumpDown)
            yield return null;

        Tween t = jumpTutorial.HideFadeOut(jumpFadeOutSeconds);
        yield return t.WaitForCompletion();

        jumpTutorial.HideImmediate();
    }

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
    }
}