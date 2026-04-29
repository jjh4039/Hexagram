using ChocDino.UIFX;
using NUnit.Framework.Internal;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 아티팩트 선택 및 획득을 관리하는 UI 매니저
public class BitManager : MonoBehaviour
{
    public static BitManager instance;                             // 전역 접근용 인스턴스

    [SerializeField] private Image backgroundImage;                // 배경 이미지

    [Header("Background Loop Settings")]
    [SerializeField] private float bgScaleMin = 0.9f;
    [SerializeField] private float bgScaleMax = 1.1f;
    [SerializeField] private float bgRotationRange = 5f;
    [SerializeField] private float bgAnimSpeed = 0.5f;

    [SerializeField] private BitChoices[] bitChoices;              // 카드 슬롯 배열
    [SerializeField] private ArtifactData[] allArtifacts;          // 전체 아티팩트 데이터베이스
    [SerializeField] private ArtifactGradeProbability gradeProbability; // 등급별 확률 설정
    [SerializeField] private ArtifactGradeColor gradeColors;       // 등급별 강조 색상
    [SerializeField] private CanvasGroup mainCanvasGroup;          // 전체 UI 투명도 관리

    [Header("Confirm Button Settings")]
    [SerializeField] private Image confirmButtonImage;             // 확정 버튼
    [SerializeField] private float buttonYOffset = -250f;          // 버튼 수직 위치 오프셋

    [Header("Animation Settings")]
    [SerializeField] private float hoverYOffset = 40f;             // 호버 시 올라가는 높이
    [SerializeField] private float animationSpeed = 8f;            // 애니메이션 부드러움 정도
    [SerializeField] private float introDelay = 0.15f;             // 카드 순차 등장 간격

    [Header("Audio")]
    [SerializeField] private AudioClip sfxIntro;                   // 등장 사운드
    [SerializeField] private AudioClip sfxSelect;                  // 카드 선택 사운드
    [SerializeField] private AudioClip sfxDecision;                // 최종 확정 사운드

    private HashSet<ArtifactData> usedArtifacts = new HashSet<ArtifactData>(); // 중복 방지용 셋
    private Coroutine[] hoverCoroutines;                           // 각 카드 호버 코루틴
    private bool[] isHovering;                                     // 각 카드 호버 상태
    private int selectedIndex = -1;                                // 현재 선택된 카드 인덱스
    private bool isInitialized = false;                            // 조작 가능 여부

    void Awake()
    {
        instance = this;
        hoverCoroutines = new Coroutine[bitChoices.Length];
        isHovering = new bool[bitChoices.Length];

        if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0;

        for (int i = 0; i < bitChoices.Length; i++)
        {
            if (bitChoices[i].group != null) bitChoices[i].group.alpha = 0;
            if (bitChoices[i].rect != null)
                bitChoices[i].initialAnchoredPos = bitChoices[i].rect.anchoredPosition;
        }

        gameObject.SetActive(false);                               // 시작 시 비활성화
    }

    // 아이템 상호작용 시 호출되는 UI 오픈 함수
    public void OpenBitUI()
    {
        // 매니저에게 조작 상태 전환 요청
        if (InputStateManager.Instance != null && !InputStateManager.Instance.TryOpenUI()) return;

        isInitialized = false;
        selectedIndex = -1;
        usedArtifacts.Clear();

        if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(false);

        for (int i = 0; i < bitChoices.Length; i++)
        {
            isHovering[i] = false;
            if (hoverCoroutines[i] != null) StopCoroutine(hoverCoroutines[i]);
            bitChoices[i].rect.anchoredPosition = bitChoices[i].initialAnchoredPos;
            bitChoices[i].rect.localScale = Vector3.one;

            if (bitChoices[i].group != null)
                bitChoices[i].group.alpha = 0;
        }

        SetupBitChoices();                                         // 카드 데이터 할당

        Time.timeScale = 0f;                                       // 게임 일시 정지
        if (sfxIntro != null && SoundManager.instance != null) 
            SoundManager.instance.PlaySFX(sfxIntro, 0.15f, 0.1f);

        StopAllCoroutines();
        gameObject.SetActive(true);
        if (backgroundImage != null) StartCoroutine(LoopBackgroundAnimation());
        StartCoroutine(SequenceIntro());                           // 등장 연출 시작
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
        float elapsed = 0;
        while (elapsed < 0.4f)
        {
            elapsed += Time.unscaledDeltaTime;
            if (mainCanvasGroup) mainCanvasGroup.alpha = elapsed / 0.4f;
            yield return null;
        }

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
            if (group) group.alpha = Mathf.Clamp01(t);
            float curve = Mathf.Sin(t * Mathf.PI);
            rect.anchoredPosition = basePos + new Vector2(0, curve * hoverYOffset);
            yield return null;
        }
        rect.anchoredPosition = basePos;
        if (group) group.alpha = 1f;
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

            if (confirmButtonImage && confirmButtonImage.gameObject.activeInHierarchy)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(confirmButtonImage.rectTransform, mousePos, null))
                {
                    if (sfxDecision && SoundManager.instance != null) 
                        SoundManager.instance.PlaySFX(sfxDecision, 0.5f, 0.2f);
                    OnConfirmButtonClick();
                    return;
                }
            }

            for (int i = 0; i < bitChoices.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(bitChoices[i].hoverSensor.rectTransform, mousePos, null))
                {
                    if (sfxSelect != null && SoundManager.instance != null) 
                        SoundManager.instance.PlaySFX(sfxSelect, 0.3f, 0.1f);
                    SelectCard(i);
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
        }

        selectedIndex = newIndex;
        isHovering[selectedIndex] = true;
        StartHoverAnimation(selectedIndex, true);
        UpdateConfirmButtonPosition();
    }

    private void UpdateConfirmButtonPosition()
    {
        if (confirmButtonImage == null || selectedIndex == -1) return;
        confirmButtonImage.gameObject.SetActive(true);
        RectTransform btnRect = confirmButtonImage.rectTransform;

        btnRect.position = bitChoices[selectedIndex].hoverSensor.transform.position;
        Vector2 anchored = btnRect.anchoredPosition;
        anchored.y = buttonYOffset;
        btnRect.anchoredPosition = anchored;
    }

    public void OnConfirmButtonClick()
    {
        if (selectedIndex == -1) return;

        ArtifactData selectedArtifact = bitChoices[selectedIndex].currentArtifact;
        if (selectedArtifact != null && ArtifactManager.instance != null)
        {
            ArtifactManager.instance.AddArtifact(selectedArtifact); // 획득 처리
        }

        StartCoroutine(ExitSequence());
    }

    private IEnumerator ExitSequence()
    {
        isInitialized = false;

        if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(false);
        for (int i = 0; i < bitChoices.Length; i++)
        {
            if (i != selectedIndex) StartCoroutine(FadeOutCanvasGroup(bitChoices[i].group, 0.2f));
        }

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

        Time.timeScale = 1.0f;                                     // 시간 복구
        selectedRect.localScale = Vector3.one;

        // 조작 상태를 평화 모드로 복귀
        if (InputStateManager.Instance != null) InputStateManager.Instance.CloseUI();

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

    public void SetupBitChoices()
    {
        usedArtifacts.Clear();
        for (int i = 0; i < bitChoices.Length; i++)
        {
            ArtifactData artifact = GetRandomArtifactByProbability();
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
            case ArtifactGrade.Rare: return new Color(0f, 0.65f, 1f);
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
        public GlowFilter[] gradeEffects;
        public Image artifactImage;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI gradeText;
        public TextMeshProUGUI desText;
        [HideInInspector] public Vector2 initialAnchoredPos;
        [HideInInspector] public ArtifactData currentArtifact;
    }

    [System.Serializable] public struct ArtifactGradeProbability { [Range(0f, 1f)] public float common, rare, epic, legendary; }
    [System.Serializable] public struct ArtifactGradeColor { public Color common, rare, epic, legendary; }
}