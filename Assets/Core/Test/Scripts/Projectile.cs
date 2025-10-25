using UnityEngine;

public sealed class Projectile : MonoBehaviour
{
    public float Radius = 0.2f;
    public float Speed = 12f;
    public float MaxDistance = 20f;
    public LayerMask HitMask;
    public int Damage = 1;
    public Transform Source;

    private Vector2 dir = Vector2.right;
    private float traveled;
    private bool deadly = true;

    private void OnEnable()
    {
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

        if (deadly)
        {
            RaycastHit2D hit = Physics2D.CircleCast(p, Radius, dir, dist, HitMask);
            if (hit.collider != null)
            {
                IProjectileResponder r = hit.collider.GetComponent<IProjectileResponder>();
                if (r != null)
                {
                    ProjectileHitResponse resp = r.OnProjectileHit(this, hit.collider);
                    if (resp == ProjectileHitResponse.IgnoreContinue)
                    {
                        transform.position = p + step;
                        traveled += dist;
                        if (traveled >= MaxDistance) Destroy(gameObject);
                        return;
                    }
                    if (resp == ProjectileHitResponse.NeutralizeContinue)
                    {
                        deadly = false;
                        transform.position = p + step;
                        traveled += dist;
                        if (traveled >= MaxDistance) Destroy(gameObject);
                        return;
                    }
                    if (resp == ProjectileHitResponse.ReflectToSource)
                    {
                        if (Source != null)
                        {
                            Vector2 toSource = ((Vector2)Source.position - p).normalized;
                            dir = toSource.sqrMagnitude > 0f ? toSource : -dir;
                        }
                        Speed *= 2f;
                        transform.position = p + step;
                        traveled += dist;
                        if (traveled >= MaxDistance) Destroy(gameObject);
                        return;
                    }
                }

                IDamageable d = hit.collider.GetComponent<IDamageable>();
                if (d != null) d.Hit(Damage);
                Destroy(gameObject);
                return;
            }
        }

        transform.position = p + step;
        traveled += dist;
        if (traveled >= MaxDistance) Destroy(gameObject);
    }
}