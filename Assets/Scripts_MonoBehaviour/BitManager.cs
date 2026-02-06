using ChocDino.UIFX;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

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
    [SerializeField] private float introDelay = 0.25f;

    private HashSet<ArtifactData> usedArtifacts = new HashSet<ArtifactData>();
    private Coroutine[] hoverCoroutines;
    private bool[] isHovering;
    private int selectedIndex = -1;
    private bool isInitialized = false;

    void Awake()
    {
        hoverCoroutines = new Coroutine[bitChoices.Length];
        isHovering = new bool[bitChoices.Length];

        if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0;
        foreach (var choice in bitChoices)
        {
            if (choice.group != null) choice.group.alpha = 0;
        }
    }

    void Start()
    {
        SetupBitChoices();
        for (int i = 0; i < bitChoices.Length; i++)
        {
            if (bitChoices[i].rect != null)
                bitChoices[i].initialAnchoredPos = bitChoices[i].rect.anchoredPosition;
        }

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
        // 1. 전체 배경 페이드 인
        float elapsed = 0;
        while (elapsed < 0.6f)
        {
            elapsed += Time.unscaledDeltaTime;
            if (mainCanvasGroup != null) mainCanvasGroup.alpha = elapsed / 0.6f;
            yield return null;
        }

        // 2. 카드 순차 등장
        for (int i = 0; i < bitChoices.Length; i++)
        {
            StartCoroutine(IntroFlashCard(i));
            yield return new WaitForSecondsRealtime(introDelay);
        }

        yield return new WaitForSecondsRealtime(0.5f);
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
            t += Time.unscaledDeltaTime * 0.8f;
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
            if (confirmButtonImage != null && confirmButtonImage.gameObject.activeInHierarchy)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(confirmButtonImage.rectTransform, mousePos, null))
                {
                    OnConfirmButtonClick();
                    return;
                }
            }
            for (int i = 0; i < bitChoices.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(bitChoices[i].hoverSensor.rectTransform, mousePos, null))
                {
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
        btnRect.position = bitChoices[selectedIndex].hoverSensor.transform.position;
        btnRect.anchoredPosition = new Vector2(btnRect.anchoredPosition.x, buttonYOffset);
    }

    public void OnConfirmButtonClick()
    {
        if (selectedIndex == -1) return;
        StartCoroutine(ExitSequence());
    }

    private IEnumerator ExitSequence()
    {
        isInitialized = false;

        // 1. 선택 안된 카드들 & 버튼 즉시 부드럽게 퇴장
        if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(false);
        for (int i = 0; i < bitChoices.Length; i++)
        {
            if (i != selectedIndex) StartCoroutine(FadeOutCanvasGroup(bitChoices[i].group, 0.25f));
        }

        // 2. 선택된 카드 강조 & 전체 페이드 아웃 연출
        RectTransform selectedRect = bitChoices[selectedIndex].rect;
        Vector3 startScale = selectedRect.localScale;
        Vector3 targetScale = startScale * 1.15f;
        float startAlpha = mainCanvasGroup.alpha;

        float elapsed = 0;
        float duration = 0.5f; // 전체 종료 시간

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            // SmoothStep으로 더 쫀득하게 스케일 업
            float smoothT = t * t * (3f - 2f * t);
            selectedRect.localScale = Vector3.Lerp(startScale, targetScale, smoothT);

            // 전체 알파값 서서히 제거
            if (mainCanvasGroup != null) mainCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0, t);

            yield return null;
        }

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
    public void SetupBitChoices() { usedArtifacts.Clear(); for (int i = 0; i < bitChoices.Length; i++) { ArtifactData artifact = GetRandomArtifactByProbability(); if (artifact != null) usedArtifacts.Add(artifact); ApplyArtifactToChoice(bitChoices[i], artifact); } }
    private ArtifactData GetRandomArtifactByProbability() { float total = gradeProbability.common + gradeProbability.rare + gradeProbability.epic + gradeProbability.legendary; float rand = Random.value * total; if (rand < gradeProbability.common) return GetRandomArtifactByGrade(ArtifactGrade.Common); rand -= gradeProbability.common; if (rand < gradeProbability.rare) return GetRandomArtifactByGrade(ArtifactGrade.Rare); rand -= gradeProbability.rare; if (rand < gradeProbability.epic) return GetRandomArtifactByGrade(ArtifactGrade.Epic); return GetRandomArtifactByGrade(ArtifactGrade.Legendary); }
    private ArtifactData GetRandomArtifactByGrade(ArtifactGrade grade) { List<ArtifactData> candidates = new List<ArtifactData>(); foreach (var artifact in allArtifacts) { if (artifact.grade == grade && !usedArtifacts.Contains(artifact)) candidates.Add(artifact); } return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null; }
    private void ApplyArtifactToChoice(BitChoices choice, ArtifactData artifact) { if (artifact == null) return; choice.artifactImage.sprite = artifact.icon; choice.titleText.text = artifact.artifactName; choice.gradeText.text = "[ " + artifact.grade.ToString() + " ]"; choice.desText.text = artifact.description; foreach (var effect in choice.gradeEffects) effect.Color = GetColorByGrade(artifact.grade); }
    private Color GetColorByGrade(ArtifactGrade grade) { switch (grade) { case ArtifactGrade.Common: return gradeColors.common; case ArtifactGrade.Rare: return gradeColors.rare; case ArtifactGrade.Epic: return gradeColors.epic; case ArtifactGrade.Legendary: return gradeColors.legendary; default: return Color.white; } }

    [System.Serializable]
    public struct BitChoices
    {
        public RectTransform rect;
        public Image hoverSensor;
        public CanvasGroup group;
        public GlowFilter choiceEffect;
        public GlowFilter[] gradeEffects;
        public Image artifactImage;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI gradeText;
        public TextMeshProUGUI desText;
        [HideInInspector] public Vector2 initialAnchoredPos;
    }

    [System.Serializable] public struct ArtifactGradeProbability { [Range(0f, 1f)] public float common, rare, epic, legendary; }
    [System.Serializable] public struct ArtifactGradeColor { public Color common, rare, epic, legendary; }
}