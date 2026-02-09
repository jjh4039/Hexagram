using ChocDino.UIFX;
using NUnit.Framework.Internal;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BitManager : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;

    [Header("Background Loop Settings")]
    [SerializeField] private float bgScaleMin = 0.9f;
    [SerializeField] private float bgScaleMax = 1.1f;
    [SerializeField] private float bgRotationRange = 5f;
    [SerializeField] private float bgAnimSpeed = 0.5f;

    [SerializeField] private BitChoices[] bitChoices;
    [SerializeField] private ArtifactData[] allArtifacts;
    [SerializeField] private ArtifactGradeProbability gradeProbability;
    [SerializeField] private ArtifactGradeColor gradeColors;
    [SerializeField] private CanvasGroup mainCanvasGroup;

    [Header("Confirm Button Settings")]
    [SerializeField] private Image confirmButtonImage;
    [SerializeField] private float buttonYOffset = -250f;

    [Header("Animation Settings")]
    [SerializeField] private float hoverYOffset = 40f;
    [SerializeField] private float animationSpeed = 8f;
    [SerializeField] private float introDelay = 0.15f;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxIntro;
    [SerializeField] private AudioClip sfxSelect;
    [SerializeField] private AudioClip sfxDecision;

    private HashSet<ArtifactData> usedArtifacts = new HashSet<ArtifactData>();
    private Coroutine[] hoverCoroutines;
    private bool[] isHovering;
    private int selectedIndex = -1;
    private bool isInitialized = false;

    void Awake()
    {
        hoverCoroutines = new Coroutine[bitChoices.Length];
        isHovering = new bool[bitChoices.Length];

        // 초기 상태 설정
        if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0;

        for (int i = 0; i < bitChoices.Length; i++)
        {
            if (bitChoices[i].group != null) bitChoices[i].group.alpha = 0;
            if (bitChoices[i].rect != null)
                bitChoices[i].initialAnchoredPos = bitChoices[i].rect.anchoredPosition;
        }
    }

    /// <summary>
    /// Bit 오브젝트에서 F를 눌렀을 때 호출될 핵심 함수
    /// </summary>
    public void OpenBitUI()
    {
        // 1. 상태 초기화
        isInitialized = false;
        selectedIndex = -1;
        usedArtifacts.Clear();
        if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(false);

        // 2. 데이터 세팅
        SetupBitChoices();

        // 3. 시간 정지 (UI 조작 중 게임 멈춤)
        Time.timeScale = 0f;
        if (sfxIntro != null) SoundManager.instance.PlaySFX(sfxIntro, 0.15f, 0.1f);

        // 4. 연출 시작
        StopAllCoroutines();
        gameObject.SetActive(true); // 비활성화 되어있을 경우를 대비
        if (backgroundImage != null) StartCoroutine(LoopBackgroundAnimation());
        StartCoroutine(SequenceIntro());
    }

    private IEnumerator LoopBackgroundAnimation()
    {
        float t = 0;
        while (true)
        {
            t += Time.unscaledDeltaTime * bgAnimSpeed;
            float wave = (Mathf.Sin(t) + 1f) * 0.5f;
            float currentScale = Mathf.Lerp(bgScaleMin, bgScaleMax, wave);
            backgroundImage.rectTransform.localScale = new Vector3(currentScale, currentScale, 1f);
            float rotWave = Mathf.Cos(t * 0.7f);
            backgroundImage.rectTransform.localRotation = Quaternion.Euler(0, 0, rotWave * bgRotationRange);
            yield return null;
        }
    }

    private IEnumerator SequenceIntro()
    {
        // 전체 배경 페이드 인
        float elapsed = 0;

        while (elapsed < 0.4f)
        {
            elapsed += Time.unscaledDeltaTime;
            if (mainCanvasGroup != null) mainCanvasGroup.alpha = elapsed / 0.4f;
            yield return null;
        }

        // 카드 순차 등장
        for (int i = 0; i < bitChoices.Length; i++)
        {
            StartCoroutine(IntroFlashCard(i));
            yield return new WaitForSecondsRealtime(introDelay);
        }

        yield return new WaitForSecondsRealtime(0.3f);
        isInitialized = true;
    }

    private IEnumerator IntroFlashCard(int index)
    {
        CanvasGroup group = bitChoices[index].group;
        RectTransform rect = bitChoices[index].rect;
        Vector2 basePos = bitChoices[index].initialAnchoredPos;
        float t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 1.2f;
            if (group != null) group.alpha = Mathf.Clamp01(t);
            float curve = Mathf.Sin(t * Mathf.PI);
            rect.anchoredPosition = basePos + new Vector2(0, curve * hoverYOffset);
            yield return null;
        }
        rect.anchoredPosition = basePos;
        if (group != null) group.alpha = 1f;
    }

    void Update()
    {
        if (!isInitialized) return;
        CheckMouseHover();
        CheckMouseClick();
    }

    private void CheckMouseHover()
    {
        if (selectedIndex != -1) return;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        for (int i = 0; i < bitChoices.Length; i++)
        {
            bool overlaps = RectTransformUtility.RectangleContainsScreenPoint(bitChoices[i].hoverSensor.rectTransform, mousePos, null);
            if (overlaps && !isHovering[i]) { isHovering[i] = true; StartHoverAnimation(i, true); }
            else if (!overlaps && isHovering[i]) { isHovering[i] = false; StartHoverAnimation(i, false); }
        }
    }

    private void CheckMouseClick()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            // 확인 버튼 클릭 체크
            if (confirmButtonImage != null && confirmButtonImage.gameObject.activeInHierarchy)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(confirmButtonImage.rectTransform, mousePos, null))
                {
                    OnConfirmButtonClick();
                    if (sfxDecision != null) SoundManager.instance.PlaySFX(sfxDecision, 0.5f, 0.2f);
                    return;
                }
            }

            // 카드 클릭 체크
            for (int i = 0; i < bitChoices.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(bitChoices[i].hoverSensor.rectTransform, mousePos, null))
                {
                    SelectCard(i);
                    if (sfxSelect != null) SoundManager.instance.PlaySFX(sfxSelect, 0.3f, 0.1f);
                    break;
                }
            }
        }
    }

    private void SelectCard(int newIndex)
    {
        if (selectedIndex == newIndex) return;

        if (selectedIndex != -1)
        {
            int prev = selectedIndex;
            isHovering[prev] = false;
            StartHoverAnimation(prev, false);
            if (bitChoices[prev].choiceEffect != null) bitChoices[prev].choiceEffect.enabled = false;
        }

        selectedIndex = newIndex;
        isHovering[selectedIndex] = true;
        StartHoverAnimation(selectedIndex, true);
        if (bitChoices[selectedIndex].choiceEffect != null) bitChoices[selectedIndex].choiceEffect.enabled = true;
        UpdateConfirmButtonPosition();
    }

    private void UpdateConfirmButtonPosition()
    {
        if (confirmButtonImage == null || selectedIndex == -1) return;
        confirmButtonImage.gameObject.SetActive(true);
        RectTransform btnRect = confirmButtonImage.rectTransform;

        // 선택한 카드 위치에 맞춤
        btnRect.position = bitChoices[selectedIndex].hoverSensor.transform.position;
        Vector2 anchored = btnRect.anchoredPosition;
        anchored.y = buttonYOffset;
        btnRect.anchoredPosition = anchored;
    }

    public void OnConfirmButtonClick()
    {
        if (selectedIndex == -1) return;

        // ★ [핵심] 선택된 아티팩트 매니저에 전달
        ArtifactData selectedArtifact = bitChoices[selectedIndex].currentArtifact;
        if (selectedArtifact != null && ArtifactManager.instance != null)
        {
            ArtifactManager.instance.AddArtifact(selectedArtifact);
        }

        StartCoroutine(ExitSequence());
    }

    private IEnumerator ExitSequence()
    {
        isInitialized = false;

        // 1. 선택 안된 카드들 & 버튼 퇴장
        if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(false);
        for (int i = 0; i < bitChoices.Length; i++)
        {
            if (i != selectedIndex) StartCoroutine(FadeOutCanvasGroup(bitChoices[i].group, 0.2f));
        }

        // 2. 선택된 카드 강조 및 전체 페이드 아웃
        RectTransform selectedRect = bitChoices[selectedIndex].rect;
        Vector3 startScale = selectedRect.localScale;
        Vector3 targetScale = startScale * 1.15f;
        float startAlpha = mainCanvasGroup.alpha;

        float elapsed = 0;
        float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float smoothT = t * t * (3f - 2f * t);

            selectedRect.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
            if (mainCanvasGroup != null) mainCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0, t);

            yield return null;
        }

        // 3. 시간 복구 및 UI 비활성화
        Time.timeScale = 1.0f;

        // 스케일 원복 (다음 오픈을 위해)
        selectedRect.localScale = Vector3.one;
        gameObject.SetActive(false);
    }

    private IEnumerator FadeOutCanvasGroup(CanvasGroup cg, float duration)
    {
        if (cg == null) yield break;
        float startAlpha = cg.alpha;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, 0, elapsed / duration);
            yield return null;
        }
        cg.alpha = 0;
    }

    private void StartHoverAnimation(int index, bool isEntering)
    {
        if (hoverCoroutines[index] != null) StopCoroutine(hoverCoroutines[index]);
        Vector2 basePos = bitChoices[index].initialAnchoredPos;
        Vector2 targetPos = isEntering ? basePos + new Vector2(0, hoverYOffset) : basePos;
        hoverCoroutines[index] = StartCoroutine(AnimateCard(index, targetPos));
    }

    private IEnumerator AnimateCard(int index, Vector2 targetAnchoredPos)
    {
        RectTransform rect = bitChoices[index].rect;
        while (Vector2.Distance(rect.anchoredPosition, targetAnchoredPos) > 0.1f)
        {
            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetAnchoredPos, Time.unscaledDeltaTime * animationSpeed);
            yield return null;
        }
        rect.anchoredPosition = targetAnchoredPos;
        hoverCoroutines[index] = null;
    }

    // --- 데이터 로직 ---
    public void SetupBitChoices()
    {
        usedArtifacts.Clear();
        for (int i = 0; i < bitChoices.Length; i++)
        {
            ArtifactData artifact = GetRandomArtifactByProbability();
            // ★ 슬롯에 데이터 할당
            bitChoices[i].currentArtifact = artifact;

            if (artifact != null) usedArtifacts.Add(artifact);
            ApplyArtifactToChoice(bitChoices[i], artifact);
        }
    }

    private ArtifactData GetRandomArtifactByProbability()
    {
        float total = gradeProbability.common + gradeProbability.rare + gradeProbability.epic + gradeProbability.legendary;
        float rand = Random.value * total;
        if (rand < gradeProbability.common) return GetRandomArtifactByGrade(ArtifactGrade.Common);
        rand -= gradeProbability.common;
        if (rand < gradeProbability.rare) return GetRandomArtifactByGrade(ArtifactGrade.Rare);
        rand -= gradeProbability.rare;
        if (rand < gradeProbability.epic) return GetRandomArtifactByGrade(ArtifactGrade.Epic);
        return GetRandomArtifactByGrade(ArtifactGrade.Legendary);
    }

    private ArtifactData GetRandomArtifactByGrade(ArtifactGrade grade)
    {
        List<ArtifactData> candidates = new List<ArtifactData>();
        foreach (var artifact in allArtifacts)
        {
            // 이미 이 화면(usedArtifacts)에 뽑힌 것은 제외
            if (artifact.grade == grade && !usedArtifacts.Contains(artifact)) candidates.Add(artifact);
        }
        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    private void ApplyArtifactToChoice(BitChoices choice, ArtifactData artifact)
    {
        if (artifact == null) return;
        choice.artifactImage.sprite = artifact.icon;
        choice.titleText.text = artifact.artifactName;
        choice.gradeText.text = "[ " + artifact.grade.ToString() + " ]";
        choice.desText.text = artifact.description;
        foreach (var effect in choice.gradeEffects) effect.Color = GetColorByGrade(artifact.grade);
        choice.outLineImage.color = GetOutLineColor(artifact.grade);
    }

    private Color GetOutLineColor(ArtifactGrade grade)
    {
        switch (grade)
        {
            case ArtifactGrade.Common: return Color.white;
            case ArtifactGrade.Rare: return new Color(0f,0.65f,1f);
            case ArtifactGrade.Epic: return new Color(0.6f, 0f, 1f);
            case ArtifactGrade.Legendary: return new Color(1f, 0.72f, 0f);
            default: return Color.white;
        }
    }

    private Color GetColorByGrade(ArtifactGrade grade)
    {
        switch (grade)
        {
            case ArtifactGrade.Common: return gradeColors.common;
            case ArtifactGrade.Rare: return gradeColors.rare;
            case ArtifactGrade.Epic: return gradeColors.epic;
            case ArtifactGrade.Legendary: return gradeColors.legendary;
            default: return Color.white;
        }

    }

    [System.Serializable]
    public struct BitChoices
    {
        public RectTransform rect;
        public Image hoverSensor;
        public CanvasGroup group;
        public Image outLineImage;
        public GlowFilter choiceEffect;
        public GlowFilter[] gradeEffects;
        public Image artifactImage;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI gradeText;
        public TextMeshProUGUI desText;
        [HideInInspector] public Vector2 initialAnchoredPos;
        // ★ 현재 이 슬롯에 할당된 데이터 저장용
        [HideInInspector] public ArtifactData currentArtifact;
    }

    [System.Serializable] public struct ArtifactGradeProbability { [Range(0f, 1f)] public float common, rare, epic, legendary; }
    [System.Serializable] public struct ArtifactGradeColor { public Color common, rare, epic, legendary; }
}