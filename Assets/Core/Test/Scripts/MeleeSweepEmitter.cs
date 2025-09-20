using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public sealed class MeleeSweepEmitter : MonoBehaviour
{
    public Transform Pivot;
    public float RadiusTip = 2.6f;
    public float RadiusMid = 1.6f;
    public float Thickness = 0.25f;
    public float StartAngleDeg = 40f;
    public float EndAngleDeg = -80f;
    public float ActiveDuration = 0.20f;
    public float Interval = 2.0f;
    public LayerMask HitMask;
    public int Damage = 1;
    public int VfxSegments = 18;
    public float VfxFade = 0.10f;

    private LineRenderer lr;
    private float lastFire;
    private HashSet<Collider2D> hitSet = new HashSet<Collider2D>();
    private Vector2 prevTip;
    private Vector2 prevMid;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 0;
        lr.useWorldSpace = true;
        lr.enabled = false;
    }

    private void Update()
    {
        if (Time.time - lastFire < Interval) return;
        lastFire = Time.time;
        StartCoroutine(SweepRoutine());
    }

    private IEnumerator SweepRoutine()
    {
        hitSet.Clear();
        float t0 = Time.time;
        float t1 = t0 + ActiveDuration;
        float a0 = StartAngleDeg;
        float a1 = EndAngleDeg;
        Vector3 pivot = Pivot != null ? Pivot.position : transform.position;
        Vector2 tip = PointOnCircle(pivot, RadiusTip, a0);
        Vector2 mid = PointOnCircle(pivot, RadiusMid, a0);
        prevTip = tip;
        prevMid = mid;
        lr.enabled = true;

        while (Time.time < t1)
        {
            float u = Mathf.InverseLerp(t0, t1, Time.time);
            float ang = Mathf.Lerp(a0, a1, u);
            tip = PointOnCircle(pivot, RadiusTip, ang);
            mid = PointOnCircle(pivot, RadiusMid, ang);
            SweepSegment(prevTip, tip);
            SweepSegment(prevMid, mid);
            prevTip = tip;
            prevMid = mid;
            DrawArc(pivot, Mathf.LerpAngle(a0, a1, 0f), ang);
            yield return new WaitForFixedUpdate();
        }

        DrawArc(pivot, a0, a1);
        yield return new WaitForSeconds(VfxFade);
        lr.positionCount = 0;
        lr.enabled = false;
    }

    private void SweepSegment(Vector2 a, Vector2 b)
    {
        float len = Vector2.Distance(a, b);
        int steps = Mathf.Max(1, Mathf.CeilToInt(len / (Thickness * 0.5f)));
        Vector2 dir = (b - a).normalized;
        float step = len / steps;
        for (int i = 0; i < steps; i++)
        {
            Vector2 p = a + dir * (i * step);
            Collider2D[] hits = Physics2D.OverlapCircleAll(p, Thickness * 0.5f, HitMask);
            if (hits == null || hits.Length == 0) continue;
            for (int k = 0; k < hits.Length; k++)
            {
                if (hitSet.Contains(hits[k])) continue;
                hitSet.Add(hits[k]);
                IDamageable d = hits[k].GetComponent<IDamageable>();
                if (d != null) d.Hit(Damage);
            }
        }
    }

    private Vector2 PointOnCircle(Vector3 c, float r, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(c.x + Mathf.Cos(rad) * r, c.y + Mathf.Sin(rad) * r);
    }

    private void DrawArc(Vector3 c, float a0, float a1)
    {
        int n = Mathf.Max(2, VfxSegments);
        lr.positionCount = n + 1;
        for (int i = 0; i <= n; i++)
        {
            float t = i / (float)n;
            float ang = Mathf.Lerp(a0, a1, t);
            Vector2 p = PointOnCircle(c, RadiusTip, ang);
            lr.SetPosition(i, p);
        }
        lr.widthMultiplier = Thickness * 0.8f;
    }
}
