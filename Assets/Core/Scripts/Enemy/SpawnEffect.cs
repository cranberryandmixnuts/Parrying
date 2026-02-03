using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class SpawnEffect : MonoBehaviour
{
    [SerializeField, Required] private GameObject mainEnemy;
    [SerializeField, Required] private SpriteRenderer outline;
    [SerializeField, Required] private SpriteRenderer silhouette;
    [SerializeField] private float outLineWarningDuration = 1.5f;
    [SerializeField] private float silhouetteFadeInDuration = 1f;

    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Start()
    {
        SetOutlineAlpha(0f);
        SetSilhouetteAlpha(1f);
        mainEnemy.SetActive(false);

        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        float seg = outLineWarningDuration * 0.25f;

        seq.Append(DOVirtual.Float(0f, 1f, seg, SetOutlineAlpha));
        seq.Append(DOVirtual.Float(1f, 0f, seg, SetOutlineAlpha));
        seq.Append(DOVirtual.Float(0f, 1f, seg, SetOutlineAlpha));
        seq.Append(DOVirtual.Float(1f, 0f, seg, SetOutlineAlpha));

        seq.AppendCallback(() => mainEnemy.SetActive(true));
        seq.Append(DOVirtual.Float(silhouette.material.GetColor(ColorId).a, 0f, silhouetteFadeInDuration, SetSilhouetteAlpha));
    }

    private void SetOutlineAlpha(float alpha)
    {
        Color c = outline.color;
        c.a = Mathf.Clamp01(alpha);
        outline.color = c;
    }

    private void SetSilhouetteAlpha(float alpha)
    {
        Color c = silhouette.material.GetColor(ColorId);
        c.a = Mathf.Clamp01(alpha);
        silhouette.material.SetColor(ColorId, c);
    }
}