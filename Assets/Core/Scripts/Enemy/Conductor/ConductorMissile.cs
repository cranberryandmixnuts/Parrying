using UnityEngine;

public sealed class ConductorMissile : MonoBehaviour, IParryReactive
{
    [SerializeField] private float speed = 6f;
    [SerializeField] private float turnSpeed = 160f;
    [SerializeField] private float reflectSpeed = 26f;
    [SerializeField] private float reflectTurnSpeed = 320f;
    [SerializeField] private float hitboxDisableDuration = 0.8f;
    [SerializeField] private float maxLifetime = 6f;
    [SerializeField] private int damage = 10;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask playerHitMask;
    [SerializeField] private Collider2D hitCollider;

    private ConductorBoss owner;
    private PlayerController player;
    private Transform target;
    private bool hitboxActive;
    private float hitboxTimer;
    private bool reflected;
    private bool homing;
    private float lifeTimer;
    private bool consumed;
    private Vector2 moveDir;
    private readonly Collider2D[] overlapResults = new Collider2D[8];

    public void Initialize(ConductorBoss shooter, PlayerController p, Vector2 initialDir)
    {
        owner = shooter;
        player = p;
        target = p.transform;
        hitboxActive = true;
        hitboxTimer = 0f;
        reflected = false;
        homing = true;
        lifeTimer = maxLifetime;
        consumed = false;
        moveDir = initialDir.sqrMagnitude > 0.0001f ? initialDir.normalized : Vector2.right;
        transform.right = moveDir;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        lifeTimer -= dt;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (hitboxTimer > 0f)
        {
            hitboxTimer -= dt;
            if (hitboxTimer <= 0f) hitboxActive = true;
        }

        Vector2 pos = transform.position;

        if (homing && target != null)
        {
            Vector2 to = (Vector2)target.position - pos;
            float cur = Mathf.Atan2(transform.right.y, transform.right.x) * Mathf.Rad2Deg;
            float des = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
            float maxStep = (reflected ? reflectTurnSpeed : turnSpeed) * dt;
            float ang = Mathf.MoveTowardsAngle(cur, des, maxStep);
            transform.rotation = Quaternion.Euler(0f, 0f, ang);
            moveDir = transform.right;
        }

        float spd = reflected ? reflectSpeed : speed;
        transform.position = pos + moveDir * spd * dt;

        if (hitboxActive)
        {
            player.GetParryDetectCircle(out Vector2 pc, out float pr);
            if (IsColliderWithinCircle(hitCollider, pc, pr)) player.RegisterParryCandidate(this, hitCollider.bounds.center, damage);

            player.GetDashDetectCircle(out Vector2 dc, out float dr);
            if (IsColliderWithinCircle(hitCollider, dc, dr)) player.RegisterDashCandidate(hitCollider.bounds.center);
        }

        if (OverlapsMask(groundMask))
        {
            Destroy(gameObject);
            return;
        }

        if (hitboxActive && OverlapsPlayer())
        {
            if (player.TryHit(damage, hitCollider.bounds.center))
            {
                Destroy(gameObject);
                return;
            }
        }

        if (reflected && owner != null && OverlapsOwner())
        {
            owner.OnMissileReflectedHit();
            Destroy(gameObject);
            return;
        }
    }

    private bool IsColliderWithinCircle(Collider2D col, Vector2 center, float radius)
    {
        Vector2 p = col.ClosestPoint(center);
        float dx = p.x - center.x;
        float dy = p.y - center.y;
        float d2 = dx * dx + dy * dy;
        float r2 = radius * radius;
        return d2 <= r2;
    }

    private int Overlap(LayerMask mask)
    {
        ContactFilter2D f = new ContactFilter2D();
        f.SetLayerMask(mask);
        f.useTriggers = true;
        return hitCollider.Overlap(f, overlapResults);
    }

    private bool OverlapsMask(LayerMask mask)
    {
        int count = Overlap(mask);
        for (int i = 0; i < count; i++) if (overlapResults[i] != null) return true;
        return false;
    }

    private bool OverlapsPlayer()
    {
        int count = Overlap(playerHitMask);
        for (int i = 0; i < count; i++)
        {
            PlayerController pc = overlapResults[i].GetComponentInParent<PlayerController>();
            if (pc == player) return true;
        }
        return false;
    }

    private bool OverlapsOwner()
    {
        Vector2 a = hitCollider.bounds.center;
        Vector2 b = owner.transform.position;
        float dx = a.x - b.x;
        float dy = a.y - b.y;
        float r = 0.6f;
        return dx * dx + dy * dy <= r * r;
    }

    public void OnPerfectParry(Vector2 hitPoint)
    {
        if (consumed) return;
        consumed = true;
        Destroy(gameObject);
    }

    public void OnImperfectParry(Vector2 hitPoint)
    {
        if (consumed) return;
        hitboxActive = false;
        hitboxTimer = hitboxDisableDuration;
        homing = false;
    }

    public void OnCounterParry(Vector2 hitPoint)
    {
        if (consumed) return;
        reflected = true;
        homing = true;
        hitboxActive = true;
        hitboxTimer = 0f;
        if (owner != null)
        {
            target = owner.transform;
            Vector2 toOwner = (Vector2)owner.transform.position - (Vector2)transform.position;
            if (toOwner.sqrMagnitude > 0.0001f)
            {
                toOwner.Normalize();
                moveDir = toOwner;
                transform.right = moveDir;
            }
        }
    }
}