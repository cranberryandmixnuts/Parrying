using UnityEngine;

public sealed class FollowObject : MonoBehaviour
{
    [SerializeField] private Transform target;

    private void Update()
    {
        transform.position = target.position;
        transform.rotation = target.rotation;
        transform.localScale = target.localScale;
    }
}
