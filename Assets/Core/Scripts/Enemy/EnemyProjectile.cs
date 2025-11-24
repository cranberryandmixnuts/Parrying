using UnityEngine;

public sealed class EnemyProjectile : MonoBehaviour, IParryReactive
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float turnSpeed = 160f;
    [SerializeField] private float reflectSpeed = 30f;
    [SerializeField] private float reflectTurnSpeed = 300f;
    [SerializeField] private float hitboxDisableDuration = 1f;
    [SerializeField] private float maxLifetime = 6f;
    [SerializeField] private int damage = 10;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask playerHitMask;
    [SerializeField] private Collider2D hitCollider;

    private SeekerEnemy owner;
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

    public void Initialize(SeekerEnemy shooter, PlayerController p, Vector2 initialDir)
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
        moveDir = initialDir.normalized;
        if (moveDir.sqrMagnitude < 0.0001f) moveDir = Vector2.right;
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
            Vector2 tgt = target.position;
            Vector2 to = tgt - pos;
            float curAng = Mathf.Atan2(transform.right.y, transform.right.x) * Mathf.Rad2Deg;
            float desAng = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
            float maxStep = (reflected ? reflectTurnSpeed : turnSpeed) * dt;
            float newAng = Mathf.MoveTowardsAngle(curAng, desAng, maxStep);
            transform.rotation = Quaternion.Euler(0f, 0f, newAng);
            moveDir = transform.right;
        }

        float spd = reflected ? reflectSpeed : speed;
        Vector2 vel = moveDir * spd;
        transform.position = pos + vel * dt;

        if (hitboxActive)
        {
            player.GetParryDetectCircle(out Vector2 center, out float radius);
            if (IsColliderWithinCircle(hitCollider, center, radius))
                player.RegisterParryCandidate(this, hitCollider.bounds.center, damage);

            player.GetDashDetectCircle(out Vector2 dcenter, out float dradius);
            if (IsColliderWithinCircle(hitCollider, dcenter, dradius))
                player.RegisterDashCandidate(hitCollider.bounds.center);
        }

        if (OverlapsGround())
        {
            Destroy(gameObject);
            return;
        }

        if (hitboxActive)
        {
            if (OverlapsPlayerBody())
            {
                if (player.TryHit(damage, hitCollider.bounds.center))
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }

        if (reflected)
        {
            if (OverlapsOwner())
            {
                owner.OnHitByReflectedProjectile();
                Destroy(gameObject);
                return;
            }
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
        ContactFilter2D f = new();
        f.SetLayerMask(mask);
        f.useTriggers = true;
        return hitCollider.Overlap(f, overlapResults);
    }

    private bool OverlapsPlayerBody()
    {
        int count = Overlap(playerHitMask);
        for (int i = 0; i < count; i++)
        {
            if (overlapResults[i] == null) continue;
            PlayerController pc = overlapResults[i].GetComponentInParent<PlayerController>();
            if (pc == player) return true;
        }
        return false;
    }

    private bool OverlapsGround()
    {
        int count = Overlap(groundMask);
        for (int i = 0; i < count; i++)
            if (overlapResults[i] != null) return true;
        return false;
    }

    private bool OverlapsOwner() => hitCollider.Distance(owner.Hitbox).isOverlapped;

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