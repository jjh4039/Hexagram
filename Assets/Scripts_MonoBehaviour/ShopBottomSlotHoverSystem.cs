using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopBottomSlotHoverSystem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private RectTransform caseRect;
    [SerializeField] private Image caseImage;
    [SerializeField] private RectTransform symbolRect;
    [SerializeField] private Image symbolImage;

    [Header("Tooltip")]
    [SerializeField] private string itemTitle = "응급 수리 키트";
    [SerializeField][TextArea(2, 4)] private string itemDescription = "즉시 체력을 30 회복한다.";
    [SerializeField] private Color tooltipBackgroundColor = new Color(0.14f, 0.22f, 0.18f, 1f);

    [Header("Case")]
    [SerializeField] private float caseHoverScale = 1.02f;
    [SerializeField] private Color normalCaseColor = new Color(0.72f, 0.72f, 0.72f, 1f);
    [SerializeField] private Color hoverCaseColor = new Color(0.82f, 0.88f, 0.88f, 1f);

    [Header("Symbol")]
    [SerializeField] private float symbolHoverScale = 1.05f;
    [SerializeField] private Color normalSymbolColor = new Color(0.65f, 0.65f, 0.65f, 1f);
    [SerializeField] private Color hoverSymbolColor = Color.white;
    [SerializeField] private float symbolFloatOffsetY = 1f;

    [Header("Motion")]
    [SerializeField] private float transitionDuration = 0.1f;

    private bool _isHovering;

    private Vector3 _caseBaseScale;
    private Vector3 _symbolBaseScale;
    private Vector2 _symbolBasePos;

    private void Awake()
    {
        if (caseRect != null)
            _caseBaseScale = caseRect.localScale;

        if (symbolRect != null)
        {
            _symbolBaseScale = symbolRect.localScale;
            _symbolBasePos = symbolRect.anchoredPosition;
        }

        ApplyImmediate(false);
    }

    private void Update()
    {
        float speed = transitionDuration <= 0.0001f ? 999f : 1f / transitionDuration;

        if (caseRect != null)
        {
            Vector3 targetScale = _isHovering ? _caseBaseScale * caseHoverScale : _caseBaseScale;
            caseRect.localScale = Vector3.Lerp(caseRect.localScale, targetScale, Time.unscaledDeltaTime * speed);
        }

        if (caseImage != null)
        {
            Color targetColor = _isHovering ? hoverCaseColor : normalCaseColor;
            caseImage.color = Color.Lerp(caseImage.color, targetColor, Time.unscaledDeltaTime * speed);
        }

        if (symbolRect != null)
        {
            Vector3 targetScale = _isHovering ? _symbolBaseScale * symbolHoverScale : _symbolBaseScale;
            symbolRect.localScale = Vector3.Lerp(symbolRect.localScale, targetScale, Time.unscaledDeltaTime * speed);

            Vector2 targetPos = _isHovering
                ? _symbolBasePos + new Vector2(0f, symbolFloatOffsetY)
                : _symbolBasePos;

            symbolRect.anchoredPosition = Vector2.Lerp(symbolRect.anchoredPosition, targetPos, Time.unscaledDeltaTime * speed);
        }

        if (symbolImage != null)
        {
            Color targetColor = _isHovering ? hoverSymbolColor : normalSymbolColor;
            symbolImage.color = Color.Lerp(symbolImage.color, targetColor, Time.unscaledDeltaTime * speed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;

        if (ShopTooltipUI.Instance != null)
        {
            ShopTooltipUI.Instance.ShowTooltip(
                itemTitle,
                itemDescription,
                tooltipBackgroundColor,
                ShopTooltipUI.TooltipAnchorType.BottomLeft
            );
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;

        if (ShopTooltipUI.Instance != null)
            ShopTooltipUI.Instance.HideTooltip();
    }

    private void OnDisable()
    {
        _isHovering = false;
        ApplyImmediate(false);
    }

    private void ApplyImmediate(bool isHovering)
    {
        if (caseRect != null)
            caseRect.localScale = isHovering ? _caseBaseScale * caseHoverScale : _caseBaseScale;

        if (caseImage != null)
            caseImage.color = isHovering ? hoverCaseColor : normalCaseColor;

        if (symbolRect != null)
        {
            symbolRect.localScale = isHovering ? _symbolBaseScale * symbolHoverScale : _symbolBaseScale;
            symbolRect.anchoredPosition = isHovering
                ? _symbolBasePos + new Vector2(0f, symbolFloatOffsetY)
                : _symbolBasePos;
        }

        if (symbolImage != null)
            symbolImage.color = isHovering ? hoverSymbolColor : normalSymbolColor;
    }
}