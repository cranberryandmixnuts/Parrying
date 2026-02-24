using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class BossTeleportEffectManager : MonoBehaviour
{
    [Header("Boss")]
    [SerializeField, Required] private SpriteRenderer bossSpriteRenderer;

    [Header("Effect Roots")]
    [SerializeField, Required] private Transform startPointRoot;
    [SerializeField, Required] private Transform endPointRoot;
    [SerializeField, Required] private Transform lineRoot;
    [SerializeField, Required] private Transform silhouetteRoot;

    [Header("Effect Renderers")]
    [SerializeField, Required] private SpriteRenderer startPointRenderer;
    [SerializeField, Required] private SpriteRenderer endPointRenderer;
    [SerializeField, Required] private SpriteRenderer lineRenderer;
    [SerializeField, Required] private SpriteRenderer silhouetteSpriteRenderer;

    [Header("Reset")]
    [SerializeField, Required] private Transform resetTransform;

    [Header("Fade")]
    [SerializeField, MinValue(0f)] private float fadeDuration = 0.5f;

    [Header("Start Point")]
    [SerializeField] private Vector3 startPointOffset = Vector3.zero;
    [SerializeField] private Vector3 startPointActiveScale = Vector3.one;

    [Header("End Point")]
    [SerializeField] private Vector3 endPointOffset = Vector3.zero;
    [SerializeField] private Vector3 endPointActiveScale = Vector3.one;

    [Header("Line")]
    [SerializeField] private Vector3 lineOffset = Vector3.zero;
    [SerializeField] private Vector3 lineActiveScale = Vector3.one;
    [SerializeField, MinValue(0f)] private float lineLengthMultiplier = 1f;

    [Header("Silhouette")]
    [SerializeField] private Vector3 silhouetteOffset = Vector3.zero;
    [SerializeField] private Vector3 silhouetteScaleMultiplier = Vector3.one;

    private readonly List<SpriteRenderer> fadeRenderers = new();
    private readonly List<Color> baseColors = new();
    private Sequence activeSequence;

    private void Awake()
    {
        CacheFadeRenderers();
        ResetToStandbyImmediate();
    }

    private void OnDisable() => ForceReset();

    public void Play(Vector3 fromPosition, Vector3 toPosition)
    {
        if (activeSequence != null && activeSequence.IsActive())
            activeSequence.Kill();

        ApplyStartPoint(fromPosition);
        ApplyEndPoint(toPosition);
        ApplyLine(fromPosition, toPosition);
        ApplySilhouette(fromPosition);
        RestoreBaseColors();

        activeSequence = DOTween.Sequence();

        for (int i = 0; i < fadeRenderers.Count; i++)
            activeSequence.Join(fadeRenderers[i].DOFade(0f, fadeDuration).SetEase(Ease.Linear));

        activeSequence.OnComplete(ResetToStandbyImmediate);
    }

    [Button]
    public void ForceReset()
    {
        if (activeSequence != null && activeSequence.IsActive())
            activeSequence.Kill();

        ResetToStandbyImmediate();
    }

    private void CacheFadeRenderers()
    {
        fadeRenderers.Clear();
        baseColors.Clear();

        RegisterRenderer(startPointRenderer);
        RegisterRenderer(endPointRenderer);
        RegisterRenderer(lineRenderer);
        RegisterRenderer(silhouetteSpriteRenderer);
    }

    private void RegisterRenderer(SpriteRenderer renderer)
    {
        fadeRenderers.Add(renderer);
        baseColors.Add(renderer.color);
    }

    private void ApplyStartPoint(Vector3 fromPosition)
    {
        startPointRoot.SetPositionAndRotation(fromPosition + startPointOffset, Quaternion.identity);
        startPointRoot.localScale = startPointActiveScale;
    }

    private void ApplyEndPoint(Vector3 toPosition)
    {
        endPointRoot.SetPositionAndRotation(toPosition + endPointOffset, Quaternion.identity);
        endPointRoot.localScale = endPointActiveScale;
    }

    private void ApplyLine(Vector3 fromPosition, Vector3 toPosition)
    {
        Vector3 midpoint = (fromPosition + toPosition) * 0.5f + lineOffset;
        Vector2 direction = (Vector2)(toPosition - fromPosition);
        float distance = direction.magnitude;
        float angle = Mathf.Approximately(direction.sqrMagnitude, 0f) ? 0f : Vector2.SignedAngle(Vector2.up, direction);

        lineRoot.SetPositionAndRotation(midpoint, Quaternion.Euler(0f, 0f, angle));
        lineRoot.localScale = new Vector3
        (
            lineActiveScale.x,
            lineActiveScale.y * distance * lineLengthMultiplier,
            lineActiveScale.z
        );
    }

    private void ApplySilhouette(Vector3 fromPosition)
    {
        silhouetteSpriteRenderer.sprite = bossSpriteRenderer.sprite;
        silhouetteSpriteRenderer.flipX = bossSpriteRenderer.flipX;
        silhouetteSpriteRenderer.flipY = bossSpriteRenderer.flipY;
        silhouetteSpriteRenderer.sortingLayerID = bossSpriteRenderer.sortingLayerID;
        silhouetteSpriteRenderer.sortingOrder = bossSpriteRenderer.sortingOrder;

        silhouetteRoot.SetPositionAndRotation(fromPosition + silhouetteOffset, bossSpriteRenderer.transform.rotation);
        silhouetteRoot.localScale = Vector3.Scale(bossSpriteRenderer.transform.lossyScale, silhouetteScaleMultiplier);
    }

    private void RestoreBaseColors()
    {
        for (int i = 0; i < fadeRenderers.Count; i++)
            fadeRenderers[i].color = baseColors[i];
    }

    private void ResetToStandbyImmediate()
    {
        startPointRoot.SetPositionAndRotation(resetTransform.position, resetTransform.rotation);
        endPointRoot.SetPositionAndRotation(resetTransform.position, resetTransform.rotation);
        lineRoot.SetPositionAndRotation(resetTransform.position, resetTransform.rotation);
        silhouetteRoot.SetPositionAndRotation(resetTransform.position, resetTransform.rotation);

        startPointRoot.localScale = Vector3.one;
        endPointRoot.localScale = Vector3.one;
        lineRoot.localScale = Vector3.one;
        silhouetteRoot.localScale = Vector3.one;

        SetAlphaMultiplier(0f);
    }

    private void SetAlphaMultiplier(float multiplier)
    {
        for (int i = 0; i < fadeRenderers.Count; i++)
        {
            Color color = baseColors[i];
            color.a *= multiplier;
            fadeRenderers[i].color = color;
        }
    }
}