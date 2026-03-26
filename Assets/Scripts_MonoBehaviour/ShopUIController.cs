using System.Collections;
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

    [Header("LED")]
    [SerializeField] private Image[] leds;
    [SerializeField] private float ledOnInterval = 0.12f;
    [SerializeField] private float ledOffInterval = 0.04f;

    [Header("Slide")]
    [SerializeField] private float slideDistance = 35f;
    [SerializeField] private float openSlideDuration = 0.8f;
    [SerializeField] private float closeSlideDuration = 0.8f;

    [Header("Fade")]
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
    public bool IsOpen => _isOpen;
    // private bool _isAnimating;

    private RectTransform _shopRect;
    private Vector2 _closedAnchoredPos;
    private Vector2 _openAnchoredPos;

    private Coroutine _slideRoutine;
    private Coroutine _ledRoutine;

    private void Awake()
    {
        if (shopRoot != null)
            _shopRect = shopRoot.GetComponent<RectTransform>();
    }

    private void Start()
    {
        if (shopRoot == null || _shopRect == null || backgroundGroup == null || shopVisualGroup == null || screenGlowGroup == null || screenContentGroup == null)
            return;

        _openAnchoredPos = _shopRect.anchoredPosition;
        _closedAnchoredPos = _openAnchoredPos + new Vector2(slideDistance, 0f);

        _shopRect.anchoredPosition = _closedAnchoredPos;

        backgroundGroup.alpha = 0f;
        shopVisualGroup.alpha = 0f;
        screenGlowGroup.alpha = 0f;
        screenContentGroup.alpha = 0f;

        SetAllLedsAlpha(0f);

        shopRoot.SetActive(false);
    }

    public void OpenShop()
    {
        if (shopRoot == null || _shopRect == null || backgroundGroup == null || shopVisualGroup == null || screenGlowGroup == null || screenContentGroup == null)
            return;

        _isOpen = true;

        if (GameManager.instance != null && GameManager.instance.player != null)
            GameManager.instance.player.canControl = false;

        if (CameraFollow.instance != null)
            CameraFollow.instance.SetUIOffset(shopCameraOffset);

        shopRoot.SetActive(true);
        _shopRect.anchoredPosition = _closedAnchoredPos;

        backgroundGroup.alpha = backgroundStartAlpha;
        shopVisualGroup.alpha = visualStartAlpha;
        screenGlowGroup.alpha = glowStartAlpha;
        screenContentGroup.alpha = contentStartAlpha;

        if (_slideRoutine != null)
            StopCoroutine(_slideRoutine);

        _slideRoutine = StartCoroutine(SlideRoutine(true));

        if (_ledRoutine != null)
            StopCoroutine(_ledRoutine);

        _ledRoutine = StartCoroutine(LedOnRoutine());
    }

    public void CloseShop()
    {
        if (shopRoot == null || _shopRect == null || backgroundGroup == null || shopVisualGroup == null || screenGlowGroup == null || screenContentGroup == null)
            return;

        _isOpen = false;

        if (GameManager.instance != null && GameManager.instance.player != null)
            GameManager.instance.player.canControl = true;

        if (CameraFollow.instance != null)
            CameraFollow.instance.ResetUIOffset();

        if (_slideRoutine != null)
            StopCoroutine(_slideRoutine);

        _slideRoutine = StartCoroutine(SlideRoutine(false));

        if (_ledRoutine != null)
            StopCoroutine(_ledRoutine);

        _ledRoutine = StartCoroutine(LedOffRoutine());
    }

    private IEnumerator SlideRoutine(bool isOpening)
    {
       // _isAnimating = true;

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

            float visualAlpha;

            if (isOpening)
            {
                visualAlpha = Mathf.Lerp(startVisualAlpha, endVisualAlpha, easedT);
            }
            else
            {
                visualAlpha = startVisualAlpha;

                if (elapsed > screenFadeOutDelay)
                {
                    float screenElapsed = elapsed - screenFadeOutDelay;
                    float screenT = Mathf.Clamp01(screenElapsed / screenFadeOutDuration);
                    float easedScreenT = 1f - Mathf.Pow(1f - screenT, 3f);

                    visualAlpha = Mathf.Lerp(startVisualAlpha, 0f, easedScreenT);
                }
            }

            shopVisualGroup.alpha = visualAlpha;

            float glowAlpha = startGlowAlpha;

            if (isOpening)
            {
                float glowDelayTime = glowFadeDelay * duration;

                if (elapsed > glowDelayTime)
                {
                    float glowElapsed = elapsed - glowDelayTime;
                    float glowT = Mathf.Clamp01(glowElapsed / glowFadeDuration);

                    if (glowT < 0.5f)
                    {
                        float peakT = glowT / 0.5f;
                        glowAlpha = Mathf.Lerp(glowStartAlpha, glowPeakAlpha, peakT);
                    }
                    else
                    {
                        float settleT = (glowT - 0.5f) / 0.5f;
                        glowAlpha = Mathf.Lerp(glowPeakAlpha, glowEndAlpha, settleT);
                    }
                }
            }
            else
            {
                float glowT = Mathf.Clamp01(elapsed / screenFadeOutDuration);
                float easedGlowT = 1f - Mathf.Pow(1f - glowT, 3f);
                glowAlpha = Mathf.Lerp(startGlowAlpha, 0f, easedGlowT);
            }

            screenGlowGroup.alpha = glowAlpha;

            float contentAlpha = startContentAlpha;

            if (isOpening)
            {
                float delayTime = contentFadeDelay * duration;

                if (elapsed > delayTime)
                {
                    float fadeElapsed = elapsed - delayTime;
                    float contentT = Mathf.Clamp01(fadeElapsed / contentFadeDuration);
                    float easedContentT = 1f - Mathf.Pow(1f - contentT, 4f);

                    contentAlpha = Mathf.Lerp(startContentAlpha, endContentAlpha, easedContentT);
                }
            }
            else
            {
                float contentT = Mathf.Clamp01(elapsed / contentFadeOutDuration);
                float easedContentT = 1f - Mathf.Pow(1f - contentT, 3f);

                contentAlpha = Mathf.Lerp(startContentAlpha, 0f, easedContentT);
            }

            screenContentGroup.alpha = contentAlpha;

            yield return null;
        }

        _shopRect.anchoredPosition = endPos;
        backgroundGroup.alpha = endBackgroundAlpha;
        shopVisualGroup.alpha = endVisualAlpha;
        screenGlowGroup.alpha = endGlowAlpha;
        screenContentGroup.alpha = endContentAlpha;

        if (!isOpening)
            shopRoot.SetActive(false);

       // _isAnimating = false;
        _slideRoutine = null;
    }

    private IEnumerator LedOnRoutine()
    {
        SetAllLedsAlpha(0f);

        // 1번 LED
        if (leds.Length > 0 && leds[0])
            StartCoroutine(LedPulseRoutine(leds[0]));

        yield return new WaitForSecondsRealtime(ledOnInterval);

        // 2번 LED (1번은 이미 켜져 있음)
        if (leds.Length > 1 && leds[1])
            StartCoroutine(LedPulseRoutine(leds[1]));

        yield return new WaitForSecondsRealtime(ledOnInterval);

        // 3번 LED (1,2는 유지된 상태)
        if (leds.Length > 2 && leds[2])
            StartCoroutine(LedPulseRoutine(leds[2]));

        _ledRoutine = null;
    }

    private IEnumerator LedOffRoutine()
    {
        for (int i = leds.Length - 1; i >= 0; i--)
        {
            if (leds[i])
                SetLedAlpha(leds[i], 0f);

            yield return new WaitForSecondsRealtime(ledOffInterval);
        }

        _ledRoutine = null;
    }

    private void SetAllLedsAlpha(float alpha)
    {
        if (leds == null)
            return;

        for (int i = 0; i < leds.Length; i++)
        {
            if (leds[i])
                SetLedAlpha(leds[i], alpha);
        }
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

        // 1. 부드럽게 켜짐 (0 → 1)
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(t / duration);
            SetLedAlpha(led, alpha);
            yield return null;
        }

        // 2. 살짝 꺼졌다가 (1 → 0.85)
        t = 0f;
        while (t < duration * 0.5f)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0.85f, t / (duration * 0.5f));
            SetLedAlpha(led, alpha);
            yield return null;
        }

        // 3. 다시 안정 (0.85 → 1)
        t = 0f;
        while (t < duration * 0.5f)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0.85f, 1f, t / (duration * 0.5f));
            SetLedAlpha(led, alpha);
            yield return null;
        }

        SetLedAlpha(led, 1f);
    }
}