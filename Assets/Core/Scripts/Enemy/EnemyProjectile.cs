using UnityEngine;
using Sirenix.OdinInspector;

public sealed class EnemyProjectile : MonoBehaviour, IParryReactive
{
    [TabGroup("Enemy Projectile", "Setup"), BoxGroup("Enemy Projectile/Setup/Masks"), SerializeField]
    private LayerMask groundMask;

    [TabGroup("Enemy Projectile", "Setup"), BoxGroup("Enemy Projectile/Setup/Masks"), SerializeField]
    private LayerMask playerHitMask;

    [TabGroup("Enemy Projectile", "Setup"), BoxGroup("Enemy Projectile/Setup/Colliders"), SerializeField, Required]
    private Collider2D hitCollider;

    [TabGroup("Enemy Projectile", "Tuning"), BoxGroup("Enemy Projectile/Tuning/Movement"), SerializeField, MinValue(0f), SuffixLabel("u/s", true)]
    private float speed = 10f;

    [TabGroup("Enemy Projectile", "Tuning"), BoxGroup("Enemy Projectile/Tuning/Movement"), SerializeField, MinValue(0f), SuffixLabel("deg/s", true)]
    private float turnSpeed = 160f;

    [TabGroup("Enemy Projectile", "Tuning"), BoxGroup("Enemy Projectile/Tuning/Reflect"), SerializeField, MinValue(0f), SuffixLabel("u/s", true)]
    private float reflectSpeed = 50f;

    [TabGroup("Enemy Projectile", "Tuning"), BoxGroup("Enemy Projectile/Tuning/Reflect"), SerializeField, MinValue(0f), SuffixLabel("deg/s", true)]
    private float reflectTurnSpeed = 600f;

    [TabGroup("Enemy Projectile", "Tuning"), BoxGroup("Enemy Projectile/Tuning/Lifetime"), SerializeField, MinValue(0.01f), SuffixLabel("s", true)]
    private float maxLifetime = 6f;

    private IEnemyProjectileOwner owner;
    private PlayerController player;
    private Transform target;
    private bool reflected;
    private float lifeTimer;
    private bool consumed;
    private Vector2 moveDir;
    private readonly Collider2D[] overlapResults = new Collider2D[8];
    private int projectileDamage;

    public void Initialize(IEnemyProjectileOwner shooter, PlayerController player, Vector2 initialDir, int damage)
    {
        owner = shooter;
        this.player = player;
        target = player.transform;
        reflected = false;
        lifeTimer = maxLifetime;
        consumed = false;
        projectileDamage = damage;

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

        Vector2 pos = transform.position;

        Vector2 tgt = target.position;
        Vector2 to = tgt - pos;

        float curAng = Mathf.Atan2(transform.right.y, transform.right.x) * Mathf.Rad2Deg;
        float desAng = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
        float maxStep = (reflected ? reflectTurnSpeed : turnSpeed) * dt;
        float newAng = Mathf.MoveTowardsAngle(curAng, desAng, maxStep);

        transform.rotation = Quaternion.Euler(0f, 0f, newAng);
        moveDir = transform.right;

        float spd = reflected ? reflectSpeed : speed;
        Vector2 vel = moveDir * spd;
        transform.position = pos + vel * dt;

        player.GetParryDetectCircle(out Vector2 center, out float radius);
        if (IsColliderWithinCircle(hitCollider, center, radius))
            player.RegisterParryCandidate(this, transform.position, projectileDamage);

        player.GetDashDetectCircle(out Vector2 dcenter, out float dradius);
        if (IsColliderWithinCircle(hitCollider, dcenter, dradius))
            player.RegisterDashCandidate(transform.position);

        if (OverlapsGround())
        {
            Destroy(gameObject);
            return;
        }

        if (OverlapsPlayerBody())
        {
            if (player.TryHit(projectileDamage, transform.position))
            {
                Destroy(gameObject);
                return;
            }
        }

        if (reflected)
        {
            if (OverlapsOwner())
            {
                owner.OnHitByReflectedProjectile();
                Destroy(gameObject);
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

    private bool OverlapsOwner()
    {
        Collider2D ownerHitbox = owner.ProjectileHitbox;

        return hitCollider.Distance(ownerHitbox).isOverlapped;
    }

    public void OnPerfectParry(Vector2 hitPoint)
    {
        if (consumed) return;
        consumed = true;
        Destroy(gameObject);
    }

    public void OnImperfectParry(Vector2 hitPoint) => OnPerfectParry(hitPoint);

    public void OnCounterParry(Vector2 hitPoint)
    {
        if (consumed) return;

        reflected = true;

        target = owner.ProjectileTargetTransform;

        Vector2 toOwner = (Vector2)owner.ProjectileTargetTransform.position - (Vector2)transform.position;
        if (toOwner.sqrMagnitude > 0.0001f)
        {
            toOwner.Normalize();
            moveDir = toOwner;
            transform.right = moveDir;
        }
    }
}