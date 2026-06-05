using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TotalGems : MonoBehaviour
{
    public enum UpgradeType { Health, Attack, Bullet, Difficulty }

    [System.Serializable]
    public class UpgradeRow
    {
        public UpgradeType type;
        public GemUpgradeButton minusBtn;
        public GemUpgradeButton plusBtn;
        
        [Tooltip("마이너스 버튼 하단 텍스트 (-비용)")]
        public TextMeshProUGUI minusCostText; 
        
        [Tooltip("플러스 버튼 하단 텍스트 (+비용)")]
        public TextMeshProUGUI plusCostText;  
        
        public TextMeshProUGUI effectText; 
        public TextMeshProUGUI levelText; // 현재 레벨 표시 텍스트

        [Header("Settings")]
        public int costPerLevel = 1;       
        public int maxLevel = 10;          
        
        [Header("Value 1")]
        public float value1PerLevel = 20f; 
        public int levelStep1 = 1;

        [Header("Value 2")]
        public float value2PerLevel = 0f;  
        public int levelStep2 = 1;

        [Header("Format")]
        public string effectTextFormat = "+{0}";
    }

    public bool IsOpen => _isOpen;

    [Header("UI References")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private CanvasGroup visualGroup;
    [SerializeField] private RectTransform bgRect;
    
    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI topTotalGemsText;    
    [SerializeField] private TextMeshProUGUI bottomRemainingText; 

    [Header("Upgrades")]
    [SerializeField] private UpgradeRow[] upgradeRows; 

    [Header("Animation Settings")]
    [SerializeField] private float targetBgWidth = 800f; 
    [SerializeField] private float bgExpandDuration = 0.25f;

    [Header("Sound")]
    [SerializeField] private AudioClip sfxOpen;
    [SerializeField] private AudioClip sfxClose;
    [SerializeField] private AudioClip sfxUpgrade; 

    private bool _isOpen;
    private bool _isAnimating;
    private Coroutine _animCoroutine;
    private Vector2 _bgOriginAnchoredPos;

    private int _totalGems;
    private int _remainingGems;
    private bool _wasShiftPressed; 

    private void Start()
    {
        RefreshDataAndUI();

        if (visualRoot != null) visualRoot.SetActive(false);
        if (bgRect != null) _bgOriginAnchoredPos = bgRect.anchoredPosition;

        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.Actions.UI.CloseUI.performed += OnCloseUI;
        }

        InitializeButtons();
    }

    private void OnDestroy()
    {
        if (InputStateManager.Instance != null && InputStateManager.Instance.Actions != null)
        {
            InputStateManager.Instance.Actions.UI.CloseUI.performed -= OnCloseUI;
        }
    }

    private void Update()
    {
        if (!_isOpen) return;

        bool isShiftPressed = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
        if (_wasShiftPressed != isShiftPressed)
        {
            _wasShiftPressed = isShiftPressed;
            foreach (var row in upgradeRows)
            {
                UpdateRowUI(row);
            }
        }
    }

    private void InitializeButtons()
    {
        foreach (var row in upgradeRows)
        {
            var currentRow = row; 
            
            if (currentRow.minusBtn != null)
                currentRow.minusBtn.OnClicked += () => OnMinusClicked(currentRow);
                
            if (currentRow.plusBtn != null)
                currentRow.plusBtn.OnClicked += () => OnPlusClicked(currentRow);
        }
    }

    private void OnCloseUI(InputAction.CallbackContext ctx)
    {
        if (!_isOpen || _isAnimating) return;
        CloseUI();
    }

    public void OpenUI()
    {
        if (_isOpen || _isAnimating) return;
        _isOpen = true;
    
        RefreshDataAndUI(); 

        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.SetForceMouseMode(true); // 마우스 강제 모드 켜기
        }

        if (sfxOpen && SoundManager.instance) SoundManager.instance.PlaySFX(sfxOpen, 0.5f);

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateOpen());
    }

    public void CloseUI()
    {
        if (!_isOpen || _isAnimating) return;
        _isOpen = false;

        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.SetForceMouseMode(false); // 마우스 강제 모드 끄기
        }

        if (sfxClose && SoundManager.instance) SoundManager.instance.PlaySFX(sfxClose, 0.5f);

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateClose());
    }

    private void RefreshDataAndUI()
    {
        if (DataManager.instance == null || DataManager.instance.data == null) return;

        GameData data = DataManager.instance.data;
        _totalGems = data.totalGems;

        int usedGems = 0;
        foreach (var row in upgradeRows)
        {
            int currentLevel = GetLevelFromData(row.type);
            usedGems += currentLevel * row.costPerLevel;
        }

        _remainingGems = _totalGems - usedGems;

        if (topTotalGemsText != null) topTotalGemsText.text = _totalGems.ToString();
        if (bottomRemainingText != null) bottomRemainingText.text = $"{_remainingGems} / {_totalGems}";

        foreach (var row in upgradeRows)
        {
            UpdateRowUI(row);
        }
    }

    private void UpdateRowUI(UpgradeRow row)
    {
        int currentLevel = GetLevelFromData(row.type);
        bool isShiftPressed = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;

        if (row.levelText != null)
        {
            row.levelText.text = $"{currentLevel}"; // 현재 레벨 표기 갱신
        }

        int minusAmount = isShiftPressed ? 5 : 1;
        int actualMinus = Mathf.Min(minusAmount, currentLevel);
        if (actualMinus <= 0) actualMinus = 1; 

        bool canMinus = currentLevel > 0;
        if (row.minusBtn != null) row.minusBtn.SetInteractable(canMinus);
        
        if (row.minusCostText != null)
        {
            row.minusCostText.gameObject.SetActive(canMinus);
            if (canMinus) row.minusCostText.text = $"-{actualMinus * row.costPerLevel}";
        }

        int plusAmount = isShiftPressed ? 5 : 1;
        int maxLevelUp = row.maxLevel - currentLevel;
        int actualPlus = Mathf.Min(plusAmount, maxLevelUp);
        if (actualPlus <= 0) actualPlus = 1; 

        int maxAffordable = Mathf.Max(0, _remainingGems) / row.costPerLevel;
        int finalPlusAmount = Mathf.Min(actualPlus, maxAffordable);
        
        bool canPlus = _remainingGems >= row.costPerLevel && currentLevel < row.maxLevel;
        if (row.plusBtn != null) row.plusBtn.SetInteractable(canPlus);

        if (row.plusCostText != null)
        {
            row.plusCostText.gameObject.SetActive(canPlus);
            if (canPlus) 
            {
                int displayPlusCost = (finalPlusAmount > 0 ? finalPlusAmount : actualPlus) * row.costPerLevel;
                row.plusCostText.text = $"+{displayPlusCost}";
            }
        }

        if (row.effectText != null)
        {
            float totalVal1 = (currentLevel / Mathf.Max(1, row.levelStep1)) * row.value1PerLevel;
            float totalVal2 = (currentLevel / Mathf.Max(1, row.levelStep2)) * row.value2PerLevel;

            row.effectText.text = string.Format(row.effectTextFormat, totalVal1, totalVal2);
        }
    }

    private void OnMinusClicked(UpgradeRow row)
    {
        if (!_isOpen || _isAnimating) return;                // 창이 닫히거나 애니메이션 중일 때 클릭 완벽 차단

        int currentLevel = GetLevelFromData(row.type);
        if (currentLevel <= 0) return;

        bool isShiftPressed = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
        int amountToDeduct = isShiftPressed ? 5 : 1;
        amountToDeduct = Mathf.Min(amountToDeduct, currentLevel); 

        SetLevelToData(row.type, currentLevel - amountToDeduct);
        
        if (sfxUpgrade && SoundManager.instance) SoundManager.instance.PlaySFX(sfxUpgrade, 0.4f);
        
        DataManager.instance.SaveGame(); 
        RefreshDataAndUI();
    }

    private void OnPlusClicked(UpgradeRow row)
    {
        if (!_isOpen || _isAnimating) return;                // 창이 닫히거나 애니메이션 중일 때 클릭 완벽 차단

        int currentLevel = GetLevelFromData(row.type);
        if (_remainingGems < row.costPerLevel || currentLevel >= row.maxLevel) return;

        bool isShiftPressed = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
        int amountToAdd = isShiftPressed ? 5 : 1;

        int maxAffordable = Mathf.Max(0, _remainingGems) / row.costPerLevel;
        int maxLevelUp = row.maxLevel - currentLevel;
        
        amountToAdd = Mathf.Min(amountToAdd, maxAffordable, maxLevelUp);

        if (amountToAdd <= 0) return;

        SetLevelToData(row.type, currentLevel + amountToAdd);
        
        if (sfxUpgrade && SoundManager.instance) SoundManager.instance.PlaySFX(sfxUpgrade, 0.5f);

        DataManager.instance.SaveGame(); 
        RefreshDataAndUI();
    }

    private int GetLevelFromData(UpgradeType type)
    {
        if (DataManager.instance == null) return 0;
        GameData data = DataManager.instance.data;

        switch (type)
        {
            case UpgradeType.Health: return data.upgradeHealthLevel;
            case UpgradeType.Attack: return data.upgradeAttackLevel;
            case UpgradeType.Bullet: return data.upgradeBulletLevel;
            case UpgradeType.Difficulty: return data.difficultyLevel;
        }
        return 0;
    }

    private void SetLevelToData(UpgradeType type, int newLevel)
    {
        if (DataManager.instance == null) return;
        GameData data = DataManager.instance.data;

        switch (type)
        {
            case UpgradeType.Health: data.upgradeHealthLevel = newLevel; break;
            case UpgradeType.Attack: data.upgradeAttackLevel = newLevel; break;
            case UpgradeType.Bullet: data.upgradeBulletLevel = newLevel; break;
            case UpgradeType.Difficulty: data.difficultyLevel = newLevel; break;
        }
    }

    private IEnumerator AnimateOpen()
    {
        _isAnimating = true;
        visualRoot.SetActive(true);

        float currentHeight = bgRect.sizeDelta.y;
        bgRect.sizeDelta = new Vector2(0f, currentHeight); 
        
        if (visualGroup != null) visualGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < bgExpandDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / bgExpandDuration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            
            bgRect.sizeDelta = new Vector2(Mathf.Lerp(0f, targetBgWidth, easedT), currentHeight);
            if (visualGroup != null) visualGroup.alpha = t;
            
            yield return null;
        }

        bgRect.sizeDelta = new Vector2(targetBgWidth, currentHeight);
        if (visualGroup != null) visualGroup.alpha = 1f;

        _isAnimating = false;
    }

    private IEnumerator AnimateClose()
    {
        _isAnimating = true;

        float elapsed = 0f;
        float startWidth = bgRect.sizeDelta.x;
        float currentHeight = bgRect.sizeDelta.y;

        while (elapsed < bgExpandDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / bgExpandDuration);
            float easedT = t * t * t; 

            bgRect.sizeDelta = new Vector2(Mathf.Lerp(startWidth, 0f, easedT), currentHeight);
            if (visualGroup != null) visualGroup.alpha = 1f - t;
            
            yield return null;
        }

        visualRoot.SetActive(false);
        bgRect.anchoredPosition = _bgOriginAnchoredPos;

        _isAnimating = false;
    }
}