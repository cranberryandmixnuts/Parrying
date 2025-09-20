using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public sealed class PlayerController2D : MonoBehaviour
{
    public float MoveSpeed = 8f;
    public float JumpForce = 14f;
    public float DoubleJumpForce = 13f;
    public float WallJumpForce = 14f;
    public float WallJumpPush = 8f;
    public float CoyoteTime = 0.1f;
    public float JumpBuffer = 0.1f;
    public float WallSlideMaxFall = 4f;

    public Transform GroundCheck;
    public Transform LeftWallCheck;
    public Transform RightWallCheck;
    public float CheckRadius = 0.2f;
    public LayerMask GroundMask;

    private Rigidbody2D rb;
    private bool grounded;
    private bool onLeftWall;
    private bool onRightWall;
    private bool canDouble;
    private float lastGroundTime;
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

        grounded = Physics2D.OverlapCircle(GroundCheck.position, CheckRadius, GroundMask);
        if (grounded) lastGroundTime = Time.time;

        onLeftWall = Physics2D.OverlapCircle(LeftWallCheck.position, CheckRadius, GroundMask);
        onRightWall = Physics2D.OverlapCircle(RightWallCheck.position, CheckRadius, GroundMask);

        if (ShouldNormalJump())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * JumpForce, ForceMode2D.Impulse);
            canDouble = true;
            lastJumpPress = -999f;
            return;
        }

        if (ShouldWallJump())
        {
            int dir = onLeftWall ? 1 : -1;
            rb.linearVelocity = new Vector2(0f, 0f);
            rb.AddForce(new Vector2(dir * WallJumpPush, WallJumpForce), ForceMode2D.Impulse);
            canDouble = true;
            lastJumpPress = -999f;
            return;
        }

        if (ShouldDoubleJump())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * DoubleJumpForce, ForceMode2D.Impulse);
            canDouble = false;
            lastJumpPress = -999f;
            return;
        }

        if (IsOnWall() && rb.linearVelocity.y < -WallSlideMaxFall)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -WallSlideMaxFall);
        }
    }

    private bool ShouldNormalJump()
    {
        if (Time.time - lastJumpPress > JumpBuffer) return false;
        if (Time.time - lastGroundTime > CoyoteTime) return false;
        return true;
    }

    private bool ShouldWallJump()
    {
        if (Time.time - lastJumpPress > JumpBuffer) return false;
        if (grounded) return false;
        if (!IsOnWall()) return false;
        return true;
    }

    private bool ShouldDoubleJump()
    {
        if (Time.time - lastJumpPress > JumpBuffer) return false;
        if (grounded) return false;
        if (IsOnWall()) return false;
        if (!canDouble) return false;
        return true;
    }

    private bool IsOnWall()
    {
        if (onLeftWall) return true;
        if (onRightWall) return true;
        return false;
    }
}
