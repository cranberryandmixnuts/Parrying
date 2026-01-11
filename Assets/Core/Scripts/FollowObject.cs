using UnityEngine;
using Sirenix.OdinInspector;

public sealed class FollowObject : MonoBehaviour
{
    [SerializeField, Required] private Transform target;

    private void Update()
    {
        transform.position = target.position;
        transform.rotation = target.rotation;
        transform.localScale = target.localScale;
    }
}
