using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class BossTeleportManager : MonoBehaviour
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

    [Header("Pre Teleport")]
    [SerializeField, MinValue(0f)] private float preEffectDuration = 0.5f;
    [SerializeField, Range(0f, 1f)] private float prePointAlphaMultiplier = 0.1f;
    [SerializeField, Range(0f, 1f)] private float bossSilhouetteTargetAlpha = 1f;

    [Header("Post Teleport Fade")]
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
    private Sequence activePreSequence;
    private Sequence activePostSequence;
    private Material bossMaterial;
    private Color bossBaseMaterialColor;

    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        CacheFadeRenderers();
        CacheBossMaterial();
        ResetToStandbyImmediate();
    }

    private void OnDisable() => ForceReset();

    public IEnumerator PlayTeleportSequence(Transform bossTransform, Vector3 toPosition, Action onTeleported = null)
    {
        KillSequences();
        ResetToStandbyImmediate();

        Vector3 fromPosition = bossTransform.position;

        ApplyStartPoint(fromPosition);
        ApplyEndPoint(toPosition);
        SetPrePointAlphaMultiplier(0f);

        activePreSequence = DOTween.Sequence().SetLink(gameObject);
        activePreSequence.Join(DOVirtual.Float(0f, prePointAlphaMultiplier, preEffectDuration, SetPrePointAlphaMultiplier).SetEase(Ease.Linear));
        activePreSequence.Join(DOVirtual.Float(GetBossSilhouetteAlpha(), bossSilhouetteTargetAlpha, preEffectDuration, SetBossSilhouetteAlpha).SetEase(Ease.Linear));

        yield return activePreSequence.WaitForCompletion();

        activePreSequence = null;

        bossTransform.position = toPosition;
        RestoreBossBaseMaterialColor();
        onTeleported?.Invoke();

        PlayPostEffect(fromPosition, toPosition);
    }

    [Button]
    public void ForceReset()
    {
        KillSequences();
        ResetToStandbyImmediate();
    }

    private void KillSequences()
    {
        if (activePreSequence != null && activePreSequence.IsActive())
            activePreSequence.Kill();

        if (activePostSequence != null && activePostSequence.IsActive())
            activePostSequence.Kill();

        activePreSequence = null;
        activePostSequence = null;
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

    private void CacheBossMaterial()
    {
        bossMaterial = bossSpriteRenderer.material;
        bossBaseMaterialColor = bossMaterial.GetColor(ColorId);
    }

    private void RegisterRenderer(SpriteRenderer renderer)
    {
        fadeRenderers.Add(renderer);
        baseColors.Add(renderer.color);
    }

    private void PlayPostEffect(Vector3 fromPosition, Vector3 toPosition)
    {
        ApplyStartPoint(fromPosition);
        ApplyEndPoint(toPosition);
        ApplyLine(fromPosition, toPosition);
        ApplySilhouette(fromPosition);
        RestoreBaseColors();

        activePostSequence = DOTween.Sequence().SetLink(gameObject);

        for (int i = 0; i < fadeRenderers.Count; i++)
            activePostSequence.Join(fadeRenderers[i].DOFade(0f, fadeDuration).SetEase(Ease.Linear));

        activePostSequence.OnComplete(ResetToStandbyImmediate);
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
        RestoreBossBaseMaterialColor();
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

    private void SetPrePointAlphaMultiplier(float multiplier)
    {
        SetRendererAlphaMultiplier(startPointRenderer, multiplier);
        SetRendererAlphaMultiplier(endPointRenderer, multiplier);
    }

    private void SetRendererAlphaMultiplier(SpriteRenderer renderer, float multiplier)
    {
        int index = fadeRenderers.IndexOf(renderer);
        Color color = baseColors[index];
        color.a *= multiplier;
        renderer.color = color;
    }

    private float GetBossSilhouetteAlpha() => bossMaterial.GetColor(ColorId).a;

    private void SetBossSilhouetteAlpha(float alpha)
    {
        Color color = bossMaterial.GetColor(ColorId);
        color.a = Mathf.Clamp01(alpha);
        bossMaterial.SetColor(ColorId, color);
    }

    private void RestoreBossBaseMaterialColor() => bossMaterial.SetColor(ColorId, bossBaseMaterialColor);
}