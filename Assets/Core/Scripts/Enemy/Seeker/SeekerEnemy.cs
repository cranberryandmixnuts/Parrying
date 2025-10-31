using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public sealed class SeekerEnemy : MonoBehaviour
{
    private enum SeekerState
    {
        Drift,
        Fire,
        Death
    }

    [SerializeField] private SeekerProjectile projectilePrefab;
    [SerializeField] private Transform firePoint;

    [Header("Drift")]
    [SerializeField] private float desiredHeight = 4.5f;
    [SerializeField] private float desiredHorizontal = 5.5f;
    [SerializeField] private float moveSpeedNear = 6f;
    [SerializeField] private float moveSpeedFar = 1f;
    [SerializeField] private float maxPlayerDistForSpeed = 10f;
    [SerializeField] private float driftStopRadius = 0.15f;

    [Header("Fire")]
    [SerializeField] private float fireWindup = 0.4f;
    [SerializeField] private Vector2 fireIntervalRange = new(2f, 4f);
    [SerializeField] private float fireRecoilForce = 2f;

    [Header("Death")]
    [SerializeField] private float deathDespawn = 1.0f;

    private PlayerController player;
    private Rigidbody2D rb;
    private Animator animator;

    private SeekerState state;
    private float fireCooldown;
    private float fireTimer;
    private float deathTimer;
    private int facingDirection = 1;
    private int keepSide = 1;

    private void Start()
    {
        player = PlayerController.Instance;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        fireCooldown = Random.Range(fireIntervalRange.x, fireIntervalRange.y);
        state = SeekerState.Drift;
        if (animator != null) animator.Play("Drift");
    }

    private void Update()
    {
        if (state == SeekerState.Drift) UpdateDrift();
        else if (state == SeekerState.Fire) UpdateFire();
        else if (state == SeekerState.Death) UpdateDeath();
    }

    private void FixedUpdate()
    {
        if (state == SeekerState.Drift) DriftMove();
        else rb.linearVelocity = Vector2.zero;
    }

    private void UpdateDrift()
    {
        fireCooldown -= Time.deltaTime;
        if (fireCooldown <= 0f)
        {
            EnterFire();
            return;
        }

        float rel = transform.position.x - player.transform.position.x;
        if (rel > 0f) keepSide = 1;
        else if (rel < 0f) keepSide = -1;
    }

    private void DriftMove()
    {
        Vector2 playerPos = player.transform.position;
        Vector2 target = new(playerPos.x + desiredHorizontal * keepSide, playerPos.y + desiredHeight);
        Vector2 pos = transform.position;
        Vector2 to = target - pos;
        float dist = to.magnitude;

        if (dist <= driftStopRadius)
        {
            rb.linearVelocity = Vector2.zero;
            FacePlayer();
            return;
        }

        float distToPlayer = Vector2.Distance(pos, playerPos);
        float t = 1f - Mathf.Clamp01(distToPlayer / maxPlayerDistForSpeed);
        float spd = Mathf.Lerp(moveSpeedFar, moveSpeedNear, t);

        Vector2 v = to.normalized * spd;
        rb.linearVelocity = v;

        if (v.x > 0.01f) facingDirection = 1;
        else if (v.x < -0.01f) facingDirection = -1;
        transform.rotation = Quaternion.Euler(0f, facingDirection == -1 ? 180f : 0f, 0f);
    }

    private void EnterFire()
    {
        state = SeekerState.Fire;
        fireTimer = fireWindup;
        rb.linearVelocity = Vector2.zero;
        FacePlayer();
        if (animator != null) animator.Play("Fire");
    }

    private void UpdateFire()
    {
        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            FireOne();
            fireCooldown = Random.Range(fireIntervalRange.x, fireIntervalRange.y);
            state = SeekerState.Drift;
            if (animator != null) animator.Play("Drift");
        }
    }

    private void FireOne()
    {
        Vector2 dir = facingDirection == 1 ? Vector2.right : Vector2.left;
        SeekerProjectile proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(Vector3.forward, Vector3.up));
        proj.Initialize(this, player, dir);
        rb.AddForce(-dir * fireRecoilForce, ForceMode2D.Impulse);
    }

    private void UpdateDeath()
    {
        deathTimer -= Time.deltaTime;
        if (deathTimer <= 0f) Destroy(gameObject);
    }

    private void FacePlayer()
    {
        float dx = player.transform.position.x - transform.position.x;
        if (dx > 0f) facingDirection = 1;
        else if (dx < 0f) facingDirection = -1;
        transform.rotation = Quaternion.Euler(0f, facingDirection == -1 ? 180f : 0f, 0f);
    }

    public void OnHitByReflectedProjectile()
    {
        if (state == SeekerState.Death) return;
        state = SeekerState.Death;
        rb.linearVelocity = Vector2.zero;
        deathTimer = deathDespawn;
        if (animator != null) animator.Play("Death");
    }
}