using UnityEngine;

public sealed class SlashRay : MonoBehaviour
{
    public Transform Pivot;
    public float Duration = 0.20f;
    public float StartAngleDeg = 140f;
    public float EndAngleDeg = 40f;
    public float BladeInner = 0.15f;
    public float BladeOuter = 2.0f;
    public int Substeps = 3;
    public LayerMask HitMask;
    public bool DestroyOnHit = true;
    public GameObject ArcEffectPrefab;

    private float t;
    private float prevAngle;
    private GameObject arcFx;

    private void OnEnable()
    {
        t = 0f;
        prevAngle = StartAngleDeg;
        if (ArcEffectPrefab != null) arcFx = Instantiate(ArcEffectPrefab);
    }

    private void Update()
    {
        if (Pivot == null) return;
        t += Time.deltaTime / Duration;
        float curAngle = Mathf.Lerp(StartAngleDeg, EndAngleDeg, Mathf.Clamp01(t));
        int steps = Mathf.Max(1, Substeps);
        for (int i = 1; i <= steps; i++)
        {
            float a = Mathf.Lerp(prevAngle, curAngle, i / (float)steps);
            Vector2 dir = new(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
            Vector2 from = (Vector2)Pivot.position + dir * BladeInner;
            Vector2 to = (Vector2)Pivot.position + dir * BladeOuter;
            RaycastHit2D[] hits = Physics2D.LinecastAll(from, to, HitMask);
            if (hits != null && hits.Length > 0)
            {
                if (DestroyOnHit)
                {
                    if (arcFx != null) Destroy(arcFx);
                    Destroy(gameObject);
                }
                return;
            }
            UpdateArcFx(from, to);
        }
        prevAngle = curAngle;
        if (t >= 1f)
        {
            if (arcFx != null) Destroy(arcFx);
            Destroy(gameObject);
        }
    }

    private void UpdateArcFx(Vector2 from, Vector2 to)
    {
        if (arcFx == null) return;
        Vector2 mid = Vector2.Lerp(from, to, 0.5f);
        Vector2 tangent = (to - from).normalized;
        float z = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
        arcFx.transform.SetPositionAndRotation(mid, Quaternion.Euler(0f, 0f, z));
    }
}