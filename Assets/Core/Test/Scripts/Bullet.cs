using UnityEngine;

public sealed class Bullet : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector3 origin;
    private float maxDistance;
    private LayerMask hitMask;
    private bool initialized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        origin = transform.position;
    }

    private void Update()
    {
        if (!initialized) return;
        if ((transform.position - origin).sqrMagnitude >= maxDistance * maxDistance) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        int mask = 1 << other.gameObject.layer;
        if ((hitMask.value & mask) == 0) return;
        Destroy(gameObject);
    }

    public void Initialize(Vector2 initialVelocity, float maxDistance, LayerMask hitMask)
    {
        this.maxDistance = maxDistance;
        this.hitMask = hitMask;
        initialized = true;
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = initialVelocity;
    }
}