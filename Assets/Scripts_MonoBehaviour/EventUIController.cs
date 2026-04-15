using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    [SerializeField] private float closeSlideDuration = 0.2f;

    [Header("Fade")]
    [SerializeField] private float backgroundStartAlpha = 0f;
    [SerializeField] private float backgroundEndAlpha = 1f;
    [SerializeField] private float visualStartAlpha = 0f;
    [SerializeField] private float visualEndAlpha = 1f;
    [SerializeField] private float contentStartAlpha = 0f;
    [SerializeField] private float contentEndAlpha = 0f;
    [SerializeField] private float contentFadeDelay = 0.08f;

    [Header("Roulette Text")]
    [SerializeField] private TextMeshProUGUI riskText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI riskDescriptionText;
    [SerializeField] private TextMeshProUGUI rewardDescriptionText;
    [SerializeField] private float rouletteDuration = 0.8f;
    [SerializeField] private float delayBetweenRiskAndReward = 0.18f;
    [SerializeField] private float rouletteStartInterval = 0.018f;
    [SerializeField] private float rouletteEndInterval = 0.18f;
    [SerializeField] private float textDefaultScale = 0.63f;

    [Header("Roulette Lock")]
    [SerializeField] private int heavySlowCount = 3;
    [SerializeField] private float heavySlowMultiplier = 1.65f;
    [SerializeField] private int finalPreviewCount = 2;
    [SerializeField] private float finalPreviewInterval = 0.1f;
    [SerializeField] private float finalLockFreeze = 0.12f;
    [SerializeField] private float finalImagePopScale = 1.12f;
    [SerializeField] private float finalImagePopDuration = 0.26f;
    [SerializeField] private float finalTextPopScale = 1.07f;
    [SerializeField] private float finalTextPopDuration = 0.22f;

    [Header("Card Image")]
    [SerializeField] private Image riskCardImage;
    [SerializeField] private Image rewardCardImage;

    [Header("Default Preview")]
    [SerializeField] private int defaultDescriptionIndex = 0;

    [Header("Card Animation")]
    [SerializeField] private RectTransform riskCard;
    [SerializeField] private RectTransform rewardCard;
    [SerializeField] private CanvasGroup riskCardGroup;
    [SerializeField] private CanvasGroup rewardCardGroup;
    [SerializeField] private Sprite defaultCardSprite;
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

    [Header("Destiny")]
    [SerializeField] private CanvasGroup destinyGroup;
    [SerializeField] private float destinyFadeDuration = 0.2f;
    [SerializeField] private float destinyHoverScale = 1.08f;
    [SerializeField] private float destinySelectedScale = 1.12f;
    [SerializeField] private float destinyScaleDuration = 0.14f;
    [SerializeField] private float destinyHoverLeaveDelay = 0.08f;
    [SerializeField] private Image[] destinyOptionImages;

    [Header("Destiny Sprites")]
    [SerializeField] private Sprite destiny0NormalSprite;
    [SerializeField] private Sprite destiny0SelectedSprite;
    [SerializeField] private Sprite destiny1NormalSprite;
    [SerializeField] private Sprite destiny1SelectedSprite;
    [SerializeField] private Sprite destiny2NormalSprite;
    [SerializeField] private Sprite destiny2SelectedSprite;

    [Header("Confirm Button")]
    [SerializeField] private Image confirmButtonImage;
    [SerializeField] private float confirmButtonDefaultScale = 0.4f;
    [SerializeField] private float confirmButtonHoverScale = 0.44f;
    [SerializeField] private float confirmButtonClickScale = 0.42f;
    [SerializeField] private float confirmButtonScaleDuration = 0.1f;
    [SerializeField] private float confirmCloseDelay = 0.02f;

    [Header("Description Emphasis")]
    [SerializeField] private float hoverDescriptionPopScale = 1.035f;
    [SerializeField] private float hoverDescriptionPopDuration = 0.12f;
    [SerializeField] private float clickDescriptionPopScale = 1.06f;
    [SerializeField] private float clickDescriptionPopDuration = 0.2f;

    [Header("Card Emphasis")]
    [SerializeField] private float hoverCardPopScale = 1.02f;
    [SerializeField] private float hoverCardPopDuration = 0.12f;
    [SerializeField] private float clickCardPopScale = 1.04f;
    [SerializeField] private float clickCardPopDuration = 0.2f;

    [Header("Idle Pulse")]
    [SerializeField] private bool enableIdlePulse = true;
    [SerializeField] private float idlePulseAmplitude = 0.018f;
    [SerializeField] private float idlePulseSpeed = 2.2f;
    [SerializeField] private float idleCardPulseAmplitude = 0.012f;
    [SerializeField] private float idleCardPulseSpeed = 1.8f;

    [Header("SFX")]
    [SerializeField] private AudioClip sfxRouletteTick;
    [SerializeField] private float sfxRouletteTickVolume = 0.9f;
    [SerializeField] private float sfxRouletteTickPitchVariation = 0.06f;
    [SerializeField] private AudioClip sfxRouletteLock;
    [SerializeField] private float sfxRouletteLockVolume = 1f;
    [SerializeField] private float sfxRouletteLockPitchVariation = 0.03f;
    [SerializeField] private AudioClip sfxDestinyClick;
    [SerializeField] private float sfxDestinyClickVolume = 0.6f;
    [SerializeField] private float sfxDestinyClickPitchVariation = 0.08f;
    [SerializeField] private AudioClip sfxSelect;
    [SerializeField] private float sfxSelectVolume = 1f;
    [SerializeField] private float sfxSelectPitchVariation = 0.04f;

    [Header("Optional")]
    [SerializeField] private bool closeWithEscape = true;

    private bool _isOpen;
    private bool canDestinyInteract;
    private bool canConfirmSelection;
    private bool _isClosingFromConfirm;
    private int currentDestinyIndex;
    private int _currentHoverIndex = -1;
    private Vector2 _openAnchoredPos;
    private Vector2 _closedAnchoredPos;
    private Vector2 _riskCardOriginalPos;
    private Vector2 _rewardCardOriginalPos;

    private Coroutine _slideRoutine;
    private Coroutine _sequenceRoutine;
    private Coroutine[] destinyScaleRoutines;
    private Coroutine _confirmButtonScaleRoutine;
    private Coroutine _riskDescriptionPopRoutine;
    private Coroutine _rewardDescriptionPopRoutine;
    private Coroutine _riskCardPopRoutine;
    private Coroutine _rewardCardPopRoutine;
    private Coroutine _destinyIdleRoutine;
    private Coroutine _riskIdlePulseRoutine;
    private Coroutine _rewardIdlePulseRoutine;
    private Coroutine _destinyHoverRestoreRoutine;
    private Coroutine _confirmButtonRoutine;

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

        SetCardHidden(riskCard, riskCardGroup, riskTitleBoxGroup, riskBodyBoxGroup, _riskCardOriginalPos);
        SetCardHidden(rewardCard, rewardCardGroup, rewardTitleBoxGroup, rewardBodyBoxGroup, _rewardCardOriginalPos);

        ClearTexts();
        InitializeDestinyUI();
        InitializeConfirmButtonUI();
        ResetConfirmButtonUI();
        eventRoot.SetActive(false);
    }

    private void OnDisable()
    {
        StopAllManagedCoroutines();
    }

    public void OpenEvent()
    {
        if (eventRoot == null || panelRect == null)
            return;

        if (EventManager.Instance == null || !EventManager.Instance.CurrentEventSelection.IsValid())
            return;
        
        if (InputStateManager.Instance != null && !InputStateManager.Instance.TryOpenUI())
            return;

        _isOpen = true;
        _isClosingFromConfirm = false;

        if (CameraFollow.instance != null)
            CameraFollow.instance.SetUIOffset(eventCameraOffset);

        eventRoot.SetActive(true);
        panelRect.anchoredPosition = _closedAnchoredPos;

        if (backgroundGroup != null) backgroundGroup.alpha = backgroundStartAlpha;
        if (eventVisualGroup != null) eventVisualGroup.alpha = visualStartAlpha;
        if (eventContentGroup != null) eventContentGroup.alpha = contentStartAlpha;

        SetCardHidden(riskCard, riskCardGroup, riskTitleBoxGroup, riskBodyBoxGroup, _riskCardOriginalPos);
        SetCardHidden(rewardCard, rewardCardGroup, rewardTitleBoxGroup, rewardBodyBoxGroup, _rewardCardOriginalPos);

        ClearTexts();
        ResetDestinyUI();
        ResetConfirmButtonUI();

        StopAllManagedCoroutines();

        _slideRoutine = StartCoroutine(SlideRoutine(true));
        _sequenceRoutine = StartCoroutine(EventSequenceRoutine());
    }

    public void CloseEvent()
    {
        if (!_isOpen)
            return;

        _isOpen = false;

        if (CameraFollow.instance)
            CameraFollow.instance.ResetUIOffset();

        StopAllManagedCoroutines();
        _slideRoutine = StartCoroutine(SlideRoutine(false));
    }

    private void StopAllManagedCoroutines()
    {
        StopCoroutineSafe(ref _slideRoutine);
        StopCoroutineSafe(ref _sequenceRoutine);
        StopCoroutineSafe(ref _confirmButtonScaleRoutine);
        StopCoroutineSafe(ref _riskDescriptionPopRoutine);
        StopCoroutineSafe(ref _rewardDescriptionPopRoutine);
        StopCoroutineSafe(ref _riskCardPopRoutine);
        StopCoroutineSafe(ref _rewardCardPopRoutine);
        StopCoroutineSafe(ref _destinyIdleRoutine);
        StopCoroutineSafe(ref _riskIdlePulseRoutine);
        StopCoroutineSafe(ref _rewardIdlePulseRoutine);
        StopCoroutineSafe(ref _destinyHoverRestoreRoutine);
        StopCoroutineSafe(ref _confirmButtonRoutine);

        if (destinyScaleRoutines != null)
        {
            for (int i = 0; i < destinyScaleRoutines.Length; i++)
                StopCoroutineSafe(ref destinyScaleRoutines[i]);
        }
    }

    private void StopCoroutineSafe(ref Coroutine routine)
    {
        if (routine == null)
            return;

        StopCoroutine(routine);
        routine = null;
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

        if (destinyGroup != null)
            yield return StartCoroutine(FadeCanvas(destinyGroup, destinyFadeDuration));

        canDestinyInteract = true;
        canConfirmSelection = true;
        SetDestinyInteractable(true);
        SetConfirmButtonInteractable(true);

        ApplyDestinyDescription(currentDestinyIndex, false);
        RefreshDestinyButtonVisual();
        ApplyDestinySelectionScale(false);

        if (confirmButtonImage != null)
            confirmButtonImage.rectTransform.localScale = Vector3.one * confirmButtonDefaultScale;

        StartIdleEffects();

        _sequenceRoutine = null;
    }

    private IEnumerator RiskSequenceRoutine(List<RiskData> riskList, EventSelectionData selection)
    {
        yield return StartCoroutine(CardMoveInRoutine(riskCard, riskCardGroup, _riskCardOriginalPos));

        Coroutine titleFadeRoutine = null;
        Coroutine bodyFadeRoutine = null;
        Coroutine rouletteRoutine = null;

        if (riskTitleBoxGroup != null)
            titleFadeRoutine = StartCoroutine(FadeCanvas(riskTitleBoxGroup, boxFadeDuration));

        rouletteRoutine = StartCoroutine(RunRiskRoulette(riskList, selection));

        if (riskBodyBoxGroup != null)
            bodyFadeRoutine = StartCoroutine(FadeCanvasDelayed(riskBodyBoxGroup, innerDelay, boxFadeDuration));

        if (titleFadeRoutine != null)
            yield return titleFadeRoutine;

        if (bodyFadeRoutine != null)
            yield return bodyFadeRoutine;

        if (rouletteRoutine != null)
            yield return rouletteRoutine;
    }

    private IEnumerator RewardSequenceRoutine(List<RewardData> rewardList, EventSelectionData selection)
    {
        yield return StartCoroutine(CardMoveInRoutine(rewardCard, rewardCardGroup, _rewardCardOriginalPos));

        Coroutine titleFadeRoutine = null;
        Coroutine bodyFadeRoutine = null;
        Coroutine rouletteRoutine = null;

        if (rewardTitleBoxGroup != null)
            titleFadeRoutine = StartCoroutine(FadeCanvas(rewardTitleBoxGroup, boxFadeDuration));

        rouletteRoutine = StartCoroutine(RunRewardRoulette(rewardList, selection));

        if (rewardBodyBoxGroup != null)
            bodyFadeRoutine = StartCoroutine(FadeCanvasDelayed(rewardBodyBoxGroup, innerDelay, boxFadeDuration));

        if (titleFadeRoutine != null)
            yield return titleFadeRoutine;

        if (bodyFadeRoutine != null)
            yield return bodyFadeRoutine;

        if (rouletteRoutine != null)
            yield return rouletteRoutine;
    }

    private IEnumerator CardMoveInRoutine(RectTransform card, CanvasGroup cardGroup, Vector2 originalPos)
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
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, cardAnimDuration));
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
        yield return RunRoulette(
            riskList,
            rouletteDuration,
            data =>
            {
                if (riskText != null)
                    riskText.text = data.riskName;

                if (riskDescriptionText != null)
                    riskDescriptionText.text = data.GetDescription(defaultDescriptionIndex);

                if (riskCardImage != null)
                    riskCardImage.sprite = data.symbolSprite;
            },
            () => selection.selectedRisk,
            data =>
            {
                if (riskText != null)
                    riskText.text = data.riskName;

                if (riskDescriptionText != null)
                    riskDescriptionText.text = data.GetDescription(defaultDescriptionIndex);

                if (riskCardImage != null)
                    riskCardImage.sprite = data.symbolSprite;
            },
            riskCardImage != null ? riskCardImage.rectTransform : null,
            riskText != null ? riskText.rectTransform : null,
            riskDescriptionText != null ? riskDescriptionText.rectTransform : null
        );
    }

    private IEnumerator RunRewardRoulette(List<RewardData> rewardList, EventSelectionData selection)
    {
        yield return RunRoulette(
            rewardList,
            rouletteDuration,
            data =>
            {
                if (rewardText != null)
                    rewardText.text = data.rewardName;

                if (rewardDescriptionText != null)
                    rewardDescriptionText.text = data.GetDescription(defaultDescriptionIndex);

                if (rewardCardImage != null)
                    rewardCardImage.sprite = data.symbolSprite;
            },
            () => selection.selectedReward,
            data =>
            {
                if (rewardText != null)
                    rewardText.text = data.rewardName;

                if (rewardDescriptionText != null)
                    rewardDescriptionText.text = data.GetDescription(defaultDescriptionIndex);

                if (rewardCardImage != null)
                    rewardCardImage.sprite = data.symbolSprite;
            },
            rewardCardImage != null ? rewardCardImage.rectTransform : null,
            rewardText != null ? rewardText.rectTransform : null,
            rewardDescriptionText != null ? rewardDescriptionText.rectTransform : null
        );
    }

    private IEnumerator RunRoulette<T>(
        List<T> dataList,
        float duration,
        System.Action<T> applyPreview,
        System.Func<T> getFinalData,
        System.Action<T> applyFinal,
        RectTransform finalImageTarget,
        RectTransform finalTitleTarget,
        RectTransform finalDescriptionTarget)
    {
        if (dataList == null || dataList.Count == 0)
            yield break;

        PlaySFX(sfxRouletteTick, sfxRouletteTickVolume, sfxRouletteTickPitchVariation);

        if (dataList.Count == 1)
        {
            T onlyData = dataList[0];
            applyPreview?.Invoke(onlyData);

            yield return new WaitForSecondsRealtime(duration);
            yield return new WaitForSecondsRealtime(finalLockFreeze);

            T finalSingle = getFinalData != null ? getFinalData() : onlyData;
            applyFinal?.Invoke(finalSingle);
            PlaySFX(sfxRouletteLock, sfxRouletteLockVolume, sfxRouletteLockPitchVariation);

            yield return StartCoroutine(PlayRouletteLockFeedback(finalImageTarget, finalTitleTarget, finalDescriptionTarget));
            yield break;
        }

        int previousIndex = -1;
        List<float> intervals = BuildRouletteIntervals(duration);

        for (int i = 0; i < intervals.Count; i++)
        {
            int index = GetNextRouletteIndex(dataList.Count, previousIndex);
            previousIndex = index;

            T data = dataList[index];
            applyPreview?.Invoke(data);

            yield return new WaitForSecondsRealtime(intervals[i]);
        }

        T finalData = getFinalData != null ? getFinalData() : default;

        if (finalData != null)
        {
            for (int i = 0; i < finalPreviewCount; i++)
            {
                int index = GetNextRouletteIndex(dataList.Count, previousIndex);
                previousIndex = index;

                T previewData = dataList[index];
                applyPreview?.Invoke(previewData);

                yield return new WaitForSecondsRealtime(finalPreviewInterval);
            }

            yield return new WaitForSecondsRealtime(finalLockFreeze);

            applyFinal?.Invoke(finalData);
            PlaySFX(sfxRouletteLock, sfxRouletteLockVolume, sfxRouletteLockPitchVariation);

            yield return StartCoroutine(PlayRouletteLockFeedback(finalImageTarget, finalTitleTarget, finalDescriptionTarget));
        }
    }

    private IEnumerator PlayRouletteLockFeedback(RectTransform imageTarget, RectTransform titleTarget, RectTransform descriptionTarget)
    {
        Coroutine imageRoutine = null;
        Coroutine titleRoutine = null;
        Coroutine descriptionRoutine = null;

        if (imageTarget != null)
            imageRoutine = StartCoroutine(PopScaleRoutine(imageTarget, finalImagePopScale, finalImagePopDuration, 1f));

        if (titleTarget != null)
            titleRoutine = StartCoroutine(PopScaleRoutine(titleTarget, finalTextPopScale, finalTextPopDuration, textDefaultScale));

        if (descriptionTarget != null)
            descriptionRoutine = StartCoroutine(PopScaleRoutine(descriptionTarget, 1.035f, finalTextPopDuration, textDefaultScale));

        if (imageRoutine != null)
            yield return imageRoutine;

        if (titleRoutine != null)
            yield return titleRoutine;

        if (descriptionRoutine != null)
            yield return descriptionRoutine;
    }

    private List<float> BuildRouletteIntervals(float duration)
    {
        List<float> intervals = new List<float>();
        float total = 0f;
        int guard = 0;

        while (total < duration && guard < 1000)
        {
            float progress = duration <= 0f ? 1f : Mathf.Clamp01(total / duration);
            float eased = Mathf.Pow(progress, 2.4f);
            float interval = Mathf.Lerp(rouletteStartInterval, rouletteEndInterval, eased);

            intervals.Add(interval);
            total += interval;
            guard++;
        }

        if (intervals.Count == 0)
            intervals.Add(rouletteStartInterval);

        int count = Mathf.Min(heavySlowCount, intervals.Count);

        for (int i = intervals.Count - count; i < intervals.Count; i++)
        {
            if (i >= 0)
                intervals[i] *= heavySlowMultiplier;
        }

        return intervals;
    }

    private int GetNextRouletteIndex(int count, int previousIndex)
    {
        if (count <= 1)
            return 0;

        int index = Random.Range(0, count - 1);

        if (previousIndex >= 0 && index >= previousIndex)
            index++;

        return index;
    }

    private IEnumerator PopScaleRoutine(RectTransform target, float popScale, float duration, float baseScale)
    {
        if (target == null)
            yield break;

        Vector3 baseVector = Vector3.one * baseScale;
        Vector3 peakScale = Vector3.one * (baseScale * popScale);

        float halfDuration = duration * 0.5f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, halfDuration));
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            target.localScale = Vector3.Lerp(baseVector, peakScale, eased);
            yield return null;
        }

        target.localScale = peakScale;
        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, halfDuration));
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            target.localScale = Vector3.Lerp(peakScale, baseVector, eased);
            yield return null;
        }

        target.localScale = baseVector;
    }

    private IEnumerator ScaleToRoutine(RectTransform target, float targetScale, float duration)
    {
        if (target == null)
            yield break;

        Vector3 start = target.localScale;
        Vector3 end = Vector3.one * targetScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            target.localScale = Vector3.Lerp(start, end, eased);
            yield return null;
        }

        target.localScale = end;
    }

    private IEnumerator IdlePulseRoutine(RectTransform target, float baseScale, float amplitude, float speed)
    {
        if (target == null)
            yield break;

        while (true)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * speed) * amplitude;
            target.localScale = Vector3.one * (baseScale * pulse);
            yield return null;
        }
    }

    private IEnumerator RestoreDestinyDescriptionDelayed()
    {
        yield return new WaitForSecondsRealtime(destinyHoverLeaveDelay);

        if (_currentHoverIndex != -1)
        {
            _destinyHoverRestoreRoutine = null;
            yield break;
        }

        ApplyDestinyDescription(currentDestinyIndex, false);
        _destinyHoverRestoreRoutine = null;
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
            cg.alpha = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
            yield return null;
        }

        cg.alpha = 1f;
    }

    private IEnumerator FadeCanvasDelayed(CanvasGroup cg, float delay, float duration)
    {
        if (cg == null)
            yield break;

        yield return new WaitForSecondsRealtime(delay);
        yield return FadeCanvas(cg, duration);
    }

    private void ClearTexts()
    {
        if (riskText != null) riskText.text = string.Empty;
        if (rewardText != null) rewardText.text = string.Empty;
        if (riskDescriptionText != null) riskDescriptionText.text = string.Empty;
        if (rewardDescriptionText != null) rewardDescriptionText.text = string.Empty;

        if (riskText != null)
            riskText.rectTransform.localScale = Vector3.one * textDefaultScale;

        if (rewardText != null)
            rewardText.rectTransform.localScale = Vector3.one * textDefaultScale;

        if (riskDescriptionText != null)
            riskDescriptionText.rectTransform.localScale = Vector3.one * textDefaultScale;

        if (rewardDescriptionText != null)
            rewardDescriptionText.rectTransform.localScale = Vector3.one * textDefaultScale;

        if (riskCardImage != null)
        {
            riskCardImage.sprite = defaultCardSprite;
            riskCardImage.rectTransform.localScale = Vector3.one;
        }

        if (rewardCardImage != null)
        {
            rewardCardImage.sprite = defaultCardSprite;
            rewardCardImage.rectTransform.localScale = Vector3.one;
        }
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
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
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
            SetCardHidden(riskCard, riskCardGroup, riskTitleBoxGroup, riskBodyBoxGroup, _riskCardOriginalPos);
            SetCardHidden(rewardCard, rewardCardGroup, rewardTitleBoxGroup, rewardBodyBoxGroup, _rewardCardOriginalPos);
            ClearTexts();
            ResetDestinyUI();
            ResetConfirmButtonUI();
            StopIdleEffects();
            
            eventRoot.SetActive(false);
            _isClosingFromConfirm = false;
            
            if (InputStateManager.Instance != null)
                InputStateManager.Instance.CloseUI();

            eventRoot.SetActive(false);
            _isClosingFromConfirm = false;
        }

        _slideRoutine = null;
    }

    private void InitializeDestinyUI()
    {
        if (destinyOptionImages == null || destinyOptionImages.Length == 0)
            return;

        destinyScaleRoutines = new Coroutine[destinyOptionImages.Length];

        for (int i = 0; i < destinyOptionImages.Length; i++)
        {
            int index = i;

            if (destinyOptionImages[i] == null)
                continue;

            EventTrigger trigger = destinyOptionImages[i].GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = destinyOptionImages[i].gameObject.AddComponent<EventTrigger>();

            if (trigger.triggers == null)
                trigger.triggers = new List<EventTrigger.Entry>();
            else
                trigger.triggers.Clear();

            AddEventTrigger(trigger, EventTriggerType.PointerClick, () =>
            {
                OnClickDestinyOption(index);
            });

            AddEventTrigger(trigger, EventTriggerType.PointerEnter, () =>
            {
                if (!canDestinyInteract)
                    return;

                OnEnterDestinyOption(index);
            });

            AddEventTrigger(trigger, EventTriggerType.PointerExit, () =>
            {
                if (!canDestinyInteract)
                    return;

                OnExitDestinyOption(index);
            });
        }

        ResetDestinyUI();
    }

    private void InitializeConfirmButtonUI()
    {
        if (confirmButtonImage == null)
            return;

        EventTrigger trigger = confirmButtonImage.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = confirmButtonImage.gameObject.AddComponent<EventTrigger>();

        if (trigger.triggers == null)
            trigger.triggers = new List<EventTrigger.Entry>();
        else
            trigger.triggers.Clear();

        AddEventTrigger(trigger, EventTriggerType.PointerEnter, () =>
        {
            if (!canConfirmSelection || _isClosingFromConfirm)
                return;

            OnEnterConfirmButton();
        });

        AddEventTrigger(trigger, EventTriggerType.PointerExit, () =>
        {
            if (!canConfirmSelection || _isClosingFromConfirm)
                return;

            OnExitConfirmButton();
        });

        AddEventTrigger(trigger, EventTriggerType.PointerClick, () =>
        {
            if (!canConfirmSelection || _isClosingFromConfirm)
                return;

            OnClickConfirmButton();
        });
    }

    private void ResetDestinyUI()
    {
        canDestinyInteract = false;
        currentDestinyIndex = 0;
        _currentHoverIndex = -1;

        if (destinyGroup != null)
        {
            destinyGroup.alpha = 0f;
            destinyGroup.interactable = false;
            destinyGroup.blocksRaycasts = false;
        }

        SetDestinyInteractable(false);
        StopIdleEffects();
        StopCoroutineSafe(ref _destinyHoverRestoreRoutine);

        if (destinyOptionImages != null)
        {
            for (int i = 0; i < destinyOptionImages.Length; i++)
            {
                if (destinyOptionImages[i] != null)
                    destinyOptionImages[i].rectTransform.localScale = Vector3.one;
            }
        }

        RefreshDestinyButtonVisual();
    }

    private void ResetConfirmButtonUI()
    {
        canConfirmSelection = false;

        if (confirmButtonImage != null)
        {
            confirmButtonImage.rectTransform.localScale = Vector3.one * confirmButtonDefaultScale;
            confirmButtonImage.raycastTarget = false;
        }
    }

    private void SetDestinyInteractable(bool value)
    {
        if (destinyGroup != null)
        {
            destinyGroup.interactable = value;
            destinyGroup.blocksRaycasts = value;
        }

        if (destinyOptionImages == null)
            return;

        for (int i = 0; i < destinyOptionImages.Length; i++)
        {
            if (destinyOptionImages[i] != null)
                destinyOptionImages[i].raycastTarget = value;
        }
    }

    private void SetConfirmButtonInteractable(bool value)
    {
        if (confirmButtonImage != null)
            confirmButtonImage.raycastTarget = value;
    }

    private void OnEnterDestinyOption(int index)
    {
        _currentHoverIndex = index;
        StopCoroutineSafe(ref _destinyHoverRestoreRoutine);

        PlayDestinyHoverScale(index, destinyHoverScale);
        ApplyDestinyDescription(index, true, true);
    }

    private void OnExitDestinyOption(int index)
    {
        if (_currentHoverIndex == index)
            _currentHoverIndex = -1;

        float targetScale = currentDestinyIndex == index ? destinySelectedScale : 1f;
        PlayDestinyHoverScale(index, targetScale);

        StopCoroutineSafe(ref _destinyHoverRestoreRoutine);
        _destinyHoverRestoreRoutine = StartCoroutine(RestoreDestinyDescriptionDelayed());
    }

    private void OnClickDestinyOption(int index)
    {
        if (!canDestinyInteract)
            return;

        currentDestinyIndex = index;
        RefreshDestinyButtonVisual();
        ApplyDestinyDescription(index, true, false);
        PlaySFX(sfxDestinyClick, sfxDestinyClickVolume, sfxDestinyClickPitchVariation);
        ApplyDestinySelectionScale(true);
        RestartIdleEffects();

        if (confirmButtonImage != null)
            confirmButtonImage.rectTransform.localScale = Vector3.one * confirmButtonDefaultScale;
    }

    private void OnEnterConfirmButton()
    {
        PlayConfirmButtonScale(confirmButtonHoverScale);
    }

    private void OnExitConfirmButton()
    {
        PlayConfirmButtonScale(confirmButtonDefaultScale);
    }

    private void OnClickConfirmButton()
    {
        if (!canConfirmSelection || _isClosingFromConfirm)
            return;

        _isClosingFromConfirm = true;
        canConfirmSelection = false;
        canDestinyInteract = false;
        SetConfirmButtonInteractable(false);
        SetDestinyInteractable(false);
        StopIdleEffects();

        string riskName = riskText != null ? riskText.text : "Unknown Risk";
        string rewardName = rewardText != null ? rewardText.text : "Unknown Reward";

        Debug.Log("[EventUI] Selection confirmed. DestinyIndex: " + currentDestinyIndex +
                  ", Risk: " + riskName +
                  ", Reward: " + rewardName);

        PlaySFX(sfxSelect, sfxSelectVolume, sfxSelectPitchVariation);

        if (_confirmButtonRoutine != null)
            StopCoroutineSafe(ref _confirmButtonRoutine);

        _confirmButtonRoutine = StartCoroutine(ConfirmAndCloseRoutine());
    }

    private IEnumerator ConfirmAndCloseRoutine()
    {
        if (confirmButtonImage != null)
        {
            RectTransform target = confirmButtonImage.rectTransform;
            yield return StartCoroutine(ScaleToRoutine(target, confirmButtonClickScale, 0.05f));
        }

        if (confirmCloseDelay > 0f)
            yield return new WaitForSecondsRealtime(confirmCloseDelay);

        CloseEvent();
        _confirmButtonRoutine = null;
    }

    private void RefreshDestinyButtonVisual()
    {
        if (destinyOptionImages == null || destinyOptionImages.Length == 0)
            return;

        for (int i = 0; i < destinyOptionImages.Length; i++)
        {
            if (destinyOptionImages[i] == null)
                continue;

            destinyOptionImages[i].sprite = GetDestinySprite(i, i == currentDestinyIndex);
        }
    }

    private void ApplyDestinySelectionScale(bool animateSelected)
    {
        if (destinyOptionImages == null)
            return;

        for (int i = 0; i < destinyOptionImages.Length; i++)
        {
            if (destinyOptionImages[i] == null)
                continue;

            float targetScale = i == currentDestinyIndex ? destinySelectedScale : 1f;
            PlayDestinyHoverScale(i, targetScale);

            if (animateSelected && i == currentDestinyIndex)
            {
                RectTransform rt = destinyOptionImages[i].rectTransform;

                if (destinyScaleRoutines != null && destinyScaleRoutines[i] != null)
                    StopCoroutineSafe(ref destinyScaleRoutines[i]);

                destinyScaleRoutines[i] = StartCoroutine(SelectedButtonClickRoutine(rt));
            }
        }
    }

    private IEnumerator SelectedButtonClickRoutine(RectTransform target)
    {
        if (target == null)
            yield break;

        float overshoot = destinySelectedScale * 1.03f;

        yield return StartCoroutine(ScaleToRoutine(target, overshoot, 0.08f));
        yield return StartCoroutine(ScaleToRoutine(target, destinySelectedScale, 0.12f));
    }

    private Sprite GetDestinySprite(int index, bool selected)
    {
        switch (index)
        {
            case 0:
                return selected ? destiny0SelectedSprite : destiny0NormalSprite;
            case 1:
                return selected ? destiny1SelectedSprite : destiny1NormalSprite;
            case 2:
                return selected ? destiny2SelectedSprite : destiny2NormalSprite;
            default:
                return null;
        }
    }

    private void ApplyDestinyDescription(int descriptionIndex, bool playEmphasis)
    {
        ApplyDestinyDescription(descriptionIndex, playEmphasis, false);
    }

    private void ApplyDestinyDescription(int descriptionIndex, bool playEmphasis, bool hoverMode)
    {
        EventSelectionData selection = EventManager.Instance != null
            ? EventManager.Instance.CurrentEventSelection
            : null;

        if (selection == null || !selection.IsValid())
            return;

        if (selection.selectedRisk != null && riskDescriptionText != null)
        {
            riskDescriptionText.text = selection.selectedRisk.GetDescription(descriptionIndex);

            if (playEmphasis)
                PlayDescriptionPop(riskDescriptionText, true, hoverMode);
        }

        if (selection.selectedReward != null && rewardDescriptionText != null)
        {
            rewardDescriptionText.text = selection.selectedReward.GetDescription(descriptionIndex);

            if (playEmphasis)
                PlayDescriptionPop(rewardDescriptionText, false, hoverMode);
        }

        if (playEmphasis)
            PlayCardImagePop(hoverMode);
    }

    private void PlayDescriptionPop(TextMeshProUGUI targetText, bool isRisk, bool hoverMode)
    {
        if (targetText == null)
            return;

        float popScale = hoverMode ? hoverDescriptionPopScale : clickDescriptionPopScale;
        float popDuration = hoverMode ? hoverDescriptionPopDuration : clickDescriptionPopDuration;

        if (isRisk)
        {
            StopCoroutineSafe(ref _riskDescriptionPopRoutine);
            _riskDescriptionPopRoutine = StartCoroutine(
                PopScaleRoutine(targetText.rectTransform, popScale, popDuration, textDefaultScale)
            );
        }
        else
        {
            StopCoroutineSafe(ref _rewardDescriptionPopRoutine);
            _rewardDescriptionPopRoutine = StartCoroutine(
                PopScaleRoutine(targetText.rectTransform, popScale, popDuration, textDefaultScale)
            );
        }
    }

    private void PlayCardImagePop(bool hoverMode)
    {
        float popScale = hoverMode ? hoverCardPopScale : clickCardPopScale;
        float popDuration = hoverMode ? hoverCardPopDuration : clickCardPopDuration;

        if (riskCardImage != null)
        {
            StopCoroutineSafe(ref _riskCardPopRoutine);
            _riskCardPopRoutine = StartCoroutine(
                PopScaleRoutine(riskCardImage.rectTransform, popScale, popDuration, 1f)
            );
        }

        if (rewardCardImage != null)
        {
            StopCoroutineSafe(ref _rewardCardPopRoutine);
            _rewardCardPopRoutine = StartCoroutine(
                PopScaleRoutine(rewardCardImage.rectTransform, popScale, popDuration, 1f)
            );
        }
    }

    private void AddEventTrigger(EventTrigger trigger, EventTriggerType type, System.Action action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(_ => action?.Invoke());
        trigger.triggers.Add(entry);
    }

    private void PlayDestinyHoverScale(int index, float targetScale)
    {
        if (destinyOptionImages == null || index < 0 || index >= destinyOptionImages.Length)
            return;

        if (destinyOptionImages[index] == null)
            return;

        if (destinyScaleRoutines != null && destinyScaleRoutines[index] != null)
            StopCoroutineSafe(ref destinyScaleRoutines[index]);

        destinyScaleRoutines[index] = StartCoroutine(
            ScaleDestinyButtonRoutine(destinyOptionImages[index].rectTransform, targetScale)
        );
    }

    private void PlayConfirmButtonScale(float targetScale)
    {
        if (confirmButtonImage == null)
            return;

        StopCoroutineSafe(ref _confirmButtonScaleRoutine);
        _confirmButtonScaleRoutine = StartCoroutine(
            ScaleConfirmButtonRoutine(confirmButtonImage.rectTransform, targetScale)
        );
    }

    private IEnumerator ScaleDestinyButtonRoutine(RectTransform target, float targetScale)
    {
        if (target == null)
            yield break;

        Vector3 start = target.localScale;
        Vector3 end = Vector3.one * targetScale;
        float elapsed = 0f;

        while (elapsed < destinyScaleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, destinyScaleDuration));
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            target.localScale = Vector3.Lerp(start, end, eased);
            yield return null;
        }

        target.localScale = end;
    }

    private IEnumerator ScaleConfirmButtonRoutine(RectTransform target, float targetScale)
    {
        if (target == null)
            yield break;

        Vector3 start = target.localScale;
        Vector3 end = Vector3.one * targetScale;
        float elapsed = 0f;

        while (elapsed < confirmButtonScaleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, confirmButtonScaleDuration));
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            target.localScale = Vector3.Lerp(start, end, eased);
            yield return null;
        }

        target.localScale = end;
        _confirmButtonScaleRoutine = null;
    }

    private void StartIdleEffects()
    {
        if (!enableIdlePulse || !canDestinyInteract)
            return;

        RestartIdleEffects();
    }

    private void RestartIdleEffects()
    {
        StopIdleEffects();

        if (!enableIdlePulse || !canDestinyInteract)
            return;

        if (destinyOptionImages != null &&
            currentDestinyIndex >= 0 &&
            currentDestinyIndex < destinyOptionImages.Length &&
            destinyOptionImages[currentDestinyIndex] != null)
        {
            _destinyIdleRoutine = StartCoroutine(
                IdlePulseRoutine(
                    destinyOptionImages[currentDestinyIndex].rectTransform,
                    destinySelectedScale,
                    idlePulseAmplitude,
                    idlePulseSpeed
                )
            );
        }

        if (riskCardImage != null)
        {
            _riskIdlePulseRoutine = StartCoroutine(
                IdlePulseRoutine(
                    riskCardImage.rectTransform,
                    1f,
                    idleCardPulseAmplitude,
                    idleCardPulseSpeed
                )
            );
        }

        if (rewardCardImage != null)
        {
            _rewardIdlePulseRoutine = StartCoroutine(
                IdlePulseRoutine(
                    rewardCardImage.rectTransform,
                    1f,
                    idleCardPulseAmplitude,
                    idleCardPulseSpeed
                )
            );
        }
    }

    private void StopIdleEffects()
    {
        StopCoroutineSafe(ref _destinyIdleRoutine);
        StopCoroutineSafe(ref _riskIdlePulseRoutine);
        StopCoroutineSafe(ref _rewardIdlePulseRoutine);

        if (destinyOptionImages != null &&
            currentDestinyIndex >= 0 &&
            currentDestinyIndex < destinyOptionImages.Length &&
            destinyOptionImages[currentDestinyIndex] != null)
        {
            destinyOptionImages[currentDestinyIndex].rectTransform.localScale = Vector3.one * destinySelectedScale;
        }

        if (riskCardImage != null)
            riskCardImage.rectTransform.localScale = Vector3.one;

        if (rewardCardImage != null)
            rewardCardImage.rectTransform.localScale = Vector3.one;
    }

    private void PlaySFX(AudioClip clip, float volumeScale, float pitchVariation)
    {
        if (clip != null && SoundManager.instance != null)
            SoundManager.instance.PlaySFX(clip, volumeScale, pitchVariation);
    }
}