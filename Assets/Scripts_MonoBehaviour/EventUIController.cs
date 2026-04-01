using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class EventUIController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject eventRoot;
    [SerializeField] private Vector3 eventCameraOffset = new Vector3(0f, -2f, 0f);

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup backgroundGroup;
    [SerializeField] private CanvasGroup eventVisualGroup;
    [SerializeField] private CanvasGroup eventContentGroup;

    [Header("Panel Slide")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private float slideDistanceY = 25f;
    [SerializeField] private float openSlideDuration = 0.5f;
    [SerializeField] private float closeSlideDuration = 0.35f;

    [Header("Fade")]
    [SerializeField] private float backgroundStartAlpha = 0f;
    [SerializeField] private float backgroundEndAlpha = 1f;
    [SerializeField] private float visualStartAlpha = 0f;
    [SerializeField] private float visualEndAlpha = 1f;
    [SerializeField] private float contentStartAlpha = 0f;
    [SerializeField] private float contentEndAlpha = 1f;
    [SerializeField] private float contentFadeDelay = 0.08f;

    [Header("Roulette Text")]
    [SerializeField] private TextMeshProUGUI riskText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI riskDescriptionText;
    [SerializeField] private TextMeshProUGUI rewardDescriptionText;
    [SerializeField] private float rouletteInterval = 0.05f;
    [SerializeField] private float rouletteDuration = 0.5f;
    [SerializeField] private float delayBetweenRiskAndReward = 0.35f;

    [Header("Default Preview")]
    [SerializeField] private int defaultDescriptionIndex = 0;

    [Header("Card Animation")]
    [SerializeField] private RectTransform riskCard;
    [SerializeField] private RectTransform rewardCard;
    [SerializeField] private CanvasGroup riskCardGroup;
    [SerializeField] private CanvasGroup rewardCardGroup;
    [SerializeField] private float cardFloatY = 30f;
    [SerializeField] private float cardAnimDuration = 0.4f;
    [SerializeField] private float cardStartScale = 0.85f;

    [Header("Card Inner")]
    [SerializeField] private CanvasGroup riskTitleBoxGroup;
    [SerializeField] private CanvasGroup riskBodyBoxGroup;
    [SerializeField] private CanvasGroup rewardTitleBoxGroup;
    [SerializeField] private CanvasGroup rewardBodyBoxGroup;
    [SerializeField] private float innerDelay = 0.08f;
    [SerializeField] private float boxFadeDuration = 0.08f;

    [Header("Optional")]
    [SerializeField] private bool closeWithEscape = true;

    private bool _isOpen;
    private Vector2 _openAnchoredPos;
    private Vector2 _closedAnchoredPos;
    private Vector2 _riskCardOriginalPos;
    private Vector2 _rewardCardOriginalPos;

    private Coroutine _slideRoutine;
    private Coroutine _sequenceRoutine;

    public bool IsOpen => _isOpen;

    private void Awake()
    {
        if (panelRect == null && eventRoot != null)
            panelRect = eventRoot.GetComponent<RectTransform>();
    }

    private void Start()
    {
        if (eventRoot == null || panelRect == null)
            return;

        _openAnchoredPos = panelRect.anchoredPosition;
        _closedAnchoredPos = _openAnchoredPos + new Vector2(0f, -slideDistanceY);

        if (riskCard != null)
            _riskCardOriginalPos = riskCard.anchoredPosition;

        if (rewardCard != null)
            _rewardCardOriginalPos = rewardCard.anchoredPosition;

        panelRect.anchoredPosition = _closedAnchoredPos;

        if (backgroundGroup != null) backgroundGroup.alpha = 0f;
        if (eventVisualGroup != null) eventVisualGroup.alpha = 0f;
        if (eventContentGroup != null) eventContentGroup.alpha = 0f;

        SetCardHidden(
            riskCard,
            riskCardGroup,
            riskTitleBoxGroup,
            riskBodyBoxGroup,
            _riskCardOriginalPos
        );

        SetCardHidden(
            rewardCard,
            rewardCardGroup,
            rewardTitleBoxGroup,
            rewardBodyBoxGroup,
            _rewardCardOriginalPos
        );

        ClearTexts();
        eventRoot.SetActive(false);
    }

    private void Update()
    {
        if (!_isOpen || !closeWithEscape || Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseEvent();
    }

    public void OpenEvent()
    {
        if (eventRoot == null || panelRect == null)
            return;

        if (EventManager.Instance == null || !EventManager.Instance.CurrentEventSelection.IsValid())
            return;

        _isOpen = true;

        if (GameManager.instance != null && GameManager.instance.player != null)
            GameManager.instance.player.canControl = false;

        if (CameraFollow.instance != null)
            CameraFollow.instance.SetUIOffset(eventCameraOffset);

        eventRoot.SetActive(true);
        panelRect.anchoredPosition = _closedAnchoredPos;

        if (backgroundGroup != null) backgroundGroup.alpha = backgroundStartAlpha;
        if (eventVisualGroup != null) eventVisualGroup.alpha = visualStartAlpha;
        if (eventContentGroup != null) eventContentGroup.alpha = contentStartAlpha;

        SetCardHidden(
            riskCard,
            riskCardGroup,
            riskTitleBoxGroup,
            riskBodyBoxGroup,
            _riskCardOriginalPos
        );

        SetCardHidden(
            rewardCard,
            rewardCardGroup,
            rewardTitleBoxGroup,
            rewardBodyBoxGroup,
            _rewardCardOriginalPos
        );

        ClearTexts();

        StopRunningRoutines();

        _slideRoutine = StartCoroutine(SlideRoutine(true));
        _sequenceRoutine = StartCoroutine(EventSequenceRoutine());
    }

    public void CloseEvent()
    {
        if (!_isOpen)
            return;

        _isOpen = false;

        if (GameManager.instance != null && GameManager.instance.player != null)
            GameManager.instance.player.canControl = true;

        if (CameraFollow.instance != null)
            CameraFollow.instance.ResetUIOffset();

        StopRunningRoutines();

        _slideRoutine = StartCoroutine(SlideRoutine(false));
    }

    private void StopRunningRoutines()
    {
        if (_slideRoutine != null)
        {
            StopCoroutine(_slideRoutine);
            _slideRoutine = null;
        }

        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = null;
        }
    }

    private IEnumerator EventSequenceRoutine()
    {
        if (EventManager.Instance == null)
            yield break;

        List<RiskData> riskList = EventManager.Instance.GetRiskList();
        List<RewardData> rewardList = EventManager.Instance.GetRewardList();
        EventSelectionData selection = EventManager.Instance.CurrentEventSelection;

        if (!selection.IsValid())
            yield break;

        yield return StartCoroutine(RiskSequenceRoutine(riskList, selection));
        yield return new WaitForSecondsRealtime(delayBetweenRiskAndReward);
        yield return StartCoroutine(RewardSequenceRoutine(rewardList, selection));

        _sequenceRoutine = null;
    }

    private IEnumerator RiskSequenceRoutine(List<RiskData> riskList, EventSelectionData selection)
    {
        yield return StartCoroutine(CardMoveInRoutine(
            riskCard,
            riskCardGroup,
            _riskCardOriginalPos
        ));

        Coroutine titleFadeRoutine = null;

        if (riskTitleBoxGroup != null)
            titleFadeRoutine = StartCoroutine(FadeCanvas(riskTitleBoxGroup, boxFadeDuration));

        yield return StartCoroutine(RunRiskRoulette(riskList, selection));

        if (titleFadeRoutine != null)
            yield return titleFadeRoutine;

        if (riskBodyBoxGroup != null)
        {
            yield return new WaitForSecondsRealtime(innerDelay);
            yield return StartCoroutine(FadeCanvas(riskBodyBoxGroup, boxFadeDuration));
        }
    }

    private IEnumerator RewardSequenceRoutine(List<RewardData> rewardList, EventSelectionData selection)
    {
        yield return StartCoroutine(CardMoveInRoutine(
            rewardCard,
            rewardCardGroup,
            _rewardCardOriginalPos
        ));

        Coroutine titleFadeRoutine = null;

        if (rewardTitleBoxGroup != null)
            titleFadeRoutine = StartCoroutine(FadeCanvas(rewardTitleBoxGroup, boxFadeDuration));

        yield return StartCoroutine(RunRewardRoulette(rewardList, selection));

        if (titleFadeRoutine != null)
            yield return titleFadeRoutine;

        if (rewardBodyBoxGroup != null)
        {
            yield return new WaitForSecondsRealtime(innerDelay);
            yield return StartCoroutine(FadeCanvas(rewardBodyBoxGroup, boxFadeDuration));
        }
    }

    private IEnumerator CardMoveInRoutine(
        RectTransform card,
        CanvasGroup cardGroup,
        Vector2 originalPos)
    {
        if (card == null || cardGroup == null)
            yield break;

        Vector2 startPos = originalPos - new Vector2(0f, cardFloatY);
        Vector2 endPos = originalPos;

        card.anchoredPosition = startPos;
        card.localScale = Vector3.one * cardStartScale;
        cardGroup.alpha = 0f;

        float elapsed = 0f;

        while (elapsed < cardAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / cardAnimDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            card.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
            card.localScale = Vector3.Lerp(Vector3.one * cardStartScale, Vector3.one, eased);
            cardGroup.alpha = eased;

            yield return null;
        }

        card.anchoredPosition = endPos;
        card.localScale = Vector3.one;
        cardGroup.alpha = 1f;
    }

    private IEnumerator RunRiskRoulette(List<RiskData> riskList, EventSelectionData selection)
    {
        float elapsed = 0f;

        while (elapsed < rouletteDuration)
        {
            elapsed += rouletteInterval;

            if (riskList != null && riskList.Count > 0)
            {
                int i = Random.Range(0, riskList.Count);
                RiskData data = riskList[i];

                if (riskText != null)
                    riskText.text = data.riskName;

                if (riskDescriptionText != null)
                    riskDescriptionText.text = data.GetDescription(defaultDescriptionIndex);
            }

            yield return new WaitForSecondsRealtime(rouletteInterval);
        }

        if (selection.selectedRisk != null)
        {
            if (riskText != null)
                riskText.text = selection.selectedRisk.riskName;

            if (riskDescriptionText != null)
                riskDescriptionText.text = selection.selectedRisk.GetDescription(defaultDescriptionIndex);
        }
    }

    private IEnumerator RunRewardRoulette(List<RewardData> rewardList, EventSelectionData selection)
    {
        float elapsed = 0f;

        while (elapsed < rouletteDuration)
        {
            elapsed += rouletteInterval;

            if (rewardList != null && rewardList.Count > 0)
            {
                int i = Random.Range(0, rewardList.Count);
                RewardData data = rewardList[i];

                if (rewardText != null)
                    rewardText.text = data.rewardName;

                if (rewardDescriptionText != null)
                    rewardDescriptionText.text = data.GetDescription(defaultDescriptionIndex);
            }

            yield return new WaitForSecondsRealtime(rouletteInterval);
        }

        if (selection.selectedReward != null)
        {
            if (rewardText != null)
                rewardText.text = selection.selectedReward.rewardName;

            if (rewardDescriptionText != null)
                rewardDescriptionText.text = selection.selectedReward.GetDescription(defaultDescriptionIndex);
        }
    }

    private IEnumerator FadeCanvas(CanvasGroup cg, float duration)
    {
        if (cg == null)
            yield break;

        float elapsed = 0f;
        cg.alpha = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        cg.alpha = 1f;
    }

    private void ClearTexts()
    {
        if (riskText != null) riskText.text = string.Empty;
        if (rewardText != null) rewardText.text = string.Empty;
        if (riskDescriptionText != null) riskDescriptionText.text = string.Empty;
        if (rewardDescriptionText != null) rewardDescriptionText.text = string.Empty;
    }

    private void SetCardHidden(
        RectTransform card,
        CanvasGroup cardGroup,
        CanvasGroup titleBox,
        CanvasGroup bodyBox,
        Vector2 originalPos)
    {
        if (card != null)
        {
            card.anchoredPosition = originalPos;
            card.localScale = Vector3.one * cardStartScale;
        }

        if (cardGroup != null) cardGroup.alpha = 0f;
        if (titleBox != null) titleBox.alpha = 0f;
        if (bodyBox != null) bodyBox.alpha = 0f;
    }

    private IEnumerator SlideRoutine(bool isOpening)
    {
        float elapsed = 0f;
        float duration = isOpening ? openSlideDuration : closeSlideDuration;

        Vector2 startPos = isOpening ? _closedAnchoredPos : _openAnchoredPos;
        Vector2 endPos = isOpening ? _openAnchoredPos : _closedAnchoredPos;

        float startBackgroundAlpha = isOpening ? backgroundStartAlpha : (backgroundGroup != null ? backgroundGroup.alpha : 0f);
        float endBackgroundAlpha = isOpening ? backgroundEndAlpha : 0f;

        float startVisualAlpha = isOpening ? visualStartAlpha : (eventVisualGroup != null ? eventVisualGroup.alpha : 0f);
        float endVisualAlpha = isOpening ? visualEndAlpha : 0f;

        float startContentAlpha = isOpening ? contentStartAlpha : (eventContentGroup != null ? eventContentGroup.alpha : 0f);
        float endContentAlpha = isOpening ? contentEndAlpha : 0f;

        panelRect.anchoredPosition = startPos;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 4f);

            panelRect.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);

            if (backgroundGroup != null)
                backgroundGroup.alpha = Mathf.Lerp(startBackgroundAlpha, endBackgroundAlpha, eased);

            if (eventVisualGroup != null)
                eventVisualGroup.alpha = Mathf.Lerp(startVisualAlpha, endVisualAlpha, eased);

            if (eventContentGroup != null)
            {
                float contentT;

                if (isOpening)
                {
                    float fadeDuration = Mathf.Max(0.0001f, duration - contentFadeDelay);
                    contentT = Mathf.Clamp01((elapsed - contentFadeDelay) / fadeDuration);
                }
                else
                {
                    contentT = t;
                }

                float contentEased = 1f - Mathf.Pow(1f - contentT, 3f);
                eventContentGroup.alpha = Mathf.Lerp(startContentAlpha, endContentAlpha, contentEased);
            }

            yield return null;
        }

        panelRect.anchoredPosition = endPos;

        if (backgroundGroup != null)
            backgroundGroup.alpha = endBackgroundAlpha;

        if (eventVisualGroup != null)
            eventVisualGroup.alpha = endVisualAlpha;

        if (eventContentGroup != null)
            eventContentGroup.alpha = endContentAlpha;

        if (!isOpening)
        {
            SetCardHidden(
                riskCard,
                riskCardGroup,
                riskTitleBoxGroup,
                riskBodyBoxGroup,
                _riskCardOriginalPos
            );

            SetCardHidden(
                rewardCard,
                rewardCardGroup,
                rewardTitleBoxGroup,
                rewardBodyBoxGroup,
                _rewardCardOriginalPos
            );

            ClearTexts();
            eventRoot.SetActive(false);
        }

        _slideRoutine = null;
    }
}