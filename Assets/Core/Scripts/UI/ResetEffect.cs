using DG.Tweening;
using TMPro;
using UnityEngine;

public sealed class ResetEffect : MonoBehaviour
{
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private float moveY = 5f;
    [SerializeField] private float duration = 0.5f;

    private RectTransform rectTransform;
    private Vector2 initialAnchoredPosition;
    private Color initialColor;
    private Sequence sequence;

    private void Awake()
    {
        rectTransform = targetText.rectTransform;
        initialAnchoredPosition = rectTransform.anchoredPosition;
        initialColor = targetText.color;
    }

    private void OnDestroy() => sequence?.Kill();

    public void PlayEffect()
    {
        sequence?.Kill();
        ResetState();

        sequence = DOTween.Sequence();

        sequence.Join
        (
            rectTransform.DOAnchorPosY(initialAnchoredPosition.y + moveY, duration)
                .SetEase(Ease.OutQuad)
        );

        sequence.Join
        (
            targetText.DOFade(0f, duration)
                .SetEase(Ease.InQuad)
        );

        sequence.OnComplete(() =>
        {
            ResetState();
            sequence = null;
        });
    }

    private void ResetState()
    {
        rectTransform.anchoredPosition = initialAnchoredPosition;
        targetText.color = initialColor;
    }
}