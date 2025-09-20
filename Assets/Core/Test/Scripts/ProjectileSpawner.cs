using UnityEngine;

public sealed class ProjectileSpawner : MonoBehaviour
{
    public Projectile ProjectilePrefab;
    public Transform Target;
    public float Interval = 1.2f;
    public float SpreadAngle = 6f;
    public LayerMask ProjectileHitMask;

    private float last;

    private void Update()
    {
        if (Time.time - last < Interval) return;
        last = Time.time;
        SpawnOne();
    }

    private void SpawnOne()
    {
        if (ProjectilePrefab == null) return;
        Vector2 dir = Vector2.right;
        if (Target != null)
        {
            dir = (Target.position - transform.position).normalized;
            dir = Quaternion.Euler(0f, 0f, Random.Range(-SpreadAngle, SpreadAngle)) * dir;
        }
        Projectile p = Instantiate(ProjectilePrefab, transform.position, Quaternion.identity);
        p.HitMask = ProjectileHitMask;
        p.Init(dir);
    }
}
