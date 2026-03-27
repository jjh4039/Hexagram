using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopStatOptionHoverSystem : MonoBehaviour
{
    [Header("Main References")]
    [SerializeField] private RectTransform caseRect;
    [SerializeField] private Image caseImage;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Reroll References")]
    [SerializeField] private RectTransform rerollRect;
    [SerializeField] private Image rerollImage;

    [Header("Main Motion")]
    [SerializeField] private float mainHoverScale = 1.015f;
    [SerializeField] private float transitionDuration = 0.05f;

    [Header("Main Color")]
    [SerializeField] private Color normalCaseColor = new Color(0.62f, 0.62f, 0.62f, 1f);
    [SerializeField] private Color hoverCaseColor = new Color(0.6f, 0.93f, 1f, 1f);
    [SerializeField] private Color normalTextColor = new Color(0.92f, 0.92f, 0.92f, 1f);
    [SerializeField] private Color hoverTextColor = Color.white;

    [Header("Reroll Motion")]
    [SerializeField] private float rerollHoverScale = 1.05f;

    [Header("Reroll Color")]
    [SerializeField] private Color normalRerollColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color hoverRerollColor = Color.white;

    private bool _isMainHovering;
    private bool _isRerollHovering;

    private Vector3 _caseBaseScale;
    private Vector3 _rerollBaseScale;

    public void SetHover(ShopHoverAreaRelay.HoverAreaType areaType, bool isHovering)
    {
        switch (areaType)
        {
            case ShopHoverAreaRelay.HoverAreaType.Main:
                _isMainHovering = isHovering;
                break;

            case ShopHoverAreaRelay.HoverAreaType.Reroll:
                _isRerollHovering = isHovering;
                break;
        }
    }

    private void Awake()
    {
        if (caseRect != null)
            _caseBaseScale = caseRect.localScale;

        if (rerollRect != null)
            _rerollBaseScale = rerollRect.localScale;

        ApplyImmediateVisual();
    }

    private void Update()
    {
        float speed = transitionDuration <= 0.0001f ? 999f : 1f / transitionDuration;

        UpdateMainVisual(speed);
        UpdateRerollVisual(speed);
    }

    private void UpdateMainVisual(float speed)
    {
        if (caseRect != null)
        {
            Vector3 targetScale = _isMainHovering ? _caseBaseScale * mainHoverScale : _caseBaseScale;
            caseRect.localScale = Vector3.Lerp(caseRect.localScale, targetScale, Time.unscaledDeltaTime * speed);
        }

        if (caseImage != null)
        {
            Color targetColor = _isMainHovering ? hoverCaseColor : normalCaseColor;
            caseImage.color = Color.Lerp(caseImage.color, targetColor, Time.unscaledDeltaTime * speed);
        }

        if (descriptionText != null)
        {
            Color targetColor = _isMainHovering ? hoverTextColor : normalTextColor;
            descriptionText.color = Color.Lerp(descriptionText.color, targetColor, Time.unscaledDeltaTime * speed);
        }
    }

    private void UpdateRerollVisual(float speed)
    {
        if (rerollRect != null)
        {
            Vector3 targetScale = _isRerollHovering ? _rerollBaseScale * rerollHoverScale : _rerollBaseScale;
            rerollRect.localScale = Vector3.Lerp(rerollRect.localScale, targetScale, Time.unscaledDeltaTime * speed);
        }

        if (rerollImage != null)
        {
            Color targetColor = _isRerollHovering ? hoverRerollColor : normalRerollColor;
            rerollImage.color = Color.Lerp(rerollImage.color, targetColor, Time.unscaledDeltaTime * speed);
        }
    }

    private void OnDisable()
    {
        _isMainHovering = false;
        _isRerollHovering = false;
        ApplyImmediateVisual();
    }

    private void ApplyImmediateVisual()
    {
        if (caseRect != null)
            caseRect.localScale = _caseBaseScale;

        if (caseImage != null)
            caseImage.color = normalCaseColor;

        if (descriptionText != null)
            descriptionText.color = normalTextColor;

        if (rerollRect != null)
            rerollRect.localScale = _rerollBaseScale;

        if (rerollImage != null)
            rerollImage.color = normalRerollColor;
    }
}