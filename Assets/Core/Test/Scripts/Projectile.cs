using UnityEngine;

public sealed class Projectile : MonoBehaviour
{
    public float Radius = 0.2f;
    public float Speed = 12f;
    public float MaxDistance = 20f;
    public LayerMask HitMask;
    public int Damage = 1;

    private Vector2 dir = Vector2.right;
    private Vector2 lastPos;
    private float traveled;

    private void OnEnable()
    {
        lastPos = transform.position;
        traveled = 0f;
    }

    public void Init(Vector2 direction)
    {
        dir = direction.normalized;
        transform.right = dir;
    }

    private void FixedUpdate()
    {
        Vector2 p = transform.position;
        Vector2 step = dir * Speed * Time.fixedDeltaTime;
        float dist = step.magnitude;
        RaycastHit2D hit = Physics2D.CircleCast(p, Radius, dir, dist, HitMask);
        if (hit.collider != null)
        {
            IDamageable d = hit.collider.GetComponent<IDamageable>();
            if (d != null) d.Hit(Damage);
            Destroy(gameObject);
            return;
        }
        transform.position = p + step;
        traveled += dist;
        if (traveled >= MaxDistance) Destroy(gameObject);
    }
}
