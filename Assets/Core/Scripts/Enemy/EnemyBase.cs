using UnityEngine;

using Sirenix.OdinInspector;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public abstract class EnemyBase : MonoBehaviour
{
    private bool dead;

    protected PlayerController Player { get; private set; }
    protected Rigidbody2D Body { get; private set; }
    protected Animator Anim { get; private set; }
    protected int FacingDirection { get; private set; } = 1;

    protected float DeathDespawnDelay = 1f;

    protected virtual string DeathAnimName => null;

    protected virtual void Awake()
    {
        Body = GetComponent<Rigidbody2D>();
        Anim = GetComponent<Animator>();
    }

    protected virtual void Start() => Player = PlayerController.Instance;

    private void Update()
    {
        if (dead) return;
        OnUpdate();
    }

    private void FixedUpdate()
    {
        if (dead) return;
        OnFixedUpdate();
    }

    protected virtual void OnUpdate() { }

    protected virtual void OnFixedUpdate() { }

    protected void FacePlayer()
    {
        float dx = Player.transform.position.x - transform.position.x;

        if (dx > 0f) ApplyFacing(1);
        else if (dx < 0f) ApplyFacing(-1);
        else ApplyFacing(FacingDirection);
    }

    protected void ApplyFacing(int dir)
    {
        if (dir > 0) FacingDirection = 1;
        else if (dir < 0) FacingDirection = -1;

        transform.rotation = Quaternion.Euler(0f, FacingDirection == -1 ? 180f : 0f, 0f);
    }

    protected float GetAnimLength(string stateName)
    {
        AnimatorStateInfo current = Anim.GetCurrentAnimatorStateInfo(0);
        if (current.IsName(stateName))
        {
            float global = Anim.speed;
            if (global <= 0f) return Mathf.Infinity;
            return current.length / global;
        }

        AnimatorStateInfo next = Anim.GetNextAnimatorStateInfo(0);
        if (next.IsName(stateName))
        {
            float global = Anim.speed;
            if (global <= 0f) return Mathf.Infinity;
            return next.length / global;
        }

        Anim.Update(0f);

        current = Anim.GetCurrentAnimatorStateInfo(0);
        if (current.IsName(stateName))
        {
            float global = Anim.speed;
            if (global <= 0f) return Mathf.Infinity;
            return current.length / global;
        }

        next = Anim.GetNextAnimatorStateInfo(0);
        if (next.IsName(stateName))
        {
            float global = Anim.speed;
            if (global <= 0f) return Mathf.Infinity;
            return next.length / global;
        }

        Debug.LogError($"EnemyBase: Animator state '{stateName}' not found or not playing.");
        return 0f;
    }

    public virtual void Die()
    {
        if (dead) return;
        dead = true;

        if (Body != null)
        {
            Body.linearVelocity = Vector2.zero;
            Body.simulated = false;
        }

        if (this is IParryReactive parry) Player.ClearParryCandidate(parry);

        string deathAnim = DeathAnimName;
        if (!string.IsNullOrEmpty(deathAnim)) Anim.Play(deathAnim);

        float delay = DeathDespawnDelay;
        if (delay < 0f && !string.IsNullOrEmpty(deathAnim)) delay = GetAnimLength(deathAnim);

        if (delay > 0f) Destroy(gameObject, delay);
        else Destroy(gameObject);
    }

    public bool IsDead() => dead;
}