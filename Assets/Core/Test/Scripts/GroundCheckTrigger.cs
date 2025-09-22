using UnityEngine;

public sealed class GroundCheckTrigger : MonoBehaviour
{
    public LayerMask GroundMask;

    public bool IsGrounded { get; private set; }
    public float LastGroundedTime { get; private set; }

    private int contacts;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & GroundMask) == 0) return;
        contacts += 1;
        if (contacts > 0) SetGrounded(true);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & GroundMask) == 0) return;
        if (contacts > 0) SetGrounded(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & GroundMask) == 0) return;
        contacts -= 1;
        if (contacts <= 0) SetGrounded(false);
    }

    private void SetGrounded(bool value)
    {
        IsGrounded = value;
        if (value) LastGroundedTime = Time.time;
    }
}