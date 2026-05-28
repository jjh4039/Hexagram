using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopStatOptionHoverSystem : MonoBehaviour
{
    public enum ShopStatType { AttackPower, AttackSpeed, MoveSpeed, CritChance, CritDamage }

    [Header("Main References")]
    [SerializeField] private RectTransform caseRect;
    [SerializeField] private Image caseImage;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private GameObject soldOutOverlay; 

    [Header("Price References")]
    [SerializeField] private GameObject priceContainer; 
    [SerializeField] private TextMeshProUGUI priceText; 

    [Header("Reroll References")]
    [SerializeField] private RectTransform rerollRect;
    [SerializeField] private Image rerollImage;
    
    [Header("Audio")]
    [SerializeField] private AudioClip sfxHover;
    [SerializeField] private AudioClip sfxPurchase;
    [SerializeField] private AudioClip sfxReroll;

    [Header("Main Motion")]
    [SerializeField] private float mainHoverScale = 1.015f;
    [SerializeField] private float transitionDuration = 0.05f;

    [Header("Main Color")]
    [SerializeField] private Color normalCaseColor = new Color(0.62f, 0.62f, 0.62f, 1f);
    [SerializeField] private Color hoverCaseColor = new Color(0.6f, 0.93f, 1f, 1f);
    [SerializeField] private Color soldOutCaseColor = new Color(0.3f, 0.3f, 0.3f, 1f); 
    [SerializeField] private Color normalTextColor = new Color(0.92f, 0.92f, 0.92f, 1f);
    [SerializeField] private Color hoverTextColor = Color.white;

    [Header("Reroll Motion")]
    [SerializeField] private float rerollHoverScale = 1.05f;

    [Header("Reroll Color")]
    [SerializeField] private Color normalRerollColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color hoverRerollColor = Color.white;
    [SerializeField] private Color usedRerollColor = new Color(0.4f, 0.4f, 0.4f, 1f); 

    private bool _isMainHovering;
    private bool _isRerollHovering;
    private Vector3 _caseBaseScale;
    private Vector3 _rerollBaseScale;

    private bool _isSoldOut;
    private bool _hasRerolled;
    private int _currentPrice;
    private ShopStatType _currentStatType;
    private int _currentStatValue;

    private Action _onScrapSpent; 

    public void SetHover(ShopHoverAreaRelay.HoverAreaType areaType, bool isHovering)
    {
        // ★ 핵심 수정: Sold Out이거나 이미 리롤을 썼다면 소리 및 Hover 상태 진입을 완벽 차단
        if (_isSoldOut) return;
        if (areaType == ShopHoverAreaRelay.HoverAreaType.Reroll && _hasRerolled) return;

        if (isHovering && sfxHover != null && SoundManager.instance != null)
        {
            SoundManager.instance.PlaySFX(sfxHover, 0.4f, 0.1f);
        }

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
        if (caseRect != null) _caseBaseScale = caseRect.localScale;
        if (rerollRect != null) _rerollBaseScale = rerollRect.localScale;
        ApplyImmediateVisual();
    }

    public void SetupOption(Action onScrapSpentCallback)
    {
        _onScrapSpent = onScrapSpentCallback;
        _isSoldOut = false;
        _hasRerolled = false;
        _isMainHovering = false;
        _isRerollHovering = false;

        if (soldOutOverlay != null) soldOutOverlay.SetActive(false);
        if (priceContainer != null) priceContainer.SetActive(true);
        if (rerollImage != null) rerollImage.gameObject.SetActive(true);

        GenerateRandomStat();
        ApplyImmediateVisual();
    }

    private void GenerateRandomStat()
    {
        _currentStatType = (ShopStatType)UnityEngine.Random.Range(0, 5);
        string statName = "";
        string hexColor = "";

        switch (_currentStatType)
        {
            case ShopStatType.AttackPower:
                _currentStatValue = UnityEngine.Random.Range(5, 11); 
                statName = "공격력";
                hexColor = "#FF4949";
                break;
            case ShopStatType.AttackSpeed:
                _currentStatValue = UnityEngine.Random.Range(3, 11); 
                statName = "공격 속도";
                hexColor = "#FFD100";
                break;
            case ShopStatType.MoveSpeed:
                _currentStatValue = UnityEngine.Random.Range(3, 11); 
                statName = "이동 속도";
                hexColor = "#3CD2F8";
                break;
            case ShopStatType.CritChance:
                _currentStatValue = UnityEngine.Random.Range(3, 8); 
                statName = "치명타 확률";
                hexColor = "#FF8C00";
                break;
            case ShopStatType.CritDamage:
                _currentStatValue = UnityEngine.Random.Range(5, 11); 
                statName = "치명타 피해";
                hexColor = "#FF33CC";
                break;
        }

        _currentPrice = UnityEngine.Random.Range(100, 201); 

        if (descriptionText != null)
            descriptionText.text = $"{statName} <color={hexColor}>+{_currentStatValue}%</color>";

        if (priceText != null)
            priceText.text = _currentPrice.ToString();
    }

    public void OnClickReroll()
    {
        if (_isSoldOut || _hasRerolled) return;

        if (sfxReroll != null && SoundManager.instance != null)
            SoundManager.instance.PlaySFX(sfxReroll, 1.3f, 0.1f);

        _hasRerolled = true;
        _isRerollHovering = false;
        
        GenerateRandomStat();
        _onScrapSpent?.Invoke(); 
    }

    public void OnClickMain()
    {
        if (_isSoldOut || GameManager.instance == null) return;

        int currentScrap = GameManager.instance.currentScrap;

        if (currentScrap >= _currentPrice)
        {
            GameManager.instance.currentScrap -= _currentPrice;

            if (GameManager.instance.stats != null)
            {
                GameManager.instance.stats.ApplyShopStat(_currentStatType, _currentStatValue);
            }

            if (sfxPurchase != null && SoundManager.instance != null)
                SoundManager.instance.PlaySFX(sfxPurchase, 0.7f, 0.1f);

            SetSoldOut();
            _onScrapSpent?.Invoke(); 
        }
        else
        {
            if (PlayerFeedbackUI.Instance != null)
                PlayerFeedbackUI.Instance.ShowWarning(6); 
        }
    }

    private void SetSoldOut()
    {
        _isSoldOut = true;
        _isMainHovering = false;
        _isRerollHovering = false;

        if (soldOutOverlay != null) soldOutOverlay.SetActive(true);
        if (priceContainer != null) priceContainer.SetActive(false);

        ApplyImmediateVisual();
    }

    public void UpdatePriceColor(int currentScrap)
    {
        if (_isSoldOut || priceText == null) return;
        priceText.color = currentScrap >= _currentPrice ? Color.black : Color.red;
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
            Vector3 targetScale = (_isMainHovering && !_isSoldOut) ? _caseBaseScale * mainHoverScale : _caseBaseScale;
            caseRect.localScale = Vector3.Lerp(caseRect.localScale, targetScale, Time.unscaledDeltaTime * speed);
        }

        if (caseImage != null)
        {
            Color targetColor = _isSoldOut ? soldOutCaseColor : (_isMainHovering ? hoverCaseColor : normalCaseColor);
            caseImage.color = Color.Lerp(caseImage.color, targetColor, Time.unscaledDeltaTime * speed);
        }

        if (descriptionText != null)
        {
            Color targetColor = (_isMainHovering && !_isSoldOut) ? hoverTextColor : normalTextColor;
            descriptionText.color = Color.Lerp(descriptionText.color, targetColor, Time.unscaledDeltaTime * speed);
        }
    }

    private void UpdateRerollVisual(float speed)
    {
        bool isRerollDisabled = _isSoldOut || _hasRerolled;

        if (rerollRect != null)
        {
            Vector3 targetScale = (_isRerollHovering && !isRerollDisabled) ? _rerollBaseScale * rerollHoverScale : _rerollBaseScale;
            rerollRect.localScale = Vector3.Lerp(rerollRect.localScale, targetScale, Time.unscaledDeltaTime * speed);
        }

        if (rerollImage != null)
        {
            Color targetColor = isRerollDisabled ? usedRerollColor : (_isRerollHovering ? hoverRerollColor : normalRerollColor);
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
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;

        if (caseRect != null) caseRect.localScale = _caseBaseScale;
        if (caseImage != null) caseImage.color = _isSoldOut ? soldOutCaseColor : normalCaseColor;
        if (descriptionText != null) descriptionText.color = normalTextColor;
        if (rerollRect != null) rerollRect.localScale = _rerollBaseScale;
        if (rerollImage != null) 
        {
            rerollImage.color = (_isSoldOut || _hasRerolled) ? usedRerollColor : normalRerollColor;
            Color c = rerollImage.color;
            c.a = 1f;
            rerollImage.color = c;
        }
    }
}