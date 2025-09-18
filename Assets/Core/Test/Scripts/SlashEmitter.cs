using UnityEngine;

public sealed class SlashEmitter : MonoBehaviour
{
    public SlashRay SlashPrefab;
    public float Interval = 1.2f;
    public Transform Pivot;
    public float Duration = 0.20f;
    public float StartAngleDeg = 40f;
    public float EndAngleDeg = 140f;
    public float BladeInner = 0.15f;
    public float BladeOuter = 2.0f;
    public int Substeps = 3;
    public LayerMask HitMask;
    public bool DestroyOnHit = true;
    public GameObject ArcEffectPrefab;

    private float nextAt;

    private void Start()
    {
        nextAt = Time.time + Interval;
    }

    private void Update()
    {
        if (Time.time < nextAt) return;
        Spawn();
        nextAt = Time.time + Interval;
    }

    private void Spawn()
    {
        SlashRay s = Instantiate(SlashPrefab, transform.position, Quaternion.identity);
        s.Pivot = Pivot != null ? Pivot : transform;
        s.Duration = Duration;
        s.StartAngleDeg = StartAngleDeg;
        s.EndAngleDeg = EndAngleDeg;
        s.BladeInner = BladeInner;
        s.BladeOuter = BladeOuter;
        s.Substeps = Substeps;
        s.HitMask = HitMask;
        s.DestroyOnHit = DestroyOnHit;
        s.ArcEffectPrefab = ArcEffectPrefab;
    }
}