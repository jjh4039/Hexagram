using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using ChocDino.UIFX;
using UnityEngine.InputSystem;

public class StageMessageUI : MonoBehaviour
{
    public static StageMessageUI instance;

    [Header("--- Entry UI (Start) ---")]
    [SerializeField] private CanvasGroup entryGroup;
    [SerializeField] private TextMeshProUGUI entryTitle;
    [SerializeField] private TextMeshProUGUI entryDesc;

    [Header("--- Clear UI (End) ---")]
    [SerializeField] private CanvasGroup clearGroup;
    [SerializeField] private TextMeshProUGUI clearText;

    [Header("--- Reward UI (New) ---")]
    [SerializeField] private CanvasGroup rewardGroup;
    [SerializeField] private RewardItem[] rewardItems;
    [SerializeField] private float rewardSlideDistance = 50f;
    [SerializeField] private float rewardInterval = 0.15f;
    [SerializeField] private RewardData[] rewardDatas;

    [Header("Reward Juicy Settings")]
    [SerializeField] private AnimationCurve appearanceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float punchScaleAmount = 1.15f;

    [Header("Settings")]
    [SerializeField] private float startDelay = 0.7f;
    [SerializeField] private float fadeInTime = 0.5f;
    [SerializeField] private float waitTime = 2.0f;
    [SerializeField] private float fadeOutTime = 0.5f;

    [Header("Sound")]
    [SerializeField] private AudioClip sfxClear;
    [SerializeField] private AudioClip sfxSelect;

    [System.Serializable]
    public struct RewardItem
    {
        public RectTransform rect;
        public CanvasGroup group;
        public GlowFilter glowEffect;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI valueText;
    }

    private Coroutine currentCoroutine;
    private bool canSelectReward = false;
    private Vector3[] originalScales;

    private void Awake()
    {
        instance = this;

        // 리워드 아이템 스케일 저장
        originalScales = new Vector3[rewardItems.Length];
        for (int i = 0; i < rewardItems.Length; i++)
            if (rewardItems[i].rect != null) originalScales[i] = rewardItems[i].rect.localScale;

        ResetAllUI();
        ShowClearMessage(); // 테스트용
    }

    private void ResetAllUI()
    {
        if (entryGroup != null) entryGroup.alpha = 0f;
        if (clearGroup != null) clearGroup.alpha = 0f;
        if (rewardGroup != null) rewardGroup.alpha = 0f;

        for (int i = 0; i < rewardItems.Length; i++)
        {
            if (rewardItems[i].group != null) rewardItems[i].group.alpha = 0f;
            if (rewardItems[i].glowEffect != null) rewardItems[i].glowEffect.enabled = false;
            if (rewardItems[i].rect != null) rewardItems[i].rect.localScale = originalScales[i];
        }
    }

    // ==========================================
    // 1. Entry Message (기존 구조 완벽 유지)
    // ==========================================
    public void ShowEntryMessage(string title, string desc)
    {
        if (entryTitle != null) entryTitle.text = title;
        if (entryDesc != null) entryDesc.text = desc;

        StopCurrentCoroutine();
        ResetAllUI();
        currentCoroutine = StartCoroutine(EntryFadeSequence());
    }

    private IEnumerator EntryFadeSequence()
    {
        if (entryGroup == null) yield break;
        yield return new WaitForSeconds(startDelay);
        yield return FadeIn(entryGroup);
        yield return new WaitForSeconds(waitTime);
        yield return FadeOut(entryGroup);
    }

    // ==========================================
    // 2. Clear & Reward Message (디테일 연출)
    // ==========================================
    public void ShowClearMessage()
    {
        StopCurrentCoroutine();
        ResetAllUI();

        if (sfxClear != null)
            SoundManager.instance.PlaySFX(sfxClear, 1.2f, 0.1f);

        currentCoroutine = StartCoroutine(ClearAndRewardSequence());
    }

    private IEnumerator ClearAndRewardSequence()
    {
        // 클리어 텍스트 페이드인 시작 (병렬 실행)
        if (clearGroup != null) StartCoroutine(FadeIn(clearGroup));

        // 리워드 아이템들 순차적 등장
        if (rewardGroup != null)
        {
            rewardGroup.alpha = 1f;
            SetRandomRewardTexts();
            for (int i = 0; i < rewardItems.Length; i++)
            {
                StartCoroutine(AnimateRewardItem(rewardItems[i], i));
                yield return new WaitForSeconds(rewardInterval);
            }
            canSelectReward = true;
        }
    }

    private void SetRandomRewardTexts()
    {
        if (rewardDatas == null || rewardDatas.Length == 0) return;

        // 중복 방지를 위한 인덱스 풀
        int[] indices = new int[rewardDatas.Length];
        for (int i = 0; i < indices.Length; i++)
            indices[i] = i;

        //
        for (int i = indices.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        // 리워드 슬롯 수만큼만 할당
        for (int i = 0; i < rewardItems.Length; i++)
        {
            if (i >= indices.Length) break;

            RewardData data = rewardDatas[indices[i]];

            if (rewardItems[i].titleText != null)
                rewardItems[i].titleText.text = data.titleText;

            if (rewardItems[i].valueText != null)
                rewardItems[i].valueText.text = data.valueText;

            if (rewardItems[i].valueText != null)
                rewardItems[i].valueText.color = data.valueTextColor;
        }
    }

    private IEnumerator AnimateRewardItem(RewardItem item, int index)
    {
        float timer = 0f;
        Vector2 endPos = item.rect.anchoredPosition;
        Vector2 startPos = endPos + new Vector2(0, -rewardSlideDistance);

        item.rect.anchoredPosition = startPos;
        item.group.alpha = 0f;
        item.rect.localScale = originalScales[index] * 0.8f;

        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            float t = timer / fadeInTime;
            float curveT = appearanceCurve.Evaluate(t);

            item.group.alpha = t;
            item.rect.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, curveT);
            item.rect.localScale = Vector3.LerpUnclamped(originalScales[index] * 0.8f, originalScales[index], curveT);
            yield return null;
        }

        item.group.alpha = 1f;
        item.rect.anchoredPosition = endPos;
        item.rect.localScale = originalScales[index];
    }

    // ==========================================
    // 3. 입력 감지 및 선택 피드백
    // ==========================================
    private void Update()
    {
        if (!canSelectReward) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectReward(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectReward(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectReward(2);
    }

    private void SelectReward(int index)
    {
        if (index >= rewardItems.Length) return;
        canSelectReward = false;

        if (sfxSelect != null) SoundManager.instance.PlaySFX(sfxSelect, 1f, 0.1f);

        // 글로우 효과 및 펀치 스케일
        if (rewardItems[index].glowEffect != null) rewardItems[index].glowEffect.enabled = true;
        StartCoroutine(PunchScale(rewardItems[index].rect, index));

        // 선택되지 않은 아이템들 흐릿하게 처리
        for (int i = 0; i < rewardItems.Length; i++)
        {
            if (i != index) StartCoroutine(QuickFadeOut(rewardItems[i].group));
        }

        Invoke(nameof(HideAllClearUI), 0.4f);
    }

    private IEnumerator PunchScale(RectTransform target, int index)
    {
        float timer = 0f;
        Vector3 startScale = originalScales[index];
        Vector3 peakScale = startScale * punchScaleAmount;

        while (timer < 0.1f)
        {
            timer += Time.deltaTime;
            target.localScale = Vector3.Lerp(startScale, peakScale, timer / 0.1f);
            yield return null;
        }
        timer = 0f;
        while (timer < 0.1f)
        {
            timer += Time.deltaTime;
            target.localScale = Vector3.Lerp(peakScale, startScale, timer / 0.1f);
            yield return null;
        }
    }

    public void HideAllClearUI()
    {
        StopCurrentCoroutine();
        StartCoroutine(FullFadeOut());
    }

    private IEnumerator FullFadeOut()
    {
        float timer = 0f;
        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;
            float t = timer / fadeOutTime;
            if (clearGroup != null) clearGroup.alpha = 1 - t;
            if (rewardGroup != null) rewardGroup.alpha = 1 - t;
            yield return null;
        }
        ResetAllUI();
    }

    // ==========================================
    // 유틸리티 (Fade 관련)
    // ==========================================
    private IEnumerator FadeIn(CanvasGroup group)
    {
        float timer = 0f;
        group.alpha = 0f;
        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            group.alpha = timer / fadeInTime;
            yield return null;
        }
        group.alpha = 1f;
    }

    private IEnumerator FadeOut(CanvasGroup group)
    {
        float timer = 0f;
        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;
            group.alpha = 1f - (timer / fadeOutTime);
            yield return null;
        }
        group.alpha = 0f;
    }

    private IEnumerator QuickFadeOut(CanvasGroup group)
    {
        float timer = 0f;
        float startAlpha = group.alpha;
        while (timer < 0.2f)
        {
            timer += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, 0.1f, timer / 0.2f);
            yield return null;
        }
    }

    private void StopCurrentCoroutine()
    {
        if (currentCoroutine != null) { StopCoroutine(currentCoroutine); currentCoroutine = null; }
    }
}