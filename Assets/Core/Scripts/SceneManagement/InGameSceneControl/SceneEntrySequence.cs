using System.Collections;
using UnityEngine;

public sealed class SceneEntrySequence : MonoBehaviour
{
    [SerializeField, Min(0f)] private float preDelaySeconds = 0f;
    [SerializeField, Min(0f)] private float walkSeconds = 0.5f;
    [SerializeField, Min(0f)] private float postDelaySeconds = 0f;
    [SerializeField] private float moveAxis = 1f;
    [SerializeField] private bool unlockInputAtEnd = true;

    private void Start() => StartCoroutine(Sequence());

    private IEnumerator Sequence()
    {
        InputManager.Instance.SetAllModes(InputMode.Auto);
        InputManager.Instance.SetMode(ActionKey.Escape, InputMode.Manual);

        yield return new WaitForSeconds(preDelaySeconds);

        InputManager.Instance.SetAutoMoveAxis(moveAxis);

        yield return new WaitForSeconds(walkSeconds);

        InputManager.Instance.SetAutoMoveAxis(0f);

        yield return new WaitForSeconds(postDelaySeconds);

        if (unlockInputAtEnd)
            InputManager.Instance.SetAllModes(InputMode.Manual);

        enabled = false;
    }
}