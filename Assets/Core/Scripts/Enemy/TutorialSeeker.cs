using UnityEngine;
using Sirenix.OdinInspector;

public sealed class TutorialSeeker : EnemyBase, IEnemyProjectileOwner
{
    private enum State
    {
        Idle,
        Fire
    }

    private const string AnimIdle = "Drift";
    private const string AnimFire = "Fire";
    private const string AnimDeath = "Death";

    [TabGroup("Tutorial Seeker", "Setup"), BoxGroup("Tutorial Seeker/Setup/References"), SerializeField, Required]
    private EnemyProjectile projectilePrefab;

    [TabGroup("Tutorial Seeker", "Setup"), BoxGroup("Tutorial Seeker/Setup/References"), SerializeField, Required]
    private Transform firePoint;

    [TabGroup("Tutorial Seeker", "Setup"), BoxGroup("Tutorial Seeker/Setup/References"), SerializeField, Required]
    private BoxCollider2D hitbox;

    [TabGroup("Tutorial Seeker", "Tuning"), BoxGroup("Tutorial Seeker/Tuning/Fire"), SerializeField, PropertyRange(0f, 1f), SuffixLabel("%", true)]
    private float fireShootPercent = 0.6f;

    [TabGroup("Tutorial Seeker", "Tuning"), BoxGroup("Tutorial Seeker/Tuning/Fire"), SerializeField, MinValue(0), SuffixLabel("HP", true)]
    private int projectileDamage = 80;

    private State state;
    private float fireTimer = -999f;
    private float fireStateLength;
    private bool fired;

    protected override string DeathAnimName => AnimDeath;

    public Transform ProjectileTargetTransform => transform;

    public Collider2D ProjectileHitbox => hitbox;

    protected override void Start()
    {
        base.Start();

        DeathDespawnDelay = -1f;

        state = State.Idle;
        Anim.Play(AnimIdle);
    }

    protected override void OnUpdate()
    {
        if (state == State.Fire) UpdateFire();
    }

    public void FireAtPlayer()
    {
        if (IsDead()) return;
        if (state == State.Fire) return;

        EnterFire();
    }

    private void EnterFire()
    {
        state = State.Fire;

        Body.linearVelocity = Vector2.zero;
        FacePlayer();

        Anim.Play(AnimFire);

        fireStateLength = GetAnimLength(AnimFire);
        fireTimer = fireStateLength;
        fired = false;
    }

    private void UpdateFire()
    {
        fireTimer -= Time.deltaTime;

        if (fireStateLength > 0f)
        {
            float progress = 1f - (fireTimer / fireStateLength);

            if (!fired && progress >= fireShootPercent)
            {
                FireOne();
                fired = true;
            }
        }

        if (fireTimer <= 0f)
        {
            state = State.Idle;
            Anim.Play(AnimIdle);
        }
    }

    private void FireOne()
    {
        Vector2 dir = transform.right;

        EnemyProjectile proj = Instantiate(projectilePrefab, firePoint.position, transform.rotation);
        proj.Initialize(this, Player, dir, projectileDamage);
    }

    public void OnHitByReflectedProjectile()
    {
        Die();
    }
}