using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public abstract class EnemyBase : MonoBehaviour
{
    private bool dead;

    protected PlayerController Player { get; private set; }
    protected Rigidbody2D Body { get; private set; }
    protected Animator Anim { get; private set; }
    protected int FacingDirection { get; private set; } = 1;

    protected virtual string DeathAnimName
    {
        get { return null; }
    }

    protected virtual float DeathDespawnDelay
    {
        get { return 1f; }
    }

    protected virtual void Awake()
    {
        Body = GetComponent<Rigidbody2D>();
        Anim = GetComponent<Animator>();
    }

    protected virtual void Start()
    {
        Player = PlayerController.Instance;
    }

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

    protected virtual void OnUpdate()
    {
    }

    protected virtual void OnFixedUpdate()
    {
    }

    protected void FacePlayer()
    {
        float dx = Player.transform.position.x - transform.position.x;
        if (dx > 0f) FacingDirection = 1;
        else if (dx < 0f) FacingDirection = -1;
        transform.rotation = Quaternion.Euler(0f, FacingDirection == -1 ? 180f : 0f, 0f);
    }

    protected void ApplyFacing(int dir)
    {
        if (dir > 0) FacingDirection = 1;
        else if (dir < 0) FacingDirection = -1;
        transform.rotation = Quaternion.Euler(0f, FacingDirection == -1 ? 180f : 0f, 0f);
    }

    protected void PlayAnim(string name)
    {
        if (Anim != null && !string.IsNullOrEmpty(name)) Anim.Play(name);
    }

    protected float GetAnimLength(string clipName)
    {
        if (Anim == null) return 0f;
        if (Anim.runtimeAnimatorController == null) return 0f;
        if (string.IsNullOrEmpty(clipName)) return 0f;

        AnimationClip[] clips = Anim.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i].name == clipName)
                return clips[i].length;
        }

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

        if (Player != null)
        {
            if (this is IParryReactive parry) Player.ClearParryCandidate(parry);
        }

        if (Anim != null && !string.IsNullOrEmpty(DeathAnimName)) Anim.Play(DeathAnimName);

        float delay = DeathDespawnDelay;
        if (delay > 0f) Destroy(gameObject, delay);
        else Destroy(gameObject);
    }

    public bool IsDead()
    {
        return dead;
    }
}