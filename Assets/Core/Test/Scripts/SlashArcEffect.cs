using UnityEngine;

public sealed class SlashArcEffect : MonoBehaviour
{
    public float Life = 0.20f;
    public float StartScale = 1.0f;
    public float EndScale = 1.2f;
    public float FadeOut = 0.12f;

    private float deathAt;
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        deathAt = Time.time + Life;
        if (sr != null) sr.color = Color.white;
        transform.localScale = Vector3.one * StartScale;
    }

    private void Update()
    {
        float remain = deathAt - Time.time;
        if (remain <= 0f)
        {
            Destroy(gameObject);
            return;
        }
        float k = 1f - Mathf.Clamp01(remain / Life);
        float s = Mathf.Lerp(StartScale, EndScale, k);
        transform.localScale = new Vector3(s, s, 1f);
        if (sr != null && remain <= FadeOut)
        {
            Color c = sr.color;
            c.a = remain / Mathf.Max(0.0001f, FadeOut);
            sr.color = c;
        }
    }
}