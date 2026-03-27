using System.Collections;
using TMPro;
using UnityEngine;

public class ShopTooltipUI : MonoBehaviour
{
    public static ShopTooltipUI Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Show Motion")]
    [SerializeField] private float showDuration = 0.12f;
    [SerializeField] private Vector2 showStartOffset = new Vector2(3f, -5f);

    [Header("Hide Motion")]
    [SerializeField] private float hideDuration = 0.08f;
    [SerializeField] private Vector2 hideOffset = Vector2.zero;

    [Header("Swap Motion")]
    [SerializeField] private float swapDuration = 0.08f;
    [SerializeField] private Vector2 swapOffset = new Vector2(4f, -1f);

    [SerializeField] private float hideDelay = 0.03f;

    private Coroutine _routine;
    private Vector2 _baseAnchoredPos;
    private bool _isVisible;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (tooltipRect != null)
            _baseAnchoredPos = tooltipRect.anchoredPosition;

        HideImmediate();
    }

    public void ShowTooltip(string title, string rarity, string description)
    {
        bool wasVisible = _isVisible;

        SetTexts(title, rarity, description);

        if (_routine != null)
            StopCoroutine(_routine);

        if (wasVisible)
            _routine = StartCoroutine(SwapRoutine());
        else
            _routine = StartCoroutine(ShowRoutine());
    }

    public void HideTooltip()
    {
        if (!_isVisible)
            return;

        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(HideDelayedRoutine());
    }

    private IEnumerator HideDelayedRoutine()
    {
        yield return new WaitForSecondsRealtime(hideDelay);

        _routine = StartCoroutine(HideRoutine());
    }


    public void HideImmediate()
    {
        _isVisible = false;

        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (tooltipRect != null)
            tooltipRect.anchoredPosition = _baseAnchoredPos + showStartOffset;
    }

    private void SetTexts(string title, string rarity, string description)
    {
        if (titleText != null)
            titleText.text = title;

        if (rarityText != null)
            rarityText.text = rarity;

        if (descriptionText != null)
            descriptionText.text = description;
    }

    private IEnumerator ShowRoutine()
    {
        _isVisible = true;

        float elapsed = 0f;
        Vector2 startPos = _baseAnchoredPos + showStartOffset;
        Vector2 endPos = _baseAnchoredPos;

        while (elapsed < showDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / showDuration);
            float eased = 1f - Mathf.Pow(1f - t, 4f);

            if (canvasGroup != null)
                canvasGroup.alpha = eased;

            if (tooltipRect != null)
                tooltipRect.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (tooltipRect != null)
            tooltipRect.anchoredPosition = _baseAnchoredPos;

        _routine = null;
    }

    private IEnumerator SwapRoutine()
    {
        _isVisible = true;

        float elapsed = 0f;
        Vector2 startPos = _baseAnchoredPos + swapOffset;
        Vector2 endPos = _baseAnchoredPos;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (tooltipRect != null)
            tooltipRect.anchoredPosition = startPos;

        while (elapsed < swapDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / swapDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            if (tooltipRect != null)
                tooltipRect.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);

            yield return null;
        }

        if (tooltipRect != null)
            tooltipRect.anchoredPosition = _baseAnchoredPos;

        _routine = null;
    }

    private IEnumerator HideRoutine()
    {
        float elapsed = 0f;
        Vector2 startPos = _baseAnchoredPos;
        Vector2 endPos = _baseAnchoredPos + hideOffset;

        while (elapsed < hideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / hideDuration);

            if (canvasGroup != null)
                canvasGroup.alpha = 1f - t;

            if (tooltipRect != null)
                tooltipRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (tooltipRect != null)
            tooltipRect.anchoredPosition = _baseAnchoredPos + showStartOffset;

        _isVisible = false;
        _routine = null;
    }
}