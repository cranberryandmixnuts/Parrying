using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public sealed class CustomSlider : MonoBehaviour, IPointerDownHandler, IDragHandler, IInitializePotentialDragHandler, IPointerUpHandler
{
    [System.Serializable]
    public sealed class FloatEvent : UnityEvent<float> { }

    [Header("References")]
    [SerializeField, Required] private RectTransform trackRect;
    [SerializeField, Required] private Image fillImage;
    [SerializeField, Required] private RectTransform handleRect;

    [Header("Config")]
    [SerializeField] private Vector2 valueRange = new(0.0f, 1.0f);
    [SerializeField] private Vector2 handleXRange = new(-100.0f, 100.0f);

    [MinMaxSlider(0.0f, 1.0f, true)]
    [SerializeField] private Vector2 fillAmountRange = new(0.0f, 1.0f);

    [SerializeField] private bool wholeNumbers = false;
    [SerializeField] private bool interactable = true;

    [Header("State")]
    [PropertyRange("@valueRange.x", "@valueRange.y")]
    [SerializeField] private float value = 0.0f;

    [Header("Events")]
    [SerializeField] private FloatEvent onValueChanged = new();

    public Vector2 ValueRange
    {
        get => valueRange;
        set
        {
            valueRange = value;
            ValidateRanges();
            SetValue(this.value);
        }
    }

    public Vector2 HandleXRange
    {
        get => handleXRange;
        set
        {
            handleXRange = value;
            ValidateRanges();
            UpdateVisuals();
        }
    }

    public Vector2 FillAmountRange
    {
        get => fillAmountRange;
        set
        {
            fillAmountRange = value;
            ValidateRanges();
            UpdateVisuals();
        }
    }

    public bool WholeNumbers
    {
        get => wholeNumbers;
        set
        {
            wholeNumbers = value;
            SetValue(this.value);
        }
    }

    public bool Interactable
    {
        get => interactable;
        set { interactable = value; }
    }

    public float Value
    {
        get => value;
        set { SetValue(value); }
    }

    public FloatEvent OnValueChanged => onValueChanged;

    public void Awake()
    {
        ValidateRanges();
        value = ClampValue(value);
        UpdateVisuals();
    }

    public void OnValidate()
    {
        ValidateRanges();
        value = ClampValue(value);
        UpdateVisuals();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (interactable == false)
            return;

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        UpdateFromPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (interactable == false)
            return;

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        UpdateFromPointer(eventData);
    }

    public void OnPointerUp(PointerEventData eventData) { }

    public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;

    [ContextMenu("Apply Fill Image Settings")]
    public void ApplyFillImageSettings()
    {
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillClockwise = true;
        fillImage.fillAmount = GetFillAmount();
    }

    private void ValidateRanges()
    {
        if (valueRange.y < valueRange.x)
        {
            (valueRange.y, valueRange.x) = (valueRange.x, valueRange.y);
        }

        if (handleXRange.y < handleXRange.x)
        {
            (handleXRange.y, handleXRange.x) = (handleXRange.x, handleXRange.y);
        }

        fillAmountRange.x = Mathf.Clamp01(fillAmountRange.x);
        fillAmountRange.y = Mathf.Clamp01(fillAmountRange.y);

        if (fillAmountRange.y < fillAmountRange.x)
        {
            (fillAmountRange.y, fillAmountRange.x) = (fillAmountRange.x, fillAmountRange.y);
        }
    }

    private float ClampValue(float v)
    {
        float clamped = Mathf.Clamp(v, valueRange.x, valueRange.y);

        if (wholeNumbers)
            clamped = Mathf.Round(clamped);

        return clamped;
    }

    private float GetNormalized()
    {
        if (Mathf.Approximately(valueRange.x, valueRange.y))
            return 0.0f;

        float t = Mathf.InverseLerp(valueRange.x, valueRange.y, value);
        return Mathf.Clamp01(t);
    }

    private float GetFillAmount()
    {
        float t = GetNormalized();
        return Mathf.Lerp(fillAmountRange.x, fillAmountRange.y, t);
    }

    private void SetValue(float newValue)
    {
        float clamped = ClampValue(newValue);

        if (Mathf.Approximately(value, clamped))
            return;

        value = clamped;
        UpdateVisuals();
    }

    private void UpdateFromPointer(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(trackRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

        float normalized = Mathf.InverseLerp(handleXRange.x, handleXRange.y, localPoint.x);
        normalized = Mathf.Clamp01(normalized);

        float v = Mathf.Lerp(valueRange.x, valueRange.y, normalized);
        SetValue(v);
    }

    private void UpdateVisuals()
    {
        float t = GetNormalized();

        fillImage.fillAmount = Mathf.Lerp(fillAmountRange.x, fillAmountRange.y, t);

        Vector2 pos = handleRect.anchoredPosition;
        pos.x = Mathf.Lerp(handleXRange.x, handleXRange.y, t);

        handleRect.anchoredPosition = pos;
    }
}
