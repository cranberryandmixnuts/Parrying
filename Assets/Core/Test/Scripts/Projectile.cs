using UnityEngine;
using System.Collections.Generic;

public sealed class Projectile : MonoBehaviour
{
    public static readonly List<Projectile> ActiveProjectiles = new();

    public float Radius = 0.2f;
    public float Speed = 12f;
    public float MaxDistance = 20f;
    public LayerMask HitMask;
    public int Damage = 1;
    public Transform Source;

    private Vector2 dir = Vector2.right;
    private float traveled;
    private bool deadly = true;
    public bool IsDeadly => deadly;

    private void OnEnable()
    {
        traveled = 0f;
        if (!ActiveProjectiles.Contains(this)) ActiveProjectiles.Add(this);
    }

    private void OnDisable()
    {
        ActiveProjectiles.Remove(this);
    }

    public void Init(Vector2 direction)
    {
        dir = direction.normalized;
        transform.right = dir;
    }

    public void Neutralize()
    {
        deadly = false;
    }

    public void ConsumeAndDestroy()
    {
        Destroy(gameObject);
    }

    public void ReflectToSource()
    {
        Vector2 selfPos = transform.position;
        if (Source != null)
        {
            Vector2 toSource = ((Vector2)Source.position - selfPos).normalized;
            if (toSource.sqrMagnitude > 0f)
                dir = toSource;
            else
                dir = -dir;
        }
        Speed *= 2f;
        traveled = 0f;
    }

    private void FixedUpdate()
    {
        Vector2 p = transform.position;
        Vector2 step = Speed * Time.fixedDeltaTime * dir;
        float dist = step.magnitude;

        if (deadly)
        {
            RaycastHit2D hit = Physics2D.CircleCast(p, Radius, dir, dist, HitMask);
            if (hit.collider != null)
            {
                IProjectileResponder r = hit.collider.GetComponent<IProjectileResponder>();
                IDamageable d = hit.collider.GetComponent<IDamageable>();

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
                        Neutralize();
                        transform.position = p + step;
                        traveled += dist;
                        if (traveled >= MaxDistance) Destroy(gameObject);
                        return;
                    }

                    if (resp == ProjectileHitResponse.ReflectToSource)
                    {
                        ReflectToSource();
                        transform.position = p + step;
                        traveled += dist;
                        if (traveled >= MaxDistance) Destroy(gameObject);
                        return;
                    }

                    if (resp == ProjectileHitResponse.ConsumedAlready)
                    {
                        Destroy(gameObject);
                        return;
                    }

                    if (resp == ProjectileHitResponse.Consume)
                    {
                        d?.Hit(Damage, p);
                        Destroy(gameObject);
                        return;
                    }
                }

                d?.Hit(Damage, p);

                Destroy(gameObject);
                return;
            }
        }

        transform.position = p + step;
        traveled += dist;
        if (traveled >= MaxDistance)
            Destroy(gameObject);
    }
}