using UnityEngine;
using UnityEngine.VFX;

public sealed class BossRadialLaserState : BossState
{
    private float delayTimer;
    private float volleyTimer;
    private bool volleyActive;

    private float laserWarningDuration;
    private float laserActiveDuration;
    private float totalLaserDuration;

    private Vector2[] beamDirs;
    private float[] beamAnglesDeg;
    private Vector2 laserOrigin;
    private float laserLength;
    private float laserThickness;

    private bool interactionsDisabled;

    private VisualEffect[] beamVfx;

    public override BossStateType StateType => BossStateType.RadialLaser;

    public BossRadialLaserState(BossController boss, BossStateMachine stateMachine)
        : base(boss, stateMachine) { }

    public override void Enter()
    {
        TeleportToRadialPosition();

        boss.SetGravityScale(0f);
        boss.SetVelocityX(0f);
        boss.SetVelocityY(0f);
        boss.StopHorizontal();

        boss.Play(BossController.AnimCrackLaser);

        laserWarningDuration = boss.Settings.laserWindupTime + boss.Settings.extraWarningTail;
        laserActiveDuration = boss.Settings.laserActiveTime;
        totalLaserDuration = laserWarningDuration + laserActiveDuration;

        laserLength = boss.Settings.laserLength;
        laserThickness = boss.Settings.laserThickness;

        delayTimer = boss.Settings.radialBeat;
        volleyTimer = 0f;
        volleyActive = false;
        interactionsDisabled = false;

        beamDirs = null;
        beamAnglesDeg = null;
        laserOrigin = boss.transform.position;

        beamVfx = null;

        boss.SetLethal(BossController.AttackContext.LaserP2, false);
    }

    public override void Update()
    {
        float dt = Time.deltaTime;

        if (!volleyActive)
        {
            delayTimer -= dt;
            if (delayTimer > 0f)
                return;

            StartVolley();
            return;
        }

        volleyTimer += dt;

        if (volleyTimer >= totalLaserDuration)
        {
            EndVolley();
            return;
        }

        laserOrigin = boss.transform.position;

        bool inWarning = volleyTimer < laserWarningDuration;
        bool inFiring = volleyTimer >= laserWarningDuration;

        if (!interactionsDisabled && boss.LethalActive)
        {
            if (inWarning)
            {
                float warningTailStart = laserWarningDuration - boss.Settings.extraWarningTail;
                if (volleyTimer >= warningTailStart)
                    RegisterDashCandidates();
            }
            else if (inFiring)
            {
                RegisterParryCandidates();
                TryHitPlayer();
            }
        }
        else if (!boss.LethalActive)
        {
            interactionsDisabled = true;
        }
    }

    public override void FixedUpdate()
    {
        boss.StopHorizontal();
        boss.SetVelocityY(0f);
    }

    public override void Exit()
    {
        boss.SetGravityScale(boss.OriginalGravityScale);
        boss.SetVelocityY(0f);
        boss.StopHorizontal();
        boss.SetLethal(BossController.AttackContext.LaserP2, false);

        StopBeamVfx();
    }

    private void TeleportToRadialPosition()
    {
        Transform target = boss.RadialLaserPoint;
        Vector3 p = new(target.position.x, target.position.y, boss.transform.position.z);
        boss.Teleport(p);
        boss.FaceToPlayer();
    }

    private void StartVolley()
    {
        int minBeams = Mathf.Max(1, Mathf.RoundToInt(boss.Settings.radialBeamCountRange.x));
        int maxBeams = Mathf.Max(minBeams, Mathf.RoundToInt(boss.Settings.radialBeamCountRange.y));
        int beamCount = Random.Range(minBeams, maxBeams + 1);

        beamDirs = new Vector2[beamCount];
        beamAnglesDeg = new float[beamCount];

        float minSep = Mathf.Max(0f, boss.Settings.radialMinAngleSeparationDeg);

        for (int i = 0; i < beamCount; i++)
        {
            float angle = Random.Range(0f, 360f);

            int safety = 0;
            while (safety < 32)
            {
                bool tooClose = false;

                for (int j = 0; j < i; j++)
                {
                    float diff = Mathf.Abs(Mathf.DeltaAngle(angle, beamAnglesDeg[j]));
                    if (diff < minSep)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose) break;

                angle = Random.Range(0f, 360f);
                safety++;
            }

            beamAnglesDeg[i] = angle;
            float rad = angle * Mathf.Deg2Rad;
            beamDirs[i] = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        StopBeamVfx();
        beamVfx = new VisualEffect[beamCount];
        for (int i = 0; i < beamCount; i++)
            beamVfx[i] = EffectManager.Instance.BeginLaser(beamAnglesDeg[i]);

        volleyTimer = 0f;
        volleyActive = true;
        interactionsDisabled = false;

        boss.SetLethal(BossController.AttackContext.LaserP2, true);
    }

    private void EndVolley()
    {
        volleyActive = false;
        volleyTimer = 0f;
        interactionsDisabled = false;

        boss.SetLethal(BossController.AttackContext.LaserP2, false);

        delayTimer = boss.Settings.radialBeat;

        StopBeamVfx();

        beamDirs = null;
        beamAnglesDeg = null;
    }

    private void StopBeamVfx()
    {
        if (beamVfx == null) return;

        for (int i = 0; i < beamVfx.Length; i++)
        {
            if (beamVfx[i] != null)
                EffectManager.Instance.EndLaser(beamVfx[i]);
        }

        beamVfx = null;
    }

    private void RegisterDashCandidates()
    {
        if (beamDirs == null || beamDirs.Length == 0)
            return;

        boss.PlayerTarget.GetDashDetectCircle(out Vector2 dCenter, out float dRadius);

        for (int i = 0; i < beamDirs.Length; i++)
        {
            GetBeamRect(beamDirs[i], out Vector2 center, out Vector2 half, out float angleDeg);
            if (RectCircleIntersects(center, half, angleDeg, dCenter, dRadius))
            {
                Vector2 dir = beamDirs[i];
                if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
                dir.Normalize();

                Vector2 tip = laserOrigin + dir * laserLength;
                boss.PlayerTarget.RegisterDashCandidate(tip);
                break;
            }
        }
    }

    private void RegisterParryCandidates()
    {
        boss.PlayerTarget.GetParryDetectCircle(out Vector2 pCenter, out float pRadius);

        for (int i = 0; i < beamDirs.Length; i++)
        {
            GetBeamRect(beamDirs[i], out Vector2 center, out Vector2 half, out float angleDeg);
            if (RectCircleIntersects(center, half, angleDeg, pCenter, pRadius))
            {
                boss.PlayerTarget.RegisterParryCandidate(boss, boss.transform.position, boss.Settings.radialDamage);
                break;
            }
        }
    }

    private void TryHitPlayer()
    {
        for (int i = 0; i < beamDirs.Length; i++)
        {
            GetBeamRect(beamDirs[i], out Vector2 center, out Vector2 half, out float angleDeg);

            Vector2 size = new(half.x * 2f, half.y * 2f);
            Vector2 dir = beamDirs[i].sqrMagnitude > 0.0001f ? beamDirs[i] : Vector2.right;

            RaycastHit2D[] hits = Physics2D.BoxCastAll(center, size, angleDeg, dir, 0f, boss.PlayerHitMask);
            for (int h = 0; h < hits.Length; h++)
            {
                PlayerController pc = hits[h].collider.GetComponentInParent<PlayerController>();
                if (pc == boss.PlayerTarget)
                {
                    if (boss.PlayerTarget.TryHit(boss.Settings.radialDamage, boss.transform.position))
                    {
                        boss.PlayerTarget.ClearParryCandidate(boss);
                        boss.SetLethal(BossController.AttackContext.None, false);
                        interactionsDisabled = true;
                    }

                    return;
                }
            }
        }
    }

    private void GetBeamRect(Vector2 dir, out Vector2 center, out Vector2 half, out float angleDeg)
    {
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
}