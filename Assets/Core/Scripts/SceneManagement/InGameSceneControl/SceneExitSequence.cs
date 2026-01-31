using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class SceneExitSequence : MonoBehaviour
{
    [SerializeField, Required] private Collider2D col;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private SceneType nextScene = SceneType.None;
    [SerializeField, Min(0f)] private float moveAxis = 1f;

    private bool triggered;
    private Coroutine routine;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & playerMask.value) == 0) return;

        if (triggered) return;
        triggered = true;

        InputManager.Instance.SetAllModes(InputMode.Auto);

        routine = StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        PlayerController.Instance.Rigidbody.linearVelocity = Vector2.zero;

        while (!PlayerController.Instance.isGround)
            yield return null;

        InputManager.Instance.SetAutoMoveAxis(moveAxis);
        SceneLoader.Instance.LoadScene(nextScene);
    }

    private void OnDisable()
    {
        if (triggered && SceneLoader.Instance.IsTransitioning)
            InputManager.Instance.SetAllModes(InputMode.Manual);

        if (routine != null) StopCoroutine(routine);
    }
}