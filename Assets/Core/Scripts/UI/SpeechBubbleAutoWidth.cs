using TMPro;
using UnityEngine;

public sealed class SpeechBubbleAutoWidth : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text text;
    [SerializeField] private RectTransform bubbleRect;

    [Header("Sizing")]
    [SerializeField] private float paddingLeft = 24f;
    [SerializeField] private float paddingRight = 24f;
    [SerializeField] private float minWidth = 80f;
    [SerializeField] private float maxWidth = 560f;

    private void OnEnable() => Refresh();

    private void LateUpdate() => Refresh();

    private void Refresh()
    {
        Vector2 preferred = text.GetPreferredValues(text.text, float.PositiveInfinity, float.PositiveInfinity);

        float targetBubbleWidth = preferred.x + paddingLeft + paddingRight;
        targetBubbleWidth = Mathf.Clamp(targetBubbleWidth, minWidth, maxWidth);

        bubbleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetBubbleWidth);

        float textAreaWidth = Mathf.Max(0f, targetBubbleWidth - paddingLeft - paddingRight);
        text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textAreaWidth);
    }
}