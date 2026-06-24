using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ShopHoverSystem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References")] [SerializeField]
    private CanvasGroup slotBackgroundGroup;

    [SerializeField] private RectTransform artifactRect;
    [SerializeField] private Image artifactImage;

    [Header("Purchase Logic")] [SerializeField]
    private TextMeshProUGUI priceText;

    [SerializeField] private GameObject soldOutOverlay;
    [SerializeField] private GameObject priceContainer;

    [Header("Audio")] [SerializeField] private AudioClip sfxHover;

    [Header("Grade Visuals")] [SerializeField]
    private Image slotFrameImage;

    [SerializeField] private Color commonColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color rareColor = new Color(0.6f, 0.87f, 1f, 1f);
    [SerializeField] private Color epicColor = new Color(0.81f, 0.43f, 0.98f, 1f);
    [SerializeField] private Color legendaryColor = new Color(0.6f, 1f, 0.43f, 1f);

    [Header("Slot Alpha")] [SerializeField]
    private float normalSlotAlpha = 0.2f;

    [SerializeField] private float hoverSlotAlpha = 0.5f;

    [Header("Artifact Motion")] [SerializeField]
    private float hoverScale = 1.03f;

    [SerializeField] private float floatOffsetY = 2f;
    [SerializeField] private float transitionDuration = 0.12f;

    [Header("Artifact Color")] [SerializeField]
    private Color normalColor = Color.white;

    [SerializeField] private Color hoverColor = new Color(1.08f, 1.08f, 1.08f, 1f);

    public ArtifactData CurrentArtifact { get; private set; }
    private bool _isHovering;
    private bool _isSoldOut;
    private Vector3 _artifactBaseScale;
    private Vector2 _artifactBaseAnchoredPos;

    private UnityEngine.Events.UnityAction _onPurchaseClick;

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

    public void SetupSlot(ArtifactData data, UnityEngine.Events.UnityAction onPurchaseClick)
    {
        CurrentArtifact = data;
        _isSoldOut = false;
        _onPurchaseClick = onPurchaseClick;

        if (artifactImage != null) artifactImage.sprite = data.icon;
        if (priceText != null) priceText.text = data.basePrice.ToString();

        if (soldOutOverlay != null) soldOutOverlay.SetActive(false);
        if (priceContainer != null) priceContainer.SetActive(true);

        if (slotFrameImage != null) slotFrameImage.color = GetGradeColor(data.grade);
    }

    public void SetSoldOut()
    {
        _isSoldOut = true;
        _isHovering = false;

        if (soldOutOverlay != null) soldOutOverlay.SetActive(true);
        if (priceContainer != null) priceContainer.SetActive(false);
        if (ShopTooltipUI.Instance != null) ShopTooltipUI.Instance.HideTooltip();

        if (artifactImage != null) artifactImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    }

    public void UpdatePriceColor(int currentScrap)
    {
        if (_isSoldOut || CurrentArtifact == null || priceText == null) return;

        if (currentScrap >= CurrentArtifact.basePrice)
            priceText.color = Color.black; // 구매 가능
        else
            priceText.color = Color.red; // 스크랩 부족
    }

    private void Update()
    {
        if (artifactRect == null || artifactImage == null || slotBackgroundGroup == null || _isSoldOut)
            return;

        float speed = transitionDuration <= 0.0001f ? 999f : (1f / transitionDuration);

        float targetAlpha = _isHovering ? hoverSlotAlpha : normalSlotAlpha;
        slotBackgroundGroup.alpha = Mathf.Lerp(slotBackgroundGroup.alpha, targetAlpha, Time.unscaledDeltaTime * speed);

        Vector3 targetScale = _isHovering ? _artifactBaseScale * hoverScale : _artifactBaseScale;
        artifactRect.localScale = Vector3.Lerp(artifactRect.localScale, targetScale, Time.unscaledDeltaTime * speed);

        Vector2 targetPos = _isHovering
            ? _artifactBaseAnchoredPos + new Vector2(0f, floatOffsetY)
            : _artifactBaseAnchoredPos;
        artifactRect.anchoredPosition =
            Vector2.Lerp(artifactRect.anchoredPosition, targetPos, Time.unscaledDeltaTime * speed);

        artifactImage.color = Color.Lerp(artifactImage.color, _isHovering ? hoverColor : normalColor,
            Time.unscaledDeltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isSoldOut || CurrentArtifact == null) return;
        _isHovering = true;

        if (sfxHover != null && SoundManager.instance != null)
        {
            SoundManager.instance.PlaySFX(sfxHover, 0.4f, 0.1f);
        }

        if (ShopTooltipUI.Instance != null)
        {
            Color gradeColor = GetGradeColor(CurrentArtifact.grade);

            string hexColor = ColorUtility.ToHtmlStringRGB(gradeColor);
            string gradeText = $"<color=#{hexColor}>[ {CurrentArtifact.grade} ]</color>\n\n";
            string finalDescription = gradeText + CurrentArtifact.description;

            ShopTooltipUI.Instance.ShowTooltip(
                CurrentArtifact.artifactName,
                finalDescription,
                gradeColor,
                ShopTooltipUI.TooltipAnchorType.TopLeft
            );
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isSoldOut) return;
        _isHovering = false;

        if (ShopTooltipUI.Instance != null)
            ShopTooltipUI.Instance.HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isSoldOut && eventData.button == PointerEventData.InputButton.Left)
        {
            _onPurchaseClick?.Invoke();
        }
    }

    private void OnDisable()
    {
        if (ShopTooltipUI.Instance != null) ShopTooltipUI.Instance.HideTooltip();

        _isHovering = false;
        if (slotBackgroundGroup != null) slotBackgroundGroup.alpha = normalSlotAlpha;
        if (artifactRect != null)
        {
            artifactRect.localScale = _artifactBaseScale;
            artifactRect.anchoredPosition = _artifactBaseAnchoredPos;
        }

        if (artifactImage != null && !_isSoldOut) artifactImage.color = normalColor;
    }

    private Color GetGradeColor(ArtifactGrade grade)
    {
        switch (grade)
        {
            case ArtifactGrade.Common: return commonColor;
            case ArtifactGrade.Rare: return rareColor;
            case ArtifactGrade.Epic: return epicColor;
            case ArtifactGrade.Legendary: return legendaryColor;
            default: return commonColor;
        }
    }
}