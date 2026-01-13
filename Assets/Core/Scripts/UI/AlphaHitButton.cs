using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class AlphaHitButton : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)]
    private float threshold = 0.1f;

    [SerializeField, Required]
    private Image img;

    private void Awake()
    {
        img.alphaHitTestMinimumThreshold = threshold;
        if (img.sprite == null) img.raycastTarget = false;
    }
}
