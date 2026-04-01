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
    [SerializeField] private float rouletteDuration = 0.7f;
    [SerializeField] private float delayBetweenRiskAndReward = 0.35f;
    [SerializeField] private float rouletteStartInterval = 0.03f;
    [SerializeField] private float rouletteEndInterval = 0.12f;
    [SerializeField] private float textDefaultScale = 0.63f;

    [Header("Roulette Impact")]
    [SerializeField] private int heavySlowCount = 3;
    [SerializeField] private float heavySlowMultiplier = 1.5f;
    [SerializeField] private float finalLockFreeze = 0.05f;
    [SerializeField] private float finalImagePopScale = 1.1f;
    [SerializeField] private float finalImagePopDuration = 0.12f;

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
    [SerializeField] private float destinyHoverScale = 1.05f;
    [SerializeField] private float destinyScaleDuration = 0.08f;
    [SerializeField] private Image[] destinyOptionImages;

    [Header("Destiny Sprites")]
    [SerializeField] private Sprite destiny0NormalSprite;
    [SerializeField] private Sprite destiny0SelectedSprite;
    [SerializeField] private Sprite destiny1NormalSprite;
    [SerializeField] private Sprite destiny1SelectedSprite;
    [SerializeField] private Sprite destiny2NormalSprite;
    [SerializeField] private Sprite destiny2SelectedSprite;

    [Header("Text Emphasis")]
    [SerializeField] private float descriptionPopScale = 1.06f;
    [SerializeField] private float descriptionPopDuration = 0.12f;

    [Header("Card Emphasis")]
    [SerializeField] private float destinyCardPopScale = 1.03f;
    [SerializeField] private float destinyCardPopDuration = 0.1f;

    [Header("SFX")]
    [SerializeField] private AudioClip sfxRouletteTick;
    [SerializeField] private float sfxRouletteTickVolume = 0.9f;
    [SerializeField] private float sfxRouletteTickPitchVariation = 0.06f;
    [SerializeField] private AudioClip sfxRouletteLock;
    [SerializeField] private float sfxRouletteLockVolume = 1f;
    [SerializeField] private float sfxRouletteLockPitchVariation = 0.03f;
    [SerializeField] private AudioClip sfxDestinyClick;
    [SerializeField] private float sfxDestinyClickVolume = 0.95f;
    [SerializeField] private float sfxDestinyClickPitchVariation = 0.05f;

    [Header("Optional")]
    [SerializeField] private bool closeWithEscape = true;

    private bool _isOpen;
    private bool canDestinyInteract;
    private int currentDestinyIndex;
    private Vector2 _openAnchoredPos;
    private Vector2 _closedAnchoredPos;
    private Vector2 _riskCardOriginalPos;
    private Vector2 _rewardCardOriginalPos;

    private Coroutine _slideRoutine;
    private Coroutine _sequenceRoutine;
    private Coroutine[] destinyScaleRoutines;
    private Coroutine _riskDescriptionPopRoutine;
    private Coroutine _rewardDescriptionPopRoutine;
    private Coroutine _riskCardPopRoutine;
    private Coroutine _rewardCardPopRoutine;

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

        SetCardHidden(riskCard, riskCardGroup, riskTitleBoxGroup, riskBodyBoxGroup, _riskCardOriginalPos);
        SetCardHidden(rewardCard, rewardCardGroup, rewardTitleBoxGroup, rewardBodyBoxGroup, _rewardCardOriginalPos);

        ClearTexts();
        ResetDestinyUI();

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

        if (_riskDescriptionPopRoutine != null)
        {
            StopCoroutine(_riskDescriptionPopRoutine);
            _riskDescriptionPopRoutine = null;
        }

        if (_rewardDescriptionPopRoutine != null)
        {
            StopCoroutine(_rewardDescriptionPopRoutine);
            _rewardDescriptionPopRoutine = null;
        }

        if (_riskCardPopRoutine != null)
        {
            StopCoroutine(_riskCardPopRoutine);
            _riskCardPopRoutine = null;
        }

        if (_rewardCardPopRoutine != null)
        {
            StopCoroutine(_rewardCardPopRoutine);
            _rewardCardPopRoutine = null;
        }

        if (destinyScaleRoutines != null)
        {
            for (int i = 0; i < destinyScaleRoutines.Length; i++)
            {
                if (destinyScaleRoutines[i] != null)
                {
                    StopCoroutine(destinyScaleRoutines[i]);
                    destinyScaleRoutines[i] = null;
                }
            }
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

        if (destinyGroup != null)
            yield return StartCoroutine(FadeCanvas(destinyGroup, destinyFadeDuration));

        canDestinyInteract = true;
        SetDestinyInteractable(true);
        ApplyDestinyDescription(currentDestinyIndex, true);

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
            riskCardImage != null ? riskCardImage.rectTransform : null
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
            rewardCardImage != null ? rewardCardImage.rectTransform : null
        );
    }

    private IEnumerator RunRoulette<T>(
        List<T> dataList,
        float duration,
        System.Action<T> applyPreview,
        System.Func<T> getFinalData,
        System.Action<T> applyFinal,
        RectTransform finalImageTarget)
    {
        if (dataList == null || dataList.Count == 0)
            yield break;

        PlaySFX(sfxRouletteTick, sfxRouletteTickVolume, sfxRouletteTickPitchVariation);

        if (dataList.Count == 1)
        {
            T onlyData = dataList[0];
            applyPreview?.Invoke(onlyData);

            yield return new WaitForSecondsRealtime(duration);

            T finalSingle = getFinalData != null ? getFinalData() : onlyData;
            if (finalSingle != null)
            {
                yield return new WaitForSecondsRealtime(finalLockFreeze);
                applyFinal?.Invoke(finalSingle);
                PlaySFX(sfxRouletteLock, sfxRouletteLockVolume, sfxRouletteLockPitchVariation);

                if (finalImageTarget != null)
                    yield return StartCoroutine(PopScaleRoutine(finalImageTarget, finalImagePopScale, finalImagePopDuration, 1f));
            }

            yield break;
        }

        int previousIndex = -1;
        int stepCount = 0;
        List<float> intervals = BuildRouletteIntervals(duration);

        while (stepCount < intervals.Count)
        {
            int index = GetNextRouletteIndex(dataList.Count, previousIndex);
            previousIndex = index;

            T data = dataList[index];
            applyPreview?.Invoke(data);

            float currentInterval = intervals[stepCount];
            yield return new WaitForSecondsRealtime(currentInterval);

            stepCount++;
        }

        T finalData = getFinalData != null ? getFinalData() : default;

        if (finalData != null)
        {
            yield return new WaitForSecondsRealtime(finalLockFreeze);
            applyFinal?.Invoke(finalData);
            PlaySFX(sfxRouletteLock, sfxRouletteLockVolume, sfxRouletteLockPitchVariation);

            if (finalImageTarget != null)
                yield return StartCoroutine(PopScaleRoutine(finalImageTarget, finalImagePopScale, finalImagePopDuration, 1f));
        }
    }

    private List<float> BuildRouletteIntervals(float duration)
    {
        List<float> intervals = new List<float>();
        float total = 0f;
        int guard = 0;

        while (total < duration && guard < 1000)
        {
            float progress = duration <= 0f ? 1f : Mathf.Clamp01(total / duration);
            float eased = progress * progress;
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

        if (index >= previousIndex && previousIndex >= 0)
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
            SetCardHidden(riskCard, riskCardGroup, riskTitleBoxGroup, riskBodyBoxGroup, _riskCardOriginalPos);
            SetCardHidden(rewardCard, rewardCardGroup, rewardTitleBoxGroup, rewardBodyBoxGroup, _rewardCardOriginalPos);
            ClearTexts();
            ResetDestinyUI();
            eventRoot.SetActive(false);
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

                PlayDestinyHoverScale(index, destinyHoverScale);
            });

            AddEventTrigger(trigger, EventTriggerType.PointerExit, () =>
            {
                if (!canDestinyInteract)
                    return;

                float targetScale = currentDestinyIndex == index ? destinyHoverScale : 1f;
                PlayDestinyHoverScale(index, targetScale);
            });
        }

        ResetDestinyUI();
    }

    private void ResetDestinyUI()
    {
        canDestinyInteract = false;
        currentDestinyIndex = 0;

        if (destinyGroup != null)
        {
            destinyGroup.alpha = 0f;
            destinyGroup.interactable = false;
            destinyGroup.blocksRaycasts = false;
        }

        SetDestinyInteractable(false);

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

    private void OnClickDestinyOption(int index)
    {
        if (!canDestinyInteract)
            return;

        currentDestinyIndex = index;
        RefreshDestinyButtonVisual();
        ApplyDestinyDescription(index, true);
        PlaySFX(sfxDestinyClick, sfxDestinyClickVolume, sfxDestinyClickPitchVariation);

        if (destinyOptionImages != null)
        {
            for (int i = 0; i < destinyOptionImages.Length; i++)
            {
                if (destinyOptionImages[i] == null)
                    continue;

                float targetScale = i == currentDestinyIndex ? destinyHoverScale : 1f;
                PlayDestinyHoverScale(i, targetScale);
            }
        }
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
        EventSelectionData selection = EventManager.Instance != null
            ? EventManager.Instance.CurrentEventSelection
            : null;

        if (selection == null || !selection.IsValid())
            return;

        if (selection.selectedRisk != null && riskDescriptionText != null)
        {
            riskDescriptionText.text = selection.selectedRisk.GetDescription(descriptionIndex);

            if (playEmphasis)
                PlayDescriptionPop(riskDescriptionText, true);
        }

        if (selection.selectedReward != null && rewardDescriptionText != null)
        {
            rewardDescriptionText.text = selection.selectedReward.GetDescription(descriptionIndex);

            if (playEmphasis)
                PlayDescriptionPop(rewardDescriptionText, false);
        }

        if (playEmphasis)
            PlayCardImagePop();
    }

    private void PlayDescriptionPop(TextMeshProUGUI targetText, bool isRisk)
    {
        if (targetText == null)
            return;

        if (isRisk)
        {
            if (_riskDescriptionPopRoutine != null)
                StopCoroutine(_riskDescriptionPopRoutine);

            _riskDescriptionPopRoutine = StartCoroutine(
                PopScaleRoutine(targetText.rectTransform, descriptionPopScale, descriptionPopDuration, textDefaultScale)
            );
        }
        else
        {
            if (_rewardDescriptionPopRoutine != null)
                StopCoroutine(_rewardDescriptionPopRoutine);

            _rewardDescriptionPopRoutine = StartCoroutine(
                PopScaleRoutine(targetText.rectTransform, descriptionPopScale, descriptionPopDuration, textDefaultScale)
            );
        }
    }

    private void PlayCardImagePop()
    {
        if (riskCardImage != null)
        {
            if (_riskCardPopRoutine != null)
                StopCoroutine(_riskCardPopRoutine);

            _riskCardPopRoutine = StartCoroutine(
                PopScaleRoutine(riskCardImage.rectTransform, destinyCardPopScale, destinyCardPopDuration, 1f)
            );
        }

        if (rewardCardImage != null)
        {
            if (_rewardCardPopRoutine != null)
                StopCoroutine(_rewardCardPopRoutine);

            _rewardCardPopRoutine = StartCoroutine(
                PopScaleRoutine(rewardCardImage.rectTransform, destinyCardPopScale, destinyCardPopDuration, 1f)
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
            StopCoroutine(destinyScaleRoutines[index]);

        destinyScaleRoutines[index] = StartCoroutine(
            ScaleDestinyButtonRoutine(destinyOptionImages[index].rectTransform, targetScale)
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
            float t = Mathf.Clamp01(elapsed / destinyScaleDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            target.localScale = Vector3.Lerp(start, end, eased);
            yield return null;
        }

        target.localScale = end;
    }

    private void PlaySFX(AudioClip clip, float volumeScale, float pitchVariation)
    {
        if (clip != null && SoundManager.instance != null)
            SoundManager.instance.PlaySFX(clip, volumeScale, pitchVariation);
    }
}