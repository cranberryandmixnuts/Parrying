using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class KeyBindingVisual : MonoBehaviour
{
    [BoxGroup("DB"), SerializeField, Required]
    private KeyIconDatabase iconDatabase;

    [BoxGroup("Refs"), SerializeField, Required]
    private Image iconImage;

    [BoxGroup("Refs"), SerializeField, Required]
    private Image frameImage;

    [BoxGroup("Refs"), SerializeField, Required]
    private TMP_Text text;

    [BoxGroup("Frame Sizing"), SerializeField, Min(0f)]
    private float horizontalPadding = 24f;

    [BoxGroup("Frame Sizing"), SerializeField, Min(0f)]
    private float minWidth = 72f;

    public void Apply(InputAction action, int bindingIndex)
    {
        InputBinding binding = action.bindings[bindingIndex];
        string path = binding.effectivePath;

        if (!string.IsNullOrEmpty(path) && iconDatabase.TryGet(path, out Sprite icon))
        {
            SetIcon(icon);
            return;
        }

        SetText(action.GetBindingDisplayString(bindingIndex));
    }

    private void SetIcon(Sprite icon)
    {
        iconImage.sprite = icon;

        if (!iconImage.gameObject.activeSelf) iconImage.gameObject.SetActive(true);
        if (frameImage.gameObject.activeSelf) frameImage.gameObject.SetActive(false);
        if (text.gameObject.activeSelf) text.gameObject.SetActive(false);
    }

    private void SetText(string value)
    {
        if (iconImage.gameObject.activeSelf) iconImage.gameObject.SetActive(false);
        if (!frameImage.gameObject.activeSelf) frameImage.gameObject.SetActive(true);
        if (!text.gameObject.activeSelf) text.gameObject.SetActive(true);

        text.text = value;
        ResizeFrameToText();
    }

    private void ResizeFrameToText()
    {
        text.ForceMeshUpdate();

        float w = text.preferredWidth + (horizontalPadding * 2f);
        if (w < minWidth) w = minWidth;

        RectTransform rt = frameImage.rectTransform;
        Vector2 size = rt.sizeDelta;
        size.x = w;
        rt.sizeDelta = size;
    }
}