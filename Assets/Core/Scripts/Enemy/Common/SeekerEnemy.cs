using UnityEngine;
using Sirenix.OdinInspector;

public sealed class SeekerEnemy : EnemyBase, IEnemyProjectileOwner
{
    private enum State
    {
        Drift,
        Fire
    }

    private const string AnimDrift = "Drift";
    private const string AnimFire = "Fire";
    private const string AnimDeath = "Death";

    [TabGroup("Seeker Enemy", "Setup"), BoxGroup("Seeker Enemy/Setup/References"), SerializeField, Required]
    private EnemyProjectile projectilePrefab;

    [TabGroup("Seeker Enemy", "Setup"), BoxGroup("Seeker Enemy/Setup/References"), SerializeField, Required]
    private Transform firePoint;

    [TabGroup("Seeker Enemy", "Setup"), BoxGroup("Seeker Enemy/Setup/References"), SerializeField, Required]
    private Collider2D hitbox;

    [TabGroup("Seeker Enemy", "Tuning"), BoxGroup("Seeker Enemy/Tuning/Drift"), SerializeField, MinValue(0f), SuffixLabel("u", true)]
    private float desiredHeight = 4.5f;

    [TabGroup("Seeker Enemy", "Tuning"), BoxGroup("Seeker Enemy/Tuning/Drift"), SerializeField, MinValue(0f), SuffixLabel("u", true)]
    private float desiredHorizontal = 5.5f;

    [TabGroup("Seeker Enemy", "Tuning"), BoxGroup("Seeker Enemy/Tuning/Drift"), SerializeField, MinValue(0f), SuffixLabel("u/s", true)]
    private float moveSpeedNear = 6f;

    [TabGroup("Seeker Enemy", "Tuning"), BoxGroup("Seeker Enemy/Tuning/Drift"), SerializeField, MinValue(0f), SuffixLabel("u/s", true)]
    private float moveSpeedFar = 1f;

    [TabGroup("Seeker Enemy", "Tuning"), BoxGroup("Seeker Enemy/Tuning/Drift"), SerializeField, MinValue(0f), SuffixLabel("u", true)]
    private float maxPlayerDistForSpeed = 10f;

    [TabGroup("Seeker Enemy", "Tuning"), BoxGroup("Seeker Enemy/Tuning/Drift"), SerializeField, MinValue(0f), SuffixLabel("u", true)]
    private float driftStopRadius = 0.15f;

    [TabGroup("Seeker Enemy", "Tuning"), BoxGroup("Seeker Enemy/Tuning/Fire"), SerializeField, MinMaxSlider(0f, 10f, true)]
    private Vector2 fireIntervalRange = new(1f, 3f);

    [TabGroup("Seeker Enemy", "Tuning"), BoxGroup("Seeker Enemy/Tuning/Fire"), SerializeField, MinValue(0f), SuffixLabel("imp", true)]
    private float fireRecoilForce = 2f;

    [TabGroup("Seeker Enemy", "Tuning"), BoxGroup("Seeker Enemy/Tuning/Fire"), SerializeField, PropertyRange(0f, 1f), SuffixLabel("%", true)]
    private float fireShootPercent = 0.65f;

    [TabGroup("Seeker Enemy", "Tuning"), BoxGroup("Seeker Enemy/Tuning/Damage"), SerializeField, MinValue(0), SuffixLabel("HP", true)]
    private int projectileDamage = 50;

    private State state;
    private float fireCooldown;
    private float fireTimer = -999f;
    private float fireStateLength;
    private bool fired;
    private int keepSide = 1;

    protected override string DeathAnimName => AnimDeath;

    public Transform ProjectileTargetTransform => transform;

    public Collider2D ProjectileHitbox => hitbox;

    protected override void Start()
    {
        base.Start();

        DeathDespawnDelay = -1f;

        fireCooldown = Random.Range(fireIntervalRange.x, fireIntervalRange.y);
        state = State.Drift;
        Anim.Play(AnimDrift);
    }

    protected override void OnUpdate()
    {
        switch (state)
        {
            case State.Drift:
                UpdateDrift();
                break;
            case State.Fire:
                UpdateFire();
                break;
        }
    }

    protected override void OnFixedUpdate()
    {
        if (state == State.Drift) DriftMove();
        else Body.linearVelocity = Vector2.zero;
    }

    private void UpdateDrift()
    {
        fireCooldown -= Time.deltaTime;
        if (fireCooldown <= 0f)
        {
            EnterFire();
            return;
        }

        float rel = transform.position.x - Player.transform.position.x;
        if (rel > 0f) keepSide = 1;
        else if (rel < 0f) keepSide = -1;
    }

    private void DriftMove()
    {
        Vector2 playerPos = Player.transform.position;
        Vector2 target = new(playerPos.x + desiredHorizontal * keepSide, playerPos.y + desiredHeight);
        Vector2 pos = transform.position;
        Vector2 to = target - pos;
        float dist = to.magnitude;

        if (dist <= driftStopRadius)
        {
            Body.linearVelocity = Vector2.zero;
            return;
        }

        float distToPlayer = Vector2.Distance(pos, playerPos);
        float t = 1f - Mathf.Clamp01(distToPlayer / maxPlayerDistForSpeed);
        float spd = Mathf.Lerp(moveSpeedFar, moveSpeedNear, t);

        Body.linearVelocity = to.normalized * spd;

        FacePlayer();
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
            fireCooldown = Random.Range(fireIntervalRange.x, fireIntervalRange.y);
            state = State.Drift;
            Anim.Play(AnimDrift);
        }
    }

    private void FireOne()
    {
        Vector2 dir = transform.right;
        EnemyProjectile proj = Instantiate(projectilePrefab, firePoint.position, transform.rotation);
        proj.Initialize(this, Player, dir, projectileDamage);
        Body.AddForce(-dir * fireRecoilForce, ForceMode2D.Impulse);
    }

    public void OnHitByReflectedProjectile()
    {
        Die();
    }
}