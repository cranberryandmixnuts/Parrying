using System.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class GameStartSceneDirector : Singleton<GameStartSceneDirector, SceneScope>
{
    [BoxGroup("Panels"), SerializeField, Required]
    private TutorialPanel moveTutorialLeft;

    [BoxGroup("Panels"), SerializeField, Required]
    private TutorialPanel moveTutorialRight;

    [BoxGroup("Panels"), SerializeField, Required]
    private TutorialPanel jumpTutorial;

    [BoxGroup("Timings"), SerializeField, Min(0f)]
    private float jumpFadeOutSeconds = 0.5f;

    [BoxGroup("Input"), SerializeField, Min(0f)]
    private float moveDetectThreshold = 0.01f;

    private Coroutine routine;

    private void Start()
    {
        jumpTutorial.HideImmediate();

        InputManager.Instance.SetAllModes(InputMode.Auto);

        routine = StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        while (SceneLoader.Instance.IsTransitioning)
            yield return null;

        InputManager.Instance.SetMode(ActionKey.Move, InputMode.Manual);
        InputManager.Instance.SetMode(ActionKey.Jump, InputMode.Manual);
        InputManager.Instance.SetMode(ActionKey.Escape, InputMode.Manual);

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

        Debug.Log("Scene End");
    }

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
    }
}