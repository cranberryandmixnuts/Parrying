using UnityEngine;
using UnityEngine.VFX;

public sealed class BossVolleyLaserState : BossState
{
    private enum Phase
    {
        Missiles,
        Laser
    }

    private Phase phase;
    private float fireDuration;
    private float stateTimer;
    private float elapsed;
    private int missilesFired;

    private int missileCount;
    private float missileInterval;
    private float missilePhaseEndTime;

    private float laserTrackDuration;
    private float laserWarningDuration;
    private float laserActiveDuration;
    private float laserStartTime;

    private bool laserStarted;
    private bool laserInteractionDisabled;
    private bool laserDirectionLocked;

    private float laserTime;

    private Vector2 laserOrigin;
    private Vector2 laserDir;
    private float laserLength;
    private float laserThickness;
    private float laserAngleDeg;

    private bool stateStarted;
    private Coroutine teleportRoutine;

    private VisualEffect laserVfx;

    public override BossStateType StateType => BossStateType.VolleyLaser;

    public BossVolleyLaserState(BossController boss, BossStateMachine stateMachine)
        : base(boss, stateMachine) { }

    public override void Enter()
    {
        stateStarted = false;
        laserVfx = null;
        boss.SetLethal(BossController.AttackContext.LaserP1, false);
        boss.SetGravityScale(0f);
        boss.SetVelocityX(0f);
        boss.SetVelocityY(0f);
        boss.StopHorizontal();

        float playerX = boss.PlayerTarget.transform.position.x;
        float centerX = boss.VolleyCenter.position.x;
        float offset = boss.Settings.volleySideOffset;

        float targetX = playerX + (playerX < centerX ? offset : -offset);
        float targetY = boss.VolleyHeight.position.y;

        Vector3 point = new(targetX, targetY, boss.transform.position.z);

        teleportRoutine = boss.StartCoroutine(boss.TeleportRoutine(point, OnTeleported));
    }

    public override void Update()
    {
        if (!stateStarted) return;

        float dt = Time.deltaTime;

        stateTimer -= dt;
        if (stateTimer <= 0f)
        {
            EndState();
            return;
        }

        elapsed += dt;

        if (phase == Phase.Missiles)
        {
            UpdateMissiles();
            if (elapsed >= laserStartTime)
                phase = Phase.Laser;
            else
                return;
        }

        if (!laserStarted)
            StartLaser();

        laserTime = Mathf.Max(0f, elapsed - laserStartTime);
        UpdateLaser();
    }

    public override void FixedUpdate()
    {
        boss.StopHorizontal();
        boss.SetVelocityY(0f);
    }

    public override void Exit()
    {
        if(teleportRoutine != null) boss.StopCoroutine(teleportRoutine);
        teleportRoutine = null;
        boss.CancelTeleportEffects();

        stateStarted = false;
        boss.SetGravityScale(boss.OriginalGravityScale);
        boss.SetVelocityY(0f);
        boss.StopHorizontal();

        if (laserVfx != null)
        {
            EffectManager.Instance.EndLaser(laserVfx);
            laserVfx = null;
        }

        boss.SetLethal(BossController.AttackContext.None, false);
    }

    private void OnTeleported()
    {
        teleportRoutine = null;
        boss.FaceToPlayer();

        boss.Play(BossController.AnimFire);
        fireDuration = boss.AnimLen(BossController.AnimFire);
        stateTimer = fireDuration;
        elapsed = 0f;

        missileCount = Mathf.Max(0, boss.Settings.missileVolleys);
        missileInterval = boss.Settings.missileVolleyInterval;
        missilesFired = 0;

        float lastShotTime = (missileCount - 1) * missileInterval;
        missilePhaseEndTime = lastShotTime + missileInterval + boss.Settings.missileToLaserExtraDelay;

        laserTrackDuration = boss.Settings.laserWindupTime;
        laserWarningDuration = laserTrackDuration + boss.Settings.extraWarningTail;
        laserActiveDuration = boss.Settings.laserActiveTime;
        laserStartTime = missilePhaseEndTime;

        laserStarted = false;
        laserInteractionDisabled = false;
        laserDirectionLocked = false;
        laserTime = 0f;

        laserLength = boss.Settings.laserLength;
        laserThickness = boss.Settings.laserThickness;

        laserOrigin = Vector2.zero;
        laserDir = Vector2.right;
        laserAngleDeg = 0f;

        phase = Phase.Missiles;
        stateStarted = true;

        boss.SetLethal(BossController.AttackContext.LaserP1, false);
    }

    private void UpdateMissiles()
    {
        while (missilesFired < missileCount)
        {
            float nextShotTime = missilesFired * missileInterval;
            if (elapsed >= nextShotTime)
            {
                FireMissile();
                missilesFired++;
            }
            else
            {
                break;
            }
        }
    }

    private void FireMissile()
    {
        Vector2 origin = boss.transform.position;

        Vector2 dir = (Vector2)boss.PlayerTarget.transform.position - origin;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();

        EnemyProjectile proj = Object.Instantiate(boss.MissilePrefab, origin, Quaternion.identity);
        proj.Initialize(boss, boss.PlayerTarget, dir, boss.Settings.projectileDamage);
    }

    private void StartLaser()
    {
        laserStarted = true;
        laserTime = 0f;
        laserInteractionDisabled = false;
        laserDirectionLocked = false;

        laserOrigin = boss.transform.position;

        Vector2 initialDir = (Vector2)boss.PlayerTarget.transform.position - laserOrigin;
        if (initialDir.sqrMagnitude < 0.0001f) initialDir = Vector2.right;
        initialDir.Normalize();

        laserDir = initialDir;
        laserAngleDeg = Mathf.Atan2(laserDir.y, laserDir.x) * Mathf.Rad2Deg;

        laserVfx = EffectManager.Instance.BeginLaser(laserAngleDeg);

        boss.SetLethal(BossController.AttackContext.LaserP1, true);
    }

    private void UpdateLaser()
    {
        float totalLaserDuration = laserWarningDuration + laserActiveDuration;
        if (laserTime >= totalLaserDuration)
        {
            if (boss.LethalActive)
                boss.SetLethal(BossController.AttackContext.None, false);

            if (laserVfx != null)
            {
                EffectManager.Instance.EndLaser(laserVfx);
                laserVfx = null;
            }

            return;
        }

        laserOrigin = boss.transform.position;

        bool inWarning = laserTime < laserWarningDuration;
        bool inTracking = laserTime < laserTrackDuration;
        bool inFiring = laserTime >= laserWarningDuration;

        if (inWarning && inTracking)
            UpdateLaserDirectionTracking();
        else if (!laserDirectionLocked)
            laserDirectionLocked = true;

        if (!laserInteractionDisabled)
        {
            if (boss.LethalActive)
            {
                if (inWarning)
                {
                    float warningTailStart = laserWarningDuration - boss.Settings.extraWarningTail;
                    if (laserTime >= warningTailStart)
                        RegisterDashCandidates();
                }
                else if (inFiring)
                {
                    RegisterParryCandidates();
                    TryHitPlayer();
                }
            }
            else
                laserInteractionDisabled = true;
        }
    }

    private void UpdateLaserDirectionTracking()
    {
        Vector2 toPlayer = (Vector2)boss.PlayerTarget.transform.position - laserOrigin;
        if (toPlayer.sqrMagnitude < 0.0001f) return;

        float desiredAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
        float maxStep = boss.Settings.laserTurnSpeedDegPerSec * Time.deltaTime;
        laserAngleDeg = Mathf.MoveTowardsAngle(laserAngleDeg, desiredAngle, maxStep);

        float rad = laserAngleDeg * Mathf.Deg2Rad;
        laserDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        if (laserVfx != null)
            EffectManager.Instance.UpdateLaser(laserVfx, laserAngleDeg);
    }

    private void RegisterParryCandidates()
    {
        GetLaserRect(out Vector2 center, out Vector2 half, out float angleDeg);

        boss.PlayerTarget.GetParryDetectCircle(out Vector2 pCenter, out float pRadius);
        if (RectCircleIntersects(center, half, angleDeg, pCenter, pRadius))
            boss.PlayerTarget.RegisterParryCandidate(boss, boss.transform.position, boss.Settings.laserDamage);
    }

    private void RegisterDashCandidates()
    {
        GetLaserRect(out Vector2 center, out Vector2 half, out float angleDeg);

        boss.PlayerTarget.GetDashDetectCircle(out Vector2 dCenter, out float dRadius);
        if (RectCircleIntersects(center, half, angleDeg, dCenter, dRadius))
        {
            Vector2 tip = laserOrigin + laserDir * laserLength;
            boss.PlayerTarget.RegisterDashCandidate(tip);
        }
    }

    private void TryHitPlayer()
    {
        GetLaserRect(out Vector2 center, out Vector2 half, out float angleDeg);

        Vector2 size = new(half.x * 2f, half.y * 2f);
        Vector2 dir = laserDir.sqrMagnitude > 0.0001f ? laserDir : Vector2.right;

        RaycastHit2D[] hits = Physics2D.BoxCastAll(center, size, angleDeg, dir, 0f, boss.PlayerHitMask);
        for (int i = 0; i < hits.Length; i++)
        {
            PlayerController pc = hits[i].collider.GetComponentInParent<PlayerController>();
            if (pc == boss.PlayerTarget)
            {
                if (boss.PlayerTarget.TryHit(boss.Settings.laserDamage, boss.transform.position))
                {
                    boss.PlayerTarget.ClearParryCandidate(boss);
                    boss.SetLethal(BossController.AttackContext.None, false);
                    laserInteractionDisabled = true;
                }
                break;
            }
        }
    }

    private void GetLaserRect(out Vector2 center, out Vector2 half, out float angleDeg)
    {
        Vector2 dir = laserDir;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();

        center = laserOrigin + dir * (laserLength * 0.5f);
        half = new Vector2(laserLength * 0.5f, laserThickness * 0.5f);
        angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    private bool RectCircleIntersects(Vector2 rectCenter, Vector2 rectHalf, float angleDeg, Vector2 circleCenter, float radius)
    {
        float rad = -angleDeg * Mathf.Deg2Rad;
        Vector2 d = circleCenter - rectCenter;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        Vector2 local = new(d.x * cos - d.y * sin, d.x * sin + d.y * cos);
        float cx = Mathf.Clamp(local.x, -rectHalf.x, rectHalf.x);
        float cy = Mathf.Clamp(local.y, -rectHalf.y, rectHalf.y);
        float dx = local.x - cx;
        float dy = local.y - cy;
        float r2 = radius * radius;
        return dx * dx + dy * dy <= r2;
    }

    private void EndState()
    {
        boss.SetLethal(BossController.AttackContext.None, false);

        if (laserVfx != null)
        {
            EffectManager.Instance.EndLaser(laserVfx);
            laserVfx = null;
        }

        boss.ChangeToIdle(false);
    }
}