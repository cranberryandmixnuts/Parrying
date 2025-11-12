using UnityEngine;

public sealed class SeekerEnemy : EnemyBase
{
    private enum SeekerState
    {
        Drift,
        Fire,
        Death
    }

    private const string AnimDrift = "Drift";
    private const string AnimFire = "Fire";
    private const string AnimDeath = "Death";

    [SerializeField] private SeekerProjectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private BoxCollider2D hitbox;
    public BoxCollider2D Hitbox => hitbox;

    [Header("Drift")]
    [SerializeField] private float desiredHeight = 4.5f;
    [SerializeField] private float desiredHorizontal = 5.5f;
    [SerializeField] private float moveSpeedNear = 6f;
    [SerializeField] private float moveSpeedFar = 1f;
    [SerializeField] private float maxPlayerDistForSpeed = 10f;
    [SerializeField] private float driftStopRadius = 0.15f;

    [Header("Fire")]
    [SerializeField] private Vector2 fireIntervalRange = new(2f, 4f);
    [SerializeField] private float fireRecoilForce = 2f;
    [SerializeField, Range(0f, 1f)] private float fireShootPercent = 0.6f;

    private SeekerState state;
    private float fireCooldown;
    private float fireTimer = -999f;
    private readonly float fireStateLength;
    private bool fired;
    private float deathTimer;
    private int keepSide = 1;

    protected override string DeathAnimName
    {
        get { return AnimDeath; }
    }

    protected override void Start()
    {
        base.Start();
        fireCooldown = Random.Range(fireIntervalRange.x, fireIntervalRange.y);
        state = SeekerState.Drift;
        Anim.Play(AnimDrift);
    }

    protected override void OnUpdate()
    {
        switch (state)
        {
            case SeekerState.Drift:
                UpdateDrift();
                break;
            case SeekerState.Fire:
                UpdateFire();
                break;
            case SeekerState.Death:
                UpdateDeath();
                break;
        }
    }

    protected override void OnFixedUpdate()
    {
        if (state == SeekerState.Drift) DriftMove();
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

        Vector2 v = to.normalized * spd;
        Body.linearVelocity = v;

        FacePlayer();
    }

    private void EnterFire()
    {
        state = SeekerState.Fire;
        Body.linearVelocity = Vector2.zero;
        Anim.Play(AnimFire);
        fireTimer = GetAnimLength(AnimFire);
        fired = false;
    }

    private void UpdateFire()
    {
        fireTimer -= Time.deltaTime;

        float elapsed = fireStateLength - fireTimer;
        if (!fired && elapsed >= fireStateLength * fireShootPercent)
        {
            FireOne();
            fired = true;
        }

        if (fireTimer <= 0f)
        {
            fireCooldown = Random.Range(fireIntervalRange.x, fireIntervalRange.y);
            state = SeekerState.Drift;
            Anim.Play(AnimDrift);
        }
    }

    private void FireOne()
    {
        Vector2 dir = transform.right;
        SeekerProjectile proj = Instantiate(projectilePrefab, firePoint.position, transform.rotation);
        proj.Initialize(this, Player, dir);
        Body.AddForce(-dir * fireRecoilForce, ForceMode2D.Impulse);
    }

    private void UpdateDeath()
    {
        deathTimer -= Time.deltaTime;
        if (deathTimer <= 0f) Destroy(gameObject);
    }

    public override void Die()
    {
        if (state == SeekerState.Death) return;
        state = SeekerState.Death;
        Body.linearVelocity = Vector2.zero;
        Anim.Play(AnimDeath);
        deathTimer = GetAnimLength(AnimDeath);
    }

    public void OnHitByReflectedProjectile()
    {
        Die();
    }
}