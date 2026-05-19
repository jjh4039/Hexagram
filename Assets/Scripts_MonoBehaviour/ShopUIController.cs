using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShopUIController : MonoBehaviour
{
    [SerializeField] private GameObject shopRoot;
    [SerializeField] private Vector3 shopCameraOffset = new Vector3(2f, 0f, 0f);

    [Header("References")]
    [SerializeField] private CanvasGroup backgroundGroup;
    [SerializeField] private CanvasGroup shopVisualGroup;
    [SerializeField] private CanvasGroup screenGlowGroup;
    [SerializeField] private CanvasGroup screenContentGroup;

    [Header("Shop Logic - Artifacts")]
    [SerializeField] private ShopHoverSystem[] artifactSlots; 

    [Header("LED")]
    [SerializeField] private Image[] leds;
    [SerializeField] private float ledOnInterval = 0.12f;
    [SerializeField] private float ledOffInterval = 0.04f;

    [Header("Slide")]
    [SerializeField] private float slideDistance = 35f;
    [SerializeField] private float openSlideDuration = 0.8f;
    [SerializeField] private float closeSlideDuration = 0.8f;

    [Header("Fade Settings")]
    [SerializeField] private float backgroundStartAlpha = 0f;
    [SerializeField] private float backgroundEndAlpha = 1f;
    [SerializeField] private float visualStartAlpha = 0.25f;
    [SerializeField] private float visualEndAlpha = 1f;
    [SerializeField] private float glowStartAlpha = 0f;
    [SerializeField] private float glowPeakAlpha = 1f;
    [SerializeField] private float glowEndAlpha = 1f;
    [SerializeField] private float glowFadeDelay = 0.55f;
    [SerializeField] private float glowFadeDuration = 0.5f;
    [SerializeField] private float contentStartAlpha = 0f;
    [SerializeField] private float contentEndAlpha = 1f;
    [SerializeField] private float contentFadeDelay = 0.3f;
    [SerializeField] private float contentFadeDuration = 0.6f;
    [SerializeField] private float contentFadeOutDuration = 0.15f;
    [SerializeField] private float screenFadeOutDelay = 0.05f;
    [SerializeField] private float screenFadeOutDuration = 0.15f;
    
    private bool _isOpen;
    private bool _isPopulated; 
    private RectTransform _shopRect;
    private Vector2 _closedAnchoredPos;
    private Vector2 _openAnchoredPos;

    private Coroutine _slideRoutine;
    private Coroutine _ledRoutine;

    public bool IsOpen => _isOpen;
    public event Action<bool> OnShopStateChanged;

    private void Awake()
    {
        if (shopRoot != null) _shopRect = shopRoot.GetComponent<RectTransform>();
    }

    private void Start()
    {
        if (shopRoot == null || _shopRect == null) return;

        _openAnchoredPos = _shopRect.anchoredPosition;
        _closedAnchoredPos = _openAnchoredPos + new Vector2(slideDistance, 0f);
        _shopRect.anchoredPosition = _closedAnchoredPos;

        backgroundGroup.alpha = 0f;
        shopVisualGroup.alpha = 0f;
        screenGlowGroup.alpha = 0f;
        screenContentGroup.alpha = 0f;
        SetAllLedsAlpha(0f);

        shopRoot.SetActive(false);

        if (InputStateManager.Instance != null)
            InputStateManager.Instance.Actions.UI.CloseUI.performed += OnEscapeInput;
    }

    private void OnDestroy()
    {
        if (InputStateManager.Instance != null)
            InputStateManager.Instance.Actions.UI.CloseUI.performed -= OnEscapeInput;
    }

    private void OnEscapeInput(InputAction.CallbackContext context)
    {
        if (_isOpen) CloseShop();
    }

    public void OpenShop()
    {
        if (shopRoot == null || _isOpen) return;
        if (InputStateManager.Instance != null && !InputStateManager.Instance.TryOpenUI()) return;

        _isOpen = true;
        OnShopStateChanged?.Invoke(true); 

        if (!_isPopulated)
        {
            GenerateShopItems();
            _isPopulated = true;
        }

        if (CameraFollow.instance != null) CameraFollow.instance.SetUIOffset(shopCameraOffset);

        shopRoot.SetActive(true);
        _shopRect.anchoredPosition = _closedAnchoredPos;

        backgroundGroup.alpha = backgroundStartAlpha;
        shopVisualGroup.alpha = visualStartAlpha;
        screenGlowGroup.alpha = glowStartAlpha;
        screenContentGroup.alpha = contentStartAlpha;

        if (_slideRoutine != null) StopCoroutine(_slideRoutine);
        _slideRoutine = StartCoroutine(SlideRoutine(true));

        if (_ledRoutine != null) StopCoroutine(_ledRoutine);
        _ledRoutine = StartCoroutine(LedOnRoutine());
    }

    private void GenerateShopItems()
    {
        if (ArtifactManager.instance == null || artifactSlots == null || artifactSlots.Length == 0) return;

        List<ArtifactData> availableArtifacts = new List<ArtifactData>();
        
        foreach (var artifact in ArtifactManager.instance.allArtifacts)
        {
            if (!ArtifactManager.instance.myArtifacts.Contains(artifact))
            {
                availableArtifacts.Add(artifact);
            }
        }

        for (int i = 0; i < availableArtifacts.Count; i++)
        {
            ArtifactData temp = availableArtifacts[i];
            int randomIndex = UnityEngine.Random.Range(i, availableArtifacts.Count);
            availableArtifacts[i] = availableArtifacts[randomIndex];
            availableArtifacts[randomIndex] = temp;
        }

        for (int i = 0; i < artifactSlots.Length; i++)
        {
            if (i < availableArtifacts.Count)
            {
                ArtifactData selectedData = availableArtifacts[i];
                int slotIndex = i; 
                
                artifactSlots[i].gameObject.SetActive(true);
                artifactSlots[i].SetupSlot(selectedData, () => TryBuyArtifact(slotIndex, selectedData));
            }
            else
            {
                artifactSlots[i].gameObject.SetActive(false); 
            }
        }
    }

    // ★ 수정: 가격 부족 시 PlayerFeedbackUI 연결
    private void TryBuyArtifact(int slotIndex, ArtifactData data)
    {
        if (GameManager.instance == null || data == null) return;

        int currentScrap = GameManager.instance.currentScrap;

        if (currentScrap >= data.basePrice)
        {
            GameManager.instance.currentScrap -= data.basePrice;
            ArtifactManager.instance.AddArtifact(data);
            artifactSlots[slotIndex].SetSoldOut();

            Debug.Log($"[상점] {data.artifactName} 구매 완료! 남은 스크랩: {GameManager.instance.currentScrap}");
        }
        else
        {
            // ★ 수정: 피드백 UI 출력 (인덱스 6: "스크랩이 부족합니다.")
            if (PlayerFeedbackUI.Instance != null)
            {
                PlayerFeedbackUI.Instance.ShowWarning(6);
            }
            Debug.Log($"[상점] 스크랩 부족! 필요: {data.basePrice}, 보유: {currentScrap}");
        }
    }

    public void OnClickReroll()
    {
        int rerollCost = 50; 
        if (GameManager.instance.currentScrap >= rerollCost)
        {
            GameManager.instance.currentScrap -= rerollCost;
            GenerateShopItems(); 
        }
        else
        {
            if (PlayerFeedbackUI.Instance != null)
                PlayerFeedbackUI.Instance.ShowWarning(6);
        }
    }

    public void CloseShop()
    {
        if (shopRoot == null || !_isOpen) return;

        _isOpen = false;
        OnShopStateChanged?.Invoke(false); 

        if (InputStateManager.Instance != null) InputStateManager.Instance.CloseUI();
        if (CameraFollow.instance != null) CameraFollow.instance.ResetUIOffset();

        if (_slideRoutine != null) StopCoroutine(_slideRoutine);
        _slideRoutine = StartCoroutine(SlideRoutine(false));

        if (_ledRoutine != null) StopCoroutine(_ledRoutine);
        _ledRoutine = StartCoroutine(LedOffRoutine());
    }

    private IEnumerator SlideRoutine(bool isOpening)
    {
        float elapsed = 0f;
        float duration = isOpening ? openSlideDuration : closeSlideDuration;
        Vector2 startPos = isOpening ? _closedAnchoredPos : _openAnchoredPos;
        Vector2 endPos = isOpening ? _openAnchoredPos : _closedAnchoredPos;

        float startBackgroundAlpha = isOpening ? backgroundStartAlpha : backgroundGroup.alpha;
        float endBackgroundAlpha = isOpening ? backgroundEndAlpha : 0f;

        float startVisualAlpha = isOpening ? visualStartAlpha : shopVisualGroup.alpha;
        float endVisualAlpha = isOpening ? visualEndAlpha : 0f;

        float startGlowAlpha = isOpening ? glowStartAlpha : screenGlowGroup.alpha;
        float endGlowAlpha = isOpening ? glowEndAlpha : 0f;

        float startContentAlpha = isOpening ? contentStartAlpha : screenContentGroup.alpha;
        float endContentAlpha = isOpening ? contentEndAlpha : 0f;

        _shopRect.anchoredPosition = startPos;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 5f);

            _shopRect.anchoredPosition = Vector2.Lerp(startPos, endPos, easedT);
            backgroundGroup.alpha = Mathf.Lerp(startBackgroundAlpha, endBackgroundAlpha, easedT);

            HandleFading(isOpening, elapsed, easedT, duration, startVisualAlpha, endVisualAlpha, startGlowAlpha, startContentAlpha);

            yield return null;
        }

        _shopRect.anchoredPosition = endPos;
        backgroundGroup.alpha = endBackgroundAlpha;
        shopVisualGroup.alpha = endVisualAlpha;
        screenGlowGroup.alpha = endGlowAlpha;
        screenContentGroup.alpha = endContentAlpha;

        if (!isOpening) shopRoot.SetActive(false);
        _slideRoutine = null;
    }

    private void HandleFading(bool isOpening, float elapsed, float easedT, float duration, float startVis, float endVis, float startGlow, float startContent)
    {
        if (isOpening) shopVisualGroup.alpha = Mathf.Lerp(startVis, endVis, easedT);
        else if (elapsed > screenFadeOutDelay)
        {
            float sT = Mathf.Clamp01((elapsed - screenFadeOutDelay) / screenFadeOutDuration);
            shopVisualGroup.alpha = Mathf.Lerp(startVis, 0f, 1f - Mathf.Pow(1f - sT, 3f));
        }

        if (isOpening)
        {
            float glowTime = glowFadeDelay * duration;
            if (elapsed > glowTime)
            {
                float gT = Mathf.Clamp01((elapsed - glowTime) / glowFadeDuration);
                screenGlowGroup.alpha = gT < 0.5f ? Mathf.Lerp(glowStartAlpha, glowPeakAlpha, gT / 0.5f) : Mathf.Lerp(glowPeakAlpha, glowEndAlpha, (gT - 0.5f) / 0.5f);
            }
        }
        else
        {
            float gT = Mathf.Clamp01(elapsed / screenFadeOutDuration);
            screenGlowGroup.alpha = Mathf.Lerp(screenGlowGroup.alpha, 0f, 1f - Mathf.Pow(1f - gT, 3f));
        }

        if (isOpening)
        {
            float contentTime = contentFadeDelay * duration;
            if (elapsed > contentTime)
            {
                float cT = Mathf.Clamp01((elapsed - contentTime) / contentFadeDuration);
                screenContentGroup.alpha = Mathf.Lerp(startContent, contentEndAlpha, 1f - Mathf.Pow(1f - cT, 4f));
            }
        }
        else
        {
            float cT = Mathf.Clamp01(elapsed / contentFadeOutDuration);
            screenContentGroup.alpha = Mathf.Lerp(startContent, 0f, 1f - Mathf.Pow(1f - cT, 3f));
        }
    }

    private IEnumerator LedOnRoutine()
    {
        SetAllLedsAlpha(0f);
        for (int i = 0; i < leds.Length; i++)
        {
            if (leds[i]) StartCoroutine(LedPulseRoutine(leds[i]));
            yield return new WaitForSecondsRealtime(ledOnInterval);
        }
        _ledRoutine = null;
    }

    private IEnumerator LedOffRoutine()
    {
        for (int i = leds.Length - 1; i >= 0; i--)
        {
            if (leds[i]) SetLedAlpha(leds[i], 0f);
            yield return new WaitForSecondsRealtime(ledOffInterval);
        }
        _ledRoutine = null;
    }

    private void SetAllLedsAlpha(float alpha)
    {
        if (leds == null) return;
        foreach (var led in leds) if (led) SetLedAlpha(led, alpha);
    }

    private void SetLedAlpha(Image led, float alpha)
    {
        Color color = led.color;
        color.a = alpha;
        led.color = color;
    }
    
    private IEnumerator LedPulseRoutine(Image led)
    {
        float t = 0f;
        float duration = 0.06f;
        while (t < duration) { t += Time.unscaledDeltaTime; SetLedAlpha(led, Mathf.Clamp01(t / duration)); yield return null; }
        t = 0f;
        while (t < duration * 0.5f) { t += Time.unscaledDeltaTime; SetLedAlpha(led, Mathf.Lerp(1f, 0.85f, t / (duration * 0.5f))); yield return null; }
        t = 0f;
        while (t < duration * 0.5f) { t += Time.unscaledDeltaTime; SetLedAlpha(led, Mathf.Lerp(0.85f, 1f, t / (duration * 0.5f))); yield return null; }
        SetLedAlpha(led, 1f);
    }
}