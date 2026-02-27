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
    private InputManager input;
    private PlayerController player;

    private void Start()
    {
        input = InputManager.Instance;
        player = PlayerController.Instance;

        jumpTutorial.HideImmediate();

        input.SetAllModes(InputMode.Auto);
        player.Settings.InitializePlayerStatus();
        int startEnergy = 
            player.Settings.perfectParryEnergyGain + 
            player.Settings.imperfectParryEnergyGain + 
            player.Settings.extremeDashEnergyGainPerDetect;
        player.Vitals.TryConsumeEnergy(startEnergy);

        routine = StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        yield return new WaitUntil(() => !SceneLoader.Instance.IsTransitioning);

        input.SetMode(ActionKey.Move, InputMode.Manual);
        input.SetMode(ActionKey.Jump, InputMode.Manual);
        input.SetMode(ActionKey.Escape, InputMode.Manual);

        yield return new WaitUntil(() => Mathf.Abs(input.MoveAxis) > moveDetectThreshold);

        moveTutorialLeft.HideImmediate();
        moveTutorialRight.HideImmediate();

        jumpTutorial.ShowImmediate();

        yield return new WaitUntil(() => input.JumpDown);

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