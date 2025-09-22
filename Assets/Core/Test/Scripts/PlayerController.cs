using UnityEngine;

public sealed class PlayerController : MonoBehaviour
{
    public float MoveSpeed = 8f;
    public float JumpForce = 14f;
    public float CoyoteTime = 0.1f;
    public float JumpBuffer = 0.1f;

    public GroundCheckTrigger GroundCheck;

    private Rigidbody2D rb;
    private float lastJumpPress;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        Vector2 v = rb.linearVelocity;
        v.x = h * MoveSpeed;
        rb.linearVelocity = v;

        if (Input.GetKeyDown(KeyCode.Space)) lastJumpPress = Time.time;

        if (ShouldJump())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * JumpForce, ForceMode2D.Impulse);
            lastJumpPress = -999f;
            return;
        }
    }

    private bool ShouldJump()
    {
        if (Time.time - lastJumpPress > JumpBuffer) return false;
        if (GroundCheck == null) return false;
        if (GroundCheck.IsGrounded) return true;
        if (Time.time - GroundCheck.LastGroundedTime <= CoyoteTime) return true;
        return false;
    }
}