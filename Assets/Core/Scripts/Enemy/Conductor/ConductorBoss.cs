using System.Collections.Generic;
using UnityEngine;

public sealed class ConductorBoss : EnemyBase, IParryReactive
{
    public enum AttackContext
    {
        None,
        Sword,
        Plunge,
        Rush,
        LaserP1,
        LaserP2
    }

    private const string AnimIdle = "Idle";
    private const string AnimSwordTele = "SwordTele";
    private const string AnimSword = "Sword";
    private const string AnimSwordRecover = "SwordRecover";
    private const string AnimPlungeTele = "PlungeTele";
    private const string AnimPlungeDive = "PlungeDive";
    private const string AnimRush = "Rush";
    private const string AnimRushRecover = "RushRecover";
    private const string AnimMissilePre = "MissilePre";
    private const string AnimMissile = "Missile";
    private const string AnimLaserWindup = "LaserWindup";
    private const string AnimLaser = "Laser";
    private const string AnimLaserRecover = "LaserRecover";
    private const string AnimP2Intro = "P2Intro";
    private const string AnimP2Radial = "P2Radial";
    private const string AnimGroggy = "Groggy";
    private const string AnimDeath = "Death";

    [Header("Common")]
    [SerializeField] private LayerMask playerHitMask;
    [SerializeField] private int p1Stacks = 3;
    [SerializeField] private int p2Stacks = 1;
    [SerializeField] private float idleDelay = 0.6f;
    [SerializeField] private float groggyDuration = 2.2f;

    [Header("Sword Drop")]
    [SerializeField] private Collider2D swordCollider;
    [SerializeField] private Transform leftTop;
    [SerializeField] private Transform rightTop;
    [SerializeField] private float swordWarningTime = 0.6f;
    [SerializeField] private float swordActiveTime = 0.35f;
    [SerializeField] private int swordDamage = 16;

    [Header("Plunge + Rush")]
    [SerializeField] private Collider2D plungeCollider;
    [SerializeField] private Transform ceilingPoint;
    [SerializeField] private float plungeTeleTime = 0.45f;
    [SerializeField] private float plungeActiveTime = 0.28f;
    [SerializeField] private int plungeDamage = 18;
    [SerializeField] private Collider2D rushCollider;
    [SerializeField] private float rushSpeed = 12f;
    [SerializeField] private float rushMaxTime = 2.0f;
    [SerializeField] private float missBehindTime = 0.6f;
    [SerializeField] private int rushDamage = 14;

    [Header("Missile + Laser")]
    [SerializeField] private ConductorMissile missilePrefab;
    [SerializeField] private Transform[] missileMuzzles;
    [SerializeField] private int missileVolleys = 3;
    [SerializeField] private float missileVolleyInterval = 0.5f;
    [SerializeField] private Collider2D chestLaserCollider;
    [SerializeField] private float laserWindupTime = 0.6f;
    [SerializeField] private float laserActiveTime = 1.2f;
    [SerializeField] private int laserDamage = 12;

    [Header("P2 Radial")]
    [SerializeField] private Collider2D radialLaserCollider;
    [SerializeField] private int radialSets = 12;
    [SerializeField] private float radialBeat = 0.25f;
    [SerializeField] private float radialActiveEach = 0.18f;
    [SerializeField] private int radialDamage = 12;

    private readonly Collider2D[] overlapResults = new Collider2D[8];

    private BossStateMachine stateMachine;
    private BossIdleState sIdle;
    private BossGroggyState sGroggy;
    private BossSwordDropState sSword;
    private BossPlungeRushState sPlungeRush;
    private BossVolleyLaserState sVolleyLaser;
    private BossRadialLaserState sRadial;
    private BossDeathState sDeath;

    private bool lethalActive;
    private AttackContext attackCx;

    public PlayerController PlayerTarget
    {
        get { return Player; }
    }

    public float FacingDir
    {
        get { return FacingDirection; }
    }

    protected override string DeathAnimName
    {
        get { return AnimDeath; }
    }

    protected override void Start()
    {
        base.Start();
        stateMachine = new BossStateMachine();
        sIdle = new BossIdleState(this, stateMachine);
        sGroggy = new BossGroggyState(this, stateMachine);
        sSword = new BossSwordDropState(this, stateMachine);
        sPlungeRush = new BossPlungeRushState(this, stateMachine);
        sVolleyLaser = new BossVolleyLaserState(this, stateMachine);
        sRadial = new BossRadialLaserState(this, stateMachine);
        sDeath = new BossDeathState(this, stateMachine);
        stateMachine.ChangeState(sIdle);
    }

    protected override void OnUpdate()
    {
        stateMachine.Tick();
    }

    protected override void OnFixedUpdate()
    {
        stateMachine.FixedTick();
    }

    public void GoIdle()
    {
        sIdle.SetDelay(idleDelay);
        stateMachine.ChangeState(sIdle);
    }

    public void GoGroggy()
    {
        sGroggy.SetDuration(groggyDuration);
        stateMachine.ChangeState(sGroggy);
    }

    public void GoSwordDrop()
    {
        sSword.Configure(leftTop, rightTop, swordCollider, swordWarningTime, swordActiveTime, swordDamage);
        stateMachine.ChangeState(sSword);
    }

    public void GoPlungeRush()
    {
        sPlungeRush.Configure(ceilingPoint, plungeCollider, plungeTeleTime, plungeActiveTime, plungeDamage, rushCollider, rushSpeed, rushMaxTime, missBehindTime, rushDamage);
        stateMachine.ChangeState(sPlungeRush);
    }

    public void GoVolleyLaser()
    {
        sVolleyLaser.Configure(missilePrefab, missileMuzzles, missileVolleys, missileVolleyInterval, chestLaserCollider, laserWindupTime, laserActiveTime, laserDamage);
        stateMachine.ChangeState(sVolleyLaser);
    }

    public void GoRadialLaser()
    {
        sRadial.Configure(radialLaserCollider, radialSets, radialBeat, radialActiveEach, radialDamage);
        stateMachine.ChangeState(sRadial);
    }

    public void GoDeath()
    {
        stateMachine.ChangeState(sDeath);
    }

    public void ChooseP1()
    {
        int r = Random.Range(0, 3);
        if (r == 0) GoSwordDrop();
        else if (r == 1) GoPlungeRush();
        else GoVolleyLaser();
    }

    public void ConsumeP1Stack()
    {
        if (p1Stacks > 0) p1Stacks -= 1;
    }

    public bool HasP1Stacks
    {
        get { return p1Stacks > 0; }
    }

    public void ConsumeP2Stack()
    {
        if (p2Stacks > 0) p2Stacks -= 1;
    }

    public bool HasP2Stacks
    {
        get { return p2Stacks > 0; }
    }

    public void SetLethal(AttackContext cx, bool on)
    {
        attackCx = cx;
        lethalActive = on;
        if (on) Player.ClearParryCandidate(this);
    }

    public void Play(string anim)
    {
        Anim.Play(anim);
    }

    public float AnimLen(string anim)
    {
        return GetAnimLength(anim);
    }

    public void FaceToPlayer()
    {
        FacePlayer();
    }

    public void Teleport(Vector3 p)
    {
        transform.position = p;
    }

    public int HandleHitbox(Collider2D hitCol, int damage)
    {
        Player.GetParryDetectCircle(out Vector2 pCenter, out float pRadius);
        if (IsColliderWithinCircle(hitCol, pCenter, pRadius)) Player.RegisterParryCandidate(this, hitCol.bounds.center, damage);

        Player.GetDashDetectCircle(out Vector2 dCenter, out float dRadius);
        if (IsColliderWithinCircle(hitCol, dCenter, dRadius)) Player.RegisterDashCandidate(hitCol.bounds.center);

        ContactFilter2D f = new();
        f.SetLayerMask(playerHitMask);
        f.useTriggers = true;
        int hitCount = hitCol.Overlap(f, overlapResults);
        for (int i = 0; i < hitCount; i++)
        {
            PlayerController pc = overlapResults[i].GetComponentInParent<PlayerController>();
            if (pc == Player)
            {
                Player.Hit(damage, hitCol.bounds.center);
                Player.ClearParryCandidate(this);
                SetLethal(AttackContext.None, false);
                return 1;
            }
        }
        return 0;
    }

    public Transform ChooseSideTop()
    {
        float px = Player.transform.position.x;
        float dl = Mathf.Abs(px - leftTop.position.x);
        float dr = Mathf.Abs(px - rightTop.position.x);
        if (dl < dr) return leftTop;
        else return rightTop;
    }

    public void FireMissilesOnce()
    {
        int n = missileMuzzles != null ? missileMuzzles.Length : 0;
        for (int i = 0; i < n; i++)
        {
            Transform m = missileMuzzles[i];
            ConductorMissile proj = Instantiate(missilePrefab, m.position, m.rotation);
            proj.Initialize(this, Player, m.right);
        }
    }

    public void OnMissileReflectedHit()
    {
        if (HasP1Stacks)
        {
            ConsumeP1Stack();
            GoGroggy();
        }
        else
        {
            if (HasP2Stacks)
            {
                ConsumeP2Stack();
                GoGroggy();
            }
            else GoDeath();
        }
    }

    public void OnPerfectParry(Vector2 hitPoint)
    {
        if (!lethalActive) return;
        Player.ClearParryCandidate(this);
        SetLethal(AttackContext.None, false);
    }

    public void OnImperfectParry(Vector2 hitPoint)
    {
        if (!lethalActive) return;
        Player.ClearParryCandidate(this);
        SetLethal(AttackContext.None, false);
    }

    public void OnCounterParry(Vector2 hitPoint)
    {
        if (!lethalActive) return;
        Player.ClearParryCandidate(this);
        SetLethal(AttackContext.None, false);
        if (attackCx == AttackContext.LaserP2)
        {
            GoDeath();
            return;
        }
        if (HasP1Stacks)
        {
            ConsumeP1Stack();
            GoGroggy();
            return;
        }
        if (HasP2Stacks)
        {
            ConsumeP2Stack();
            GoGroggy();
            return;
        }
        GoDeath();
    }

    public string IdleAnim
    {
        get { return AnimIdle; }
    }

    public string SwordTeleAnim
    {
        get { return AnimSwordTele; }
    }

    public string SwordAnim
    {
        get { return AnimSword; }
    }

    public string SwordRecoverAnim
    {
        get { return AnimSwordRecover; }
    }

    public string PlungeTeleAnim
    {
        get { return AnimPlungeTele; }
    }

    public string PlungeAnim
    {
        get { return AnimPlungeDive; }
    }

    public string RushAnim
    {
        get { return AnimRush; }
    }

    public string RushRecoverAnim
    {
        get { return AnimRushRecover; }
    }

    public string MissilePreAnim
    {
        get { return AnimMissilePre; }
    }

    public string MissileAnim
    {
        get { return AnimMissile; }
    }

    public string LaserWindupAnim
    {
        get { return AnimLaserWindup; }
    }

    public string LaserAnim
    {
        get { return AnimLaser; }
    }

    public string LaserRecoverAnim
    {
        get { return AnimLaserRecover; }
    }

    public string P2IntroAnim
    {
        get { return AnimP2Intro; }
    }

    public string P2RadialAnim
    {
        get { return AnimP2Radial; }
    }

    public string GroggyAnim
    {
        get { return AnimGroggy; }
    }

    public bool LethalActive
    {
        get { return lethalActive; }
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

    public void SetVelocityX(float v)
    {
        Body.linearVelocity = new Vector2(v, Body.linearVelocity.y);
    }

    public void StopHorizontal()
    {
        Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
    }

    public void SetLethalSword(bool on)
    {
        SetLethal(AttackContext.Sword, on);
    }

    public void SetLethalPlunge(bool on)
    {
        SetLethal(AttackContext.Plunge, on);
    }

    public void SetLethalRush(bool on)
    {
        SetLethal(AttackContext.Rush, on);
    }

    public void SetLethalLaserP1(bool on)
    {
        SetLethal(AttackContext.LaserP1, on);
    }

    public void SetLethalLaserP2(bool on)
    {
        SetLethal(AttackContext.LaserP2, on);
    }

    public LayerMask PlayerHitMask
    {
        get { return playerHitMask; }
    }

    public int SwordDamage
    {
        get { return swordDamage; }
    }

    public int PlungeDamage
    {
        get { return plungeDamage; }
    }

    public int RushDamage
    {
        get { return rushDamage; }
    }

    public int LaserDamage
    {
        get { return laserDamage; }
    }

    public int RadialDamage
    {
        get { return radialDamage; }
    }

    public Collider2D SwordCol
    {
        get { return swordCollider; }
    }

    public Collider2D PlungeCol
    {
        get { return plungeCollider; }
    }

    public Collider2D RushCol
    {
        get { return rushCollider; }
    }

    public Collider2D ChestLaserCol
    {
        get { return chestLaserCollider; }
    }

    public Collider2D RadialLaserCol
    {
        get { return radialLaserCollider; }
    }
}
