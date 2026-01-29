using System.Collections;
using UnityEngine;

public sealed class SceneEntrySequence : MonoBehaviour
{
    [SerializeField, Min(0f)] private float preDelaySeconds = 0f;
    [SerializeField, Min(0f)] private float walkSeconds = 0.5f;
    [SerializeField, Min(0f)] private float postDelaySeconds = 0f;
    [SerializeField] private float moveAxis = 1f;
    [SerializeField] private bool unlockInputAtEnd = true;

    private void Start()
    {
        InputManager.Instance.SetAllModes(InputMode.Auto);
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        if (preDelaySeconds > 0f)
            yield return new WaitForSeconds(preDelaySeconds);

        InputManager.Instance.SetAutoMoveAxis(moveAxis);

        if (walkSeconds > 0f)
            yield return new WaitForSeconds(walkSeconds);

        InputManager.Instance.SetAutoMoveAxis(0f);

        if (postDelaySeconds > 0f)
            yield return new WaitForSeconds(postDelaySeconds);

        if (unlockInputAtEnd)
            InputManager.Instance.SetAllModes(InputMode.Manual);

        enabled = false;
    }
}