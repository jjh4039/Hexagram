using UnityEngine;
using TMPro;
using System.Collections;
using ChocDino.UIFX;
using UnityEngine.InputSystem;

// 스테이지 진입, 클리어, 보상 선택 및 적 남은 수를 표시하는 UI 컨트롤러
public class StageMessageUI : MonoBehaviour
{
    public static StageMessageUI instance;

    [Header("--- Debug / Test ---")]
    [SerializeField] private bool enableRewardTest = true;

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
    [SerializeField] private ModuleData[] rewardDatas;

    [Header("--- Enemy Count UI (New) ---")]
    [SerializeField] private CanvasGroup enemyCountGroup;
    [SerializeField] private TextMeshProUGUI enemyCountText;

    [Header("Reward Juicy Settings")]
    [SerializeField] private AnimationCurve appearanceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float punchScaleAmount = 1.15f;

    [Header("Settings")]
    [SerializeField] private float startDelay = 0.55f;
    [SerializeField] private float fadeInTime = 0.5f;
    [SerializeField] private float waitTime = 1.0f;
    [SerializeField] private float fadeOutTime = 0.5f;

    [Header("Sound")]
    [SerializeField] private AudioClip sfxClear;
    [SerializeField] private AudioClip sfxDecision;

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
    private bool isEnemyCountVisible = false;
    private Vector3 enemyCountOriginScale;

    // [추가됨] 화면에 표시된 3개의 보상 데이터를 기억해둘 배열
    private ModuleData[] _currentDisplayedRewards;

    private void Awake()
    {
        instance = this;
        originalScales = new Vector3[rewardItems.Length];
        _currentDisplayedRewards = new ModuleData[rewardItems.Length]; // 배열 초기화

        for (int i = 0; i < rewardItems.Length; i++)
            if (rewardItems[i].rect != null) originalScales[i] = rewardItems[i].rect.localScale;

        if (enemyCountText != null)
            enemyCountOriginScale = enemyCountText.rectTransform.localScale;

        ResetAllUI();
        if (enemyCountGroup != null) enemyCountGroup.alpha = 0f;
        isEnemyCountVisible = false;
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

    public void ShowClearMessage()
    {
        StopCurrentCoroutine();
        ResetAllUI();
        if (sfxClear != null) SoundManager.instance.PlaySFX(sfxClear, 1.2f, 0.1f);
        currentCoroutine = StartCoroutine(ClearAndRewardSequence());
    }

    private IEnumerator ClearAndRewardSequence()
    {
        if (clearGroup != null) StartCoroutine(FadeIn(clearGroup));
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

        int[] indices = new int[rewardDatas.Length];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;
        for (int i = indices.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        for (int i = 0; i < rewardItems.Length; i++)
        {
            if (i >= indices.Length) break;
            ModuleData data = rewardDatas[indices[i]];

            // [추가됨] 화면에 표시되는 슬롯 인덱스에 실제 데이터를 저장
            _currentDisplayedRewards[i] = data;

            if (rewardItems[i].titleText != null) rewardItems[i].titleText.text = data.titleText;
            if (rewardItems[i].valueText != null) rewardItems[i].valueText.text = data.valueText;
            if (rewardItems[i].valueText != null) rewardItems[i].valueText.color = data.valueTextColor;
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

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (InputStateManager.Instance != null && InputStateManager.Instance.CurrentInputState != InputState.Normal)
            return;

        if (enableRewardTest && Keyboard.current.rKey.wasPressedThisFrame)
        {
            ShowClearMessage();
            return;
        }

        if (!canSelectReward) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectReward(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectReward(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectReward(2);
    }

    private void SelectReward(int index)
    {
        if (index >= rewardItems.Length) return;
        canSelectReward = false;
        if (sfxDecision != null) SoundManager.instance.PlaySFX(sfxDecision, 0.5f, 0.1f);
        if (rewardItems[index].glowEffect != null) rewardItems[index].glowEffect.enabled = true;

        // ★ [핵심 추가] 저장해둔 데이터를 GameManager를 통해 PlayerStats로 전송
        if (GameManager.instance != null && GameManager.instance.stats != null)
        {
            GameManager.instance.stats.ApplyModuleReward(_currentDisplayedRewards[index]);
        }

        StartCoroutine(PunchScale(rewardItems[index].rect, index));
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

    public void UpdateEnemyCount(int totalCount, bool playPunch = false)
    {
        if (enemyCountText != null)
        {
            enemyCountText.text = totalCount.ToString();
        }

        if (totalCount > 0 && !isEnemyCountVisible)
        {
            isEnemyCountVisible = true;
            float entryTotalTime = startDelay + fadeInTime + waitTime + fadeOutTime;
            StartCoroutine(DelayedFadeIn(enemyCountGroup, entryTotalTime));
        }
        else if (totalCount <= 0 && isEnemyCountVisible)
        {
            isEnemyCountVisible = false;
            StartCoroutine(FadeOut(enemyCountGroup));
        }

        if (totalCount > 0 && isEnemyCountVisible && playPunch)
        {
            StartCoroutine(EnemyCountPunch());
        }
    }

    public void HideEnemyCountUI()
    {
        if (enemyCountGroup != null && enemyCountGroup.alpha > 0f)
        {
            StartCoroutine(FadeOut(enemyCountGroup));
        }
    }

    private IEnumerator EnemyCountPunch()
    {
        if (enemyCountText == null) yield break;
        float duration = 0.2f;
        float elapsed = 0f;
        float maxScaleAmount = 1.25f;
        Color originColor = Color.white;
        Color punchColor = Color.red;
        Vector3 targetScale = enemyCountOriginScale * maxScaleAmount;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curve = Mathf.Sin(t * Mathf.PI);
            enemyCountText.rectTransform.localScale = Vector3.Lerp(enemyCountOriginScale, targetScale, curve);
            enemyCountText.color = Color.Lerp(originColor, punchColor, curve);
            yield return null;
        }
        enemyCountText.rectTransform.localScale = enemyCountOriginScale;
        enemyCountText.color = originColor;
    }

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

    private IEnumerator DelayedFadeIn(CanvasGroup group, float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return FadeIn(group);
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