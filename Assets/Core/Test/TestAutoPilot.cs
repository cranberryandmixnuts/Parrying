using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;

// 일정 시간 오른쪽 이동 → 정지 → 점프(짧게 홀드) → 다시 수동 복구
// 자동 입력모드 테스트 코드
public sealed class TestAutoPilot : MonoBehaviour
{
    [Header("Auto Pilot Settings")]
    [SerializeField] private float walkSeconds = 1f;
    [SerializeField] private float pauseSeconds = 1f;
    [SerializeField] private float jumpHoldSeconds = 0.3f;

    private InputManager input;
    private Sequence sequence;

    private void Start() => input = InputManager.Instance;

    [Button, DisableInEditorMode]
    public void StartAutoPilot()
    {
        if (input == null) return;

        input.SetMode(ActionKey.Move, InputMode.Auto);
        input.SetMode(ActionKey.Jump, InputMode.Auto);

        input.SetAutoMoveAxis(0f);
        input.SetAutoHeld(ActionKey.Jump, false);

        sequence = DOTween.Sequence();

        sequence.AppendCallback(() => input.SetAutoMoveAxis(1f));
        sequence.AppendInterval(walkSeconds);

        sequence.AppendCallback(() => input.SetAutoMoveAxis(0f));
        sequence.AppendInterval(pauseSeconds);

        sequence.AppendCallback(() =>
        {
            input.TriggerAutoDown(ActionKey.Jump);
            input.SetAutoHeld(ActionKey.Jump, true);
        });

        sequence.AppendInterval(jumpHoldSeconds);
        sequence.AppendCallback(() => input.SetAutoHeld(ActionKey.Jump, false));

        sequence.OnComplete(() =>
        {
            EndCutsceneAuto();
            enabled = false;
        });
    }

    private void EndCutsceneAuto()
    {
        input.SetMode(ActionKey.Move, InputMode.Manual);
        input.SetMode(ActionKey.Jump, InputMode.Manual);
    }
}