using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopHoverSystem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private CanvasGroup slotBackgroundGroup;
    [SerializeField] private RectTransform artifactRect;
    [SerializeField] private Image artifactImage;

    [Header("Preview Text")]
    [SerializeField] private string itemTitle = "트럼프 카드";
    [SerializeField][TextArea(2, 4)] private string itemDescription = "연속으로 같은 면이 나왔을 때\n공격력이 10% 상승한다.";

    [Header("Tooltip Color")]
    [SerializeField] private Color tooltipBackgroundColor = new Color(0.23f, 0.07f, 0.28f, 1f);

    [Header("Slot Alpha")]
    [SerializeField] private float normalSlotAlpha = 0.2f;
    [SerializeField] private float hoverSlotAlpha = 0.5f;

    [Header("Artifact Motion")]
    [SerializeField] private float hoverScale = 1.03f;
    [SerializeField] private float floatOffsetY = 2f;
    [SerializeField] private float transitionDuration = 0.12f;

    [Header("Artifact Color")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1.08f, 1.08f, 1.08f, 1f);

    private bool _isHovering;
    private Vector3 _artifactBaseScale;
    private Vector2 _artifactBaseAnchoredPos;

    private void Awake()
    {
        if (artifactRect != null)
        {
            _artifactBaseScale = artifactRect.localScale;
            _artifactBaseAnchoredPos = artifactRect.anchoredPosition;
        }

        if (slotBackgroundGroup != null)
            slotBackgroundGroup.alpha = normalSlotAlpha;

        if (artifactImage != null)
            artifactImage.color = normalColor;
    }

    private void Update()
    {
        if (artifactRect == null || artifactImage == null || slotBackgroundGroup == null)
            return;

        float speed = transitionDuration <= 0.0001f ? 999f : (1f / transitionDuration);

        float targetAlpha = _isHovering ? hoverSlotAlpha : normalSlotAlpha;
        slotBackgroundGroup.alpha = Mathf.Lerp(slotBackgroundGroup.alpha, targetAlpha, Time.unscaledDeltaTime * speed);

        Vector3 targetScale = _isHovering
            ? _artifactBaseScale * hoverScale
            : _artifactBaseScale;

        artifactRect.localScale = Vector3.Lerp(
            artifactRect.localScale,
            targetScale,
            Time.unscaledDeltaTime * speed
        );

        Vector2 targetPos = _isHovering
            ? _artifactBaseAnchoredPos + new Vector2(0f, floatOffsetY)
            : _artifactBaseAnchoredPos;

        artifactRect.anchoredPosition = Vector2.Lerp(
            artifactRect.anchoredPosition,
            targetPos,
            Time.unscaledDeltaTime * speed
        );

        artifactImage.color = Color.Lerp(
            artifactImage.color,
            _isHovering ? hoverColor : normalColor,
            Time.unscaledDeltaTime * speed
        );
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
                ShopTooltipUI.TooltipAnchorType.TopLeft
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

        if (slotBackgroundGroup != null)
            slotBackgroundGroup.alpha = normalSlotAlpha;

        if (artifactRect != null)
        {
            artifactRect.localScale = _artifactBaseScale;
            artifactRect.anchoredPosition = _artifactBaseAnchoredPos;
        }

        if (artifactImage != null)
            artifactImage.color = normalColor;
    }
}