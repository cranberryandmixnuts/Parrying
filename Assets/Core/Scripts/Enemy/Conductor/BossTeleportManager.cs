using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using UnityEngine;

public sealed class BossTeleportManager : MonoBehaviour
{
    [Header("Boss")]
    [SerializeField, Required] private Transform bossTransform;
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
    [SerializeField, Range(0f, 1f)] private float prePointTargetAlpha = 0.1f;
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
    [SerializeField] private float lineLengthAdd = 0f;

    [Header("Silhouette")]
    [SerializeField] private Vector3 silhouetteOffset = Vector3.zero;
    [SerializeField] private Vector3 silhouetteScaleAdd = Vector3.zero;

    private Color startPointBaseColor;
    private Color endPointBaseColor;
    private Color lineBaseColor;
    private Color silhouetteBaseColor;

    private Sequence activePreSequence;
    private Sequence activePostSequence;
    private Material bossMaterial;
    private Color bossMaterialColorTemplate;

    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        startPointBaseColor = startPointRenderer.color;
        endPointBaseColor = endPointRenderer.color;
        lineBaseColor = lineRenderer.color;
        silhouetteBaseColor = silhouetteSpriteRenderer.color;

        bossMaterial = bossSpriteRenderer.material;
        bossMaterialColorTemplate = bossMaterial.GetColor(ColorId);

        ResetToStandbyImmediate();
    }

    private void OnDisable() => ForceReset();

    public void TeleportImmediate(Vector3 toPosition)
    {
        KillSequences();
        ResetToStandbyImmediate();

        bossTransform.position = toPosition;
        Debug.Log("Boss teleported immediately to: " + toPosition);
    }

    public IEnumerator PlayTeleportSequence(Vector3 toPosition, Action onTeleported = null)
    {
        KillSequences();
        ResetToStandbyImmediate();

        Vector3 fromPosition = bossTransform.position;

        ApplyPoints(fromPosition, toPosition);
        SetPrePointAlpha(0f);
        SetBossSilhouetteAlpha(0f);

        AudioManager.Instance.PlayOneShotSFX("텔레포트 충전", gameObject);

        activePreSequence = DOTween.Sequence().SetLink(gameObject);
        activePreSequence.Join(DOVirtual.Float(0f, prePointTargetAlpha, preEffectDuration, SetPrePointAlpha).SetEase(Ease.Linear));
        activePreSequence.Join(DOVirtual.Float(0f, bossSilhouetteTargetAlpha, preEffectDuration, SetBossSilhouetteAlpha).SetEase(Ease.Linear));

        yield return activePreSequence.WaitForCompletion();

        activePreSequence = null;

        AudioManager.Instance.PlayOneShotSFX("텔레포트", gameObject);

        bossTransform.position = toPosition;
        SetBossSilhouetteAlpha(0f);
        onTeleported?.Invoke();
        Debug.Log("Boss teleported with a effect to: " + toPosition);

        PlayPostEffect(fromPosition, toPosition);
    }

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

    private void PlayPostEffect(Vector3 fromPosition, Vector3 toPosition)
    {
        ApplyLine(fromPosition, toPosition);
        ApplySilhouette(fromPosition);
        RestoreBaseColors();

        activePostSequence = DOTween.Sequence().SetLink(gameObject);

        activePostSequence.Join(startPointRenderer.DOFade(0f, fadeDuration).SetEase(Ease.Linear));
        activePostSequence.Join(endPointRenderer.DOFade(0f, fadeDuration).SetEase(Ease.Linear));
        activePostSequence.Join(lineRenderer.DOFade(0f, fadeDuration).SetEase(Ease.Linear));
        activePostSequence.Join(silhouetteSpriteRenderer.DOFade(0f, fadeDuration).SetEase(Ease.Linear));

        activePostSequence.OnComplete(ResetToStandbyImmediate);
    }

    private void ApplyPoints(Vector3 fromPosition, Vector3 toPosition)
    {
        startPointRoot.SetPositionAndRotation(fromPosition + startPointOffset, Quaternion.identity);
        startPointRoot.localScale = startPointActiveScale;

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
            lineActiveScale.y * distance + lineLengthAdd,
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
        silhouetteRoot.localScale = bossSpriteRenderer.transform.lossyScale + silhouetteScaleAdd;
    }

    private void RestoreBaseColors()
    {
        startPointRenderer.color = startPointBaseColor;
        endPointRenderer.color = endPointBaseColor;
        lineRenderer.color = lineBaseColor;
        silhouetteSpriteRenderer.color = silhouetteBaseColor;
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

        RestoreBaseColors();
        SetBossSilhouetteAlpha(0f);
    }

    private void SetPrePointAlpha(float alpha)
    {
        SetRendererAlpha(startPointRenderer, startPointBaseColor, alpha);
        SetRendererAlpha(endPointRenderer, endPointBaseColor, alpha);
    }

    private static void SetRendererAlpha(SpriteRenderer renderer, Color baseColor, float alpha)
    {
        baseColor.a = Mathf.Clamp01(alpha);
        renderer.color = baseColor;
    }

    private void SetBossSilhouetteAlpha(float alpha)
    {
        Color color = bossMaterialColorTemplate;
        color.a = Mathf.Clamp01(alpha);
        bossMaterial.SetColor(ColorId, color);
    }
}