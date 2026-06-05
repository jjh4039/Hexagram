using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopBottomSlotHoverSystem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public enum BottomItemType { HealKit, WeightKit }
    public enum ItemGrade { Low, Medium, High }

    [Header("Slot Function Settings")]
    [SerializeField] private BottomItemType itemType;

    [Header("References")]
    [SerializeField] private RectTransform caseRect;
    [SerializeField] private Image caseImage;
    [SerializeField] private RectTransform symbolRect;
    [SerializeField] private Image symbolImage;
    [SerializeField] private GameObject soldOutOverlay; 
    [SerializeField] private GameObject priceContainer; 
    [SerializeField] private TextMeshProUGUI priceText; 

    [Header("Tooltip")]
    [SerializeField] private string itemTitle = "아이템 이름";
    [SerializeField][TextArea(2, 4)] private string itemDescription = "아이템 설명";
    [SerializeField] private Color tooltipBackgroundColor = new Color(0.14f, 0.22f, 0.18f, 1f);

    [Header("Weight Kit Settings")]
    [SerializeField] private GameObject balancePrefab; 

    [Header("Heal Kit Prices")]
    [SerializeField] private int healLowPrice = 100;
    [SerializeField] private int healMediumPrice = 100; 
    [SerializeField] private int healHighPrice = 100;

    [Header("Weight Kit Prices")]
    [SerializeField] private int weightLowPrice = 100;
    [SerializeField] private int weightMediumPrice = 200;
    [SerializeField] private int weightHighPrice = 300;

    [Header("Case Color Motion")]
    [SerializeField] private float caseHoverScale = 1.02f;
    [SerializeField] private Color normalCaseColor = new Color(0.72f, 0.72f, 0.72f, 1f);
    [SerializeField] private Color hoverCaseColor = new Color(0.82f, 0.88f, 0.88f, 1f);
    [SerializeField] private Color soldOutCaseColor = new Color(0.35f, 0.35f, 0.35f, 1f); 

    [Header("Symbol Color Motion")]
    [SerializeField] private float symbolHoverScale = 1.05f;
    [SerializeField] private Color normalSymbolColor = new Color(0.65f, 0.65f, 0.65f, 1f);
    [SerializeField] private Color hoverSymbolColor = Color.white;
    [SerializeField] private Color usedSymbolColor = new Color(0.3f, 0.3f, 0.3f, 0.6f); 
    [SerializeField] private float symbolFloatOffsetY = 1f;

    [Header("Motion")]
    [SerializeField] private float transitionDuration = 0.1f;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxHover;
    [SerializeField] private AudioClip sfxPurchase;

    private bool _isHovering;
    private bool _isSoldOut;
    private Vector3 _caseBaseScale;
    private Vector3 _symbolBaseScale;
    private Vector2 _symbolBasePos;

    private int _currentPrice;
    private ItemGrade _currentGrade;
    private string _dynamicDescription; 
    private Action _onScrapSpent;
    
    private Vector3 _spawnPosition; 
    private Transform _robotTransform; 

    private static int staticHealGradeIndex = 2; 
    private static bool staticHealBoughtLastTime = false;

    private void Awake()
    {
        if (caseRect != null) _caseBaseScale = caseRect.localScale;
        if (symbolRect != null)
        {
            _symbolBaseScale = symbolRect.localScale;
            _symbolBasePos = symbolRect.anchoredPosition;
        }

        if (soldOutOverlay != null) soldOutOverlay.SetActive(false);
        ApplyImmediate(false);
    }

    public void SetupBottomSlot(Action onScrapSpentCallback, Transform robotTransform, bool isNewShop, bool isReroll)
    {
        _onScrapSpent = onScrapSpentCallback;
        _robotTransform = robotTransform; 
        
        _spawnPosition = (robotTransform != null ? robotTransform.position : Vector3.zero) + new Vector3(0f, -3f, 0f);

        _isSoldOut = false;
        _isHovering = false;

        if (soldOutOverlay != null) soldOutOverlay.SetActive(false);
        if (priceContainer != null) priceContainer.SetActive(true);

        int weightValue = 0;

        if (itemType == BottomItemType.HealKit)
        {
            if (isNewShop)
            {
                if (staticHealBoughtLastTime) 
                    staticHealGradeIndex = Mathf.Max(staticHealGradeIndex - 1, 0); 
                else 
                    staticHealGradeIndex = 2; 

                staticHealBoughtLastTime = false; 
            }
            _currentGrade = (ItemGrade)staticHealGradeIndex;
            _currentPrice = _currentGrade == ItemGrade.Low ? healLowPrice : (_currentGrade == ItemGrade.Medium ? healMediumPrice : healHighPrice);
        }
        else 
        {
            if (isNewShop || isReroll)
            {
                _currentGrade = (ItemGrade)UnityEngine.Random.Range(0, 3);
            }
            _currentPrice = _currentGrade == ItemGrade.Low ? weightLowPrice : (_currentGrade == ItemGrade.Medium ? weightMediumPrice : weightHighPrice);
            weightValue = _currentGrade == ItemGrade.Low ? 2 : (_currentGrade == ItemGrade.Medium ? 4 : 6);
        }

        string gradeStr = _currentGrade == ItemGrade.Low ? "하급" : (_currentGrade == ItemGrade.Medium ? "중급" : "상급");
        string cleanDesc = itemDescription.Replace("[하급]", "").Replace("[중급]", "").Replace("[상급]", "").Trim();
        
        if (itemType == BottomItemType.WeightKit)
        {
            _dynamicDescription = $"[{gradeStr} - {weightValue}%]\n\n{cleanDesc}";
        }
        else
        {
            _dynamicDescription = $"[{gradeStr}]\n\n{cleanDesc}";
        }

        if (priceText != null) priceText.text = _currentPrice.ToString();
        ApplyImmediate(false);
    }

    public void UpdatePriceColor(int currentScrap)
    {
        if (_isSoldOut || priceText == null) return;
        priceText.color = currentScrap >= _currentPrice ? Color.black : Color.red;
    }

    private void Update()
    {
        float speed = transitionDuration <= 0.0001f ? 999f : 1f / transitionDuration;

        if (caseRect != null)
        {
            Vector3 targetScale = (_isHovering && !_isSoldOut) ? _caseBaseScale * caseHoverScale : _caseBaseScale;
            caseRect.localScale = Vector3.Lerp(caseRect.localScale, targetScale, Time.unscaledDeltaTime * speed);
        }

        if (caseImage != null)
        {
            Color targetColor = _isSoldOut ? soldOutCaseColor : (_isHovering ? hoverCaseColor : normalCaseColor);
            caseImage.color = Color.Lerp(caseImage.color, targetColor, Time.unscaledDeltaTime * speed);
        }

        if (symbolRect != null)
        {
            Vector3 targetScale = (_isHovering && !_isSoldOut) ? _symbolBaseScale * symbolHoverScale : _symbolBaseScale;
            symbolRect.localScale = Vector3.Lerp(symbolRect.localScale, targetScale, Time.unscaledDeltaTime * speed);

            Vector2 targetPos = (_isHovering && !_isSoldOut) ? _symbolBasePos + new Vector2(0f, symbolFloatOffsetY) : _symbolBasePos;
            symbolRect.anchoredPosition = Vector2.Lerp(symbolRect.anchoredPosition, targetPos, Time.unscaledDeltaTime * speed);
        }

        if (symbolImage != null)
        {
            Color targetColor = _isSoldOut ? usedSymbolColor : (_isHovering ? hoverSymbolColor : normalSymbolColor);
            symbolImage.color = Color.Lerp(symbolImage.color, targetColor, Time.unscaledDeltaTime * speed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isSoldOut) return;
        _isHovering = true;

        if (sfxHover != null && SoundManager.instance != null)
            SoundManager.instance.PlaySFX(sfxHover, 0.4f, 0.1f);

        if (ShopTooltipUI.Instance != null)
        {
            ShopTooltipUI.Instance.ShowTooltip(itemTitle, _dynamicDescription, tooltipBackgroundColor, ShopTooltipUI.TooltipAnchorType.BottomLeft);
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
        if (_isSoldOut || GameManager.instance == null || eventData.button != PointerEventData.InputButton.Left) return;

        PlayerStats stats = GameManager.instance.stats;
        if (itemType == BottomItemType.HealKit && stats != null && stats.cannotHeal)
        {
            if (PlayerFeedbackUI.Instance != null)
                PlayerFeedbackUI.Instance.ShowWarning(7); 
            return;
        }

        int currentScrap = GameManager.instance.currentScrap;

        if (currentScrap >= _currentPrice)
        {
            GameManager.instance.currentScrap -= _currentPrice;

            ExecuteItemEffect();

            if (sfxPurchase != null && SoundManager.instance != null)
                SoundManager.instance.PlaySFX(sfxPurchase, 0.65f, 0.1f);

            if (AnalyticsManager.Instance != null)
            {
                string itemName = itemType.ToString() + "_" + _currentGrade.ToString();
                AnalyticsManager.Instance.LogShopPurchase("Consumable", itemName, _currentPrice); // 소모품 구매 로그 전송
            }

            SetSoldOut();
            _onScrapSpent?.Invoke(); 
        }
        else
        {
            if (PlayerFeedbackUI.Instance != null)
                PlayerFeedbackUI.Instance.ShowWarning(6); 
        }
    }

    private void ExecuteItemEffect()
    {
        PlayerStats stats = GameManager.instance.stats;

        if (itemType == BottomItemType.HealKit)
        {
            staticHealBoughtLastTime = true; 
            int healAmount = Mathf.RoundToInt(stats.maxHealth * 0.33f);
            stats.Heal(healAmount); 
        }
        else 
        {
            if (balancePrefab != null)
            {
                // ★ 수정: 일단 월드 기준으로 먼저 생성하여 프리팹 고유 스케일을 유지합니다.
                GameObject balanceObj = Instantiate(balancePrefab, _spawnPosition, Quaternion.identity);
                
                // ★ 수정: 부모를 설정하되, worldPositionStays 인자를 true로 주어 부모 스케일에 맞춰 자동 역산되도록 합니다.
                if (_robotTransform != null)
                {
                    balanceObj.transform.SetParent(_robotTransform, true);
                }

                Balance balanceScript = balanceObj.GetComponent<Balance>();
                
                if (balanceScript != null)
                {
                    float weightValue = _currentGrade == ItemGrade.Low ? 2f : (_currentGrade == ItemGrade.Medium ? 4f : 6f);
                    balanceScript.Setup(weightValue);
                }
            }
        }
    }

    private void SetSoldOut()
    {
        _isSoldOut = true;
        _isHovering = false;

        if (soldOutOverlay != null) soldOutOverlay.SetActive(true);
        if (priceContainer != null) priceContainer.SetActive(false);
        if (ShopTooltipUI.Instance != null) ShopTooltipUI.Instance.HideTooltip();

        ApplyImmediate(false);
    }

    public static void ResetHealKitState()
    {
        staticHealGradeIndex = 2; 
        staticHealBoughtLastTime = false; 
    }

    private void OnDisable()
    {
        _isHovering = false;
        ApplyImmediate(false);
    }

    private void ApplyImmediate(bool isHovering)
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;

        if (caseRect != null) caseRect.localScale = isHovering ? _caseBaseScale * caseHoverScale : _caseBaseScale;
        if (caseImage != null) caseImage.color = _isSoldOut ? soldOutCaseColor : (isHovering ? hoverCaseColor : normalCaseColor);

        if (symbolRect != null)
        {
            symbolRect.localScale = isHovering ? _symbolBaseScale * symbolHoverScale : _symbolBaseScale;
            symbolRect.anchoredPosition = isHovering ? _symbolBasePos + new Vector2(0f, symbolFloatOffsetY) : _symbolBasePos;
        }

        if (symbolImage != null) symbolImage.color = _isSoldOut ? usedSymbolColor : (isHovering ? hoverSymbolColor : normalSymbolColor);
    }
}