using UnityEngine;

public sealed class BulletEmitter : MonoBehaviour
{
    public Bullet BulletPrefab;
    public float Interval = 1.0f;
    public Vector2 InitialVelocity = new(10f, 0f);
    public float MaxTravelDistance = 20f;
    public LayerMask HitMask;

    private float nextAt;

    private void Start()
    {
        nextAt = Time.time + Interval;
    }

    private void Update()
    {
        if (Time.time < nextAt) return;
        Spawn();
        nextAt = Time.time + Interval;
    }

    private void Spawn()
    {
        Bullet b = Instantiate(BulletPrefab, transform.position, Quaternion.identity);
        b.Initialize(InitialVelocity, MaxTravelDistance, HitMask);
    }
}