using ChocDino.UIFX;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BitManager : MonoBehaviour
{
    public static BitManager Instance; // 전역 접근용 인스턴스

    [SerializeField] private Image backgroundImage; // 배경 이미지

    [Header("Background Loop Settings")] [SerializeField]
    private float bgScaleMin = 0.9f;

    [SerializeField] private float bgScaleMax = 1.1f;
    [SerializeField] private float bgRotationRange = 3f;
    [SerializeField] private float bgAnimSpeed = 0.3f;

    [SerializeField] private BitChoices[] bitChoices; // 카드 슬롯 배열
    [SerializeField] private ArtifactData[] allArtifacts; // 전체 아티팩트 데이터베이스
    [SerializeField] private ArtifactGradeProbability gradeProbability; // 등급별 확률 설정
    [SerializeField] private ArtifactGradeColor gradeColors; // 등급별 강조 색상
    [SerializeField] private CanvasGroup mainCanvasGroup; // 전체 UI 투명도 관리

    [Header("Confirm Button Settings")] [SerializeField]
    private Image confirmButtonImage; // 확정 버튼

    [SerializeField] private float buttonYOffset = -155f; // 버튼 수직 위치 오프셋

    [Header("Selection Arrow Settings")] [SerializeField]
    private Image selectionArrow; // 선택 표시 화살표

    [SerializeField] private float arrowYOffset = 180f; // 카드 기준 화살표 기본 높이 오프셋
    [SerializeField] private float arrowBounceHeight = 15f; // 위아래 이동 폭
    [SerializeField] private float arrowBounceSpeed = 8f; // 바운스 속도

    [Header("Animation Settings")] [SerializeField]
    private float hoverYOffset = 15f; // 호버 시 올라가는 높이

    [SerializeField] private float animationSpeed = 10f; // 애니메이션 부드러움 정도
    [SerializeField] private float introDelay = 0.3f; // 카드 순차 등장 간격

    [Header("Audio")] [SerializeField] private AudioClip sfxIntro; // 등장 사운드
    [SerializeField] private AudioClip sfxSelect; // 카드 선택 사운드
    [SerializeField] private AudioClip sfxDecision; // 최종 확정 사운드

    private readonly HashSet<ArtifactData> _usedArtifacts = new HashSet<ArtifactData>(); // 중복 방지용 셋
    private Coroutine[] _hoverCoroutines; // 각 카드 호버 코루틴
    private bool[] _isHovering; // 각 카드 호버 상태
    private int _selectedIndex = -1; // 현재 선택된 카드 인덱스
    private bool _isInitialized = false; // 조작 가능 여부

    void Awake()
    {
        Instance = this;
        _hoverCoroutines = new Coroutine[bitChoices.Length];
        _isHovering = new bool[bitChoices.Length];

        if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(false);
        if (selectionArrow != null) selectionArrow.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0;

        for (int i = 0; i < bitChoices.Length; i++)
        {
            if (bitChoices[i].group != null) bitChoices[i].group.alpha = 0;
            if (bitChoices[i].rect != null)
                bitChoices[i].initialAnchoredPos = bitChoices[i].rect.anchoredPosition;
        }

        gameObject.SetActive(false); // 시작 시 비활성화
    }

    // 아이템 상호작용 시 호출되는 UI 오픈 함수
    public void OpenBitUI()
    {
        // 매니저에게 조작 상태 전환 요청
        if (InputStateManager.Instance != null && !InputStateManager.Instance.TryOpenUI()) return;

        _isInitialized = false;
        _selectedIndex = -1;
        _usedArtifacts.Clear();

        if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(false);
        if (selectionArrow != null) selectionArrow.gameObject.SetActive(false);

        for (int i = 0; i < bitChoices.Length; i++)
        {
            _isHovering[i] = false;
            if (_hoverCoroutines[i] != null) StopCoroutine(_hoverCoroutines[i]);
            bitChoices[i].rect.anchoredPosition = bitChoices[i].initialAnchoredPos;
            bitChoices[i].rect.localScale = Vector3.one;

            if (bitChoices[i].group != null)
                bitChoices[i].group.alpha = 0;
        }

        SetupBitChoices(); // 카드 데이터 할당

        Time.timeScale = 0f; // 게임 일시 정지
        if (sfxIntro != null && SoundManager.instance != null)
            SoundManager.instance.PlaySFX(sfxIntro, 0.15f);

        StopAllCoroutines();
        gameObject.SetActive(true);
        if (backgroundImage != null) StartCoroutine(LoopBackgroundAnimation());
        StartCoroutine(SequenceIntro()); // 등장 연출 시작
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
        _isInitialized = true;
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
        if (!_isInitialized) return;
        CheckMouseHover();
        CheckMouseClick();
        UpdateArrowAnimation(); // 화살표 바운스 애니메이션 처리
    }

    private void UpdateArrowAnimation()
    {
        if (selectionArrow == null || _selectedIndex == -1) return;

        RectTransform arrowRect = selectionArrow.rectTransform;
        RectTransform cardRect = bitChoices[_selectedIndex].rect;

        arrowRect.position = cardRect.position;

        Vector2 anchored = arrowRect.anchoredPosition;
        float bounce = Mathf.Sin(Time.unscaledTime * arrowBounceSpeed) * arrowBounceHeight;
        anchored.y += arrowYOffset + bounce;
        arrowRect.anchoredPosition = anchored;
    }

    private void CheckMouseHover()
    {
        if (_selectedIndex != -1) return;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        for (int i = 0; i < bitChoices.Length; i++)
        {
            bool overlaps =
                RectTransformUtility.RectangleContainsScreenPoint(bitChoices[i].hoverSensor.rectTransform, mousePos,
                    null);
            if (overlaps && !_isHovering[i])
            {
                _isHovering[i] = true;
                StartHoverAnimation(i, true);
            }
            else if (!overlaps && _isHovering[i])
            {
                _isHovering[i] = false;
                StartHoverAnimation(i, false);
            }
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
                if (RectTransformUtility.RectangleContainsScreenPoint(bitChoices[i].hoverSensor.rectTransform, mousePos,
                        null))
                {
                    if (sfxSelect != null && SoundManager.instance != null)
                        SoundManager.instance.PlaySFX(sfxSelect, 0.3f);
                    SelectCard(i);
                    break;
                }
            }
        }
    }

    private void SelectCard(int newIndex)
    {
        if (_selectedIndex == newIndex) return;

        if (_selectedIndex != -1)
        {
            int prev = _selectedIndex;
            _isHovering[prev] = false;
            StartHoverAnimation(prev, false);
        }

        _selectedIndex = newIndex;
        _isHovering[_selectedIndex] = true;

        if (selectionArrow != null) selectionArrow.gameObject.SetActive(true); // 선택 시 화살표 활성화

        StartHoverAnimation(_selectedIndex, true);
        UpdateConfirmButtonPosition();
    }

    private void UpdateConfirmButtonPosition()
    {
        if (confirmButtonImage == null || _selectedIndex == -1) return;
        confirmButtonImage.gameObject.SetActive(true);
        RectTransform btnRect = confirmButtonImage.rectTransform;

        btnRect.position = bitChoices[_selectedIndex].hoverSensor.transform.position;
        Vector2 anchored = btnRect.anchoredPosition;
        anchored.y = buttonYOffset;
        btnRect.anchoredPosition = anchored;
    }

    public void OnConfirmButtonClick()
    {
        if (_selectedIndex == -1) return;

        ArtifactData selectedArtifact = bitChoices[_selectedIndex].currentArtifact;
        if (selectedArtifact != null && ArtifactManager.Instance != null)
        {
            ArtifactManager.Instance.AddArtifact(selectedArtifact); // 획득 처리
        }

        StartCoroutine(ExitSequence());
    }

    private IEnumerator ExitSequence()
    {
        _isInitialized = false;

        if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(false);
        if (selectionArrow != null) selectionArrow.gameObject.SetActive(false); // 확정 시 화살표 숨김

        for (int i = 0; i < bitChoices.Length; i++)
        {
            if (i != _selectedIndex) StartCoroutine(FadeOutCanvasGroup(bitChoices[i].group, 0.2f));
        }

        RectTransform selectedRect = bitChoices[_selectedIndex].rect;
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

        Time.timeScale = 1.0f; // 시간 복구
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
        if (_hoverCoroutines[index] != null) StopCoroutine(_hoverCoroutines[index]);
        Vector2 basePos = bitChoices[index].initialAnchoredPos;
        Vector2 targetPos = isEntering ? basePos + new Vector2(0, hoverYOffset) : basePos;
        _hoverCoroutines[index] = StartCoroutine(AnimateCard(index, targetPos));
    }

    private IEnumerator AnimateCard(int index, Vector2 targetAnchoredPos)
    {
        RectTransform rect = bitChoices[index].rect;
        while (Vector2.Distance(rect.anchoredPosition, targetAnchoredPos) > 0.1f)
        {
            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetAnchoredPos,
                Time.unscaledDeltaTime * animationSpeed);
            yield return null;
        }

        rect.anchoredPosition = targetAnchoredPos;
        _hoverCoroutines[index] = null;
    }

    public void SetupBitChoices()
    {
        _usedArtifacts.Clear();
        for (int i = 0; i < bitChoices.Length; i++)
        {
            ArtifactData artifact = GetRandomArtifactByProbability();
            bitChoices[i].currentArtifact = artifact;

            if (artifact != null)
            {
                _usedArtifacts.Add(artifact);
                ApplyArtifactToChoice(bitChoices[i], artifact);
            }
            else
            {
                // 더 이상 획득할 아티팩트가 완전히 없을 경우 카드를 숨김 처리
                if (bitChoices[i].group != null) bitChoices[i].group.alpha = 0;
            }
        }
    }

    private ArtifactData GetRandomArtifactByProbability()
    {
        ArtifactGrade targetGrade = RollGrade(); // 목표 등급 추첨
        ArtifactData selected = GetRandomArtifactByGrade(targetGrade); // 해당 등급 아티팩트 탐색

        // 해당 등급이 모두 소진되었다면 다른 등급에서 대체 탐색
        if (selected == null)
        {
            selected = GetFallbackArtifact();
        }

        return selected;
    }

    private ArtifactGrade RollGrade() // 확률에 따른 목표 등급 계산
    {
        float total = gradeProbability.common + gradeProbability.rare + gradeProbability.epic +
                      gradeProbability.legendary;
        float rand = Random.value * total;

        if (rand < gradeProbability.common) return ArtifactGrade.Common;
        rand -= gradeProbability.common;
        if (rand < gradeProbability.rare) return ArtifactGrade.Rare;
        rand -= gradeProbability.rare;
        if (rand < gradeProbability.epic) return ArtifactGrade.Epic;
        return ArtifactGrade.Legendary;
    }

    private ArtifactData GetRandomArtifactByGrade(ArtifactGrade grade)
    {
        List<ArtifactData> candidates = new List<ArtifactData>();
        foreach (var artifact in allArtifacts)
        {
            if (artifact.grade == grade && IsArtifactAvailable(artifact))
            {
                candidates.Add(artifact);
            }
        }

        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    private ArtifactData GetFallbackArtifact() // 목표 등급 고갈 시 대체 아티팩트 탐색
    {
        List<ArtifactData> candidates = new List<ArtifactData>();
        foreach (var artifact in allArtifacts)
        {
            if (IsArtifactAvailable(artifact))
            {
                candidates.Add(artifact);
            }
        }

        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    private bool IsArtifactAvailable(ArtifactData artifact) // 획득 및 제시 가능 여부 확인
    {
        if (_usedArtifacts.Contains(artifact)) return false; // 이번 선택지에 이미 올라갔는지 확인

        if (ArtifactManager.Instance != null && ArtifactManager.Instance.myArtifacts.Contains(artifact))
            return false; // 플레이어가 이미 소지하고 있는지 확인

        return true;
    }

    private void ApplyArtifactToChoice(BitChoices choice, ArtifactData artifact)
    {
        if (artifact == null) return;
        choice.artifactImage.sprite = artifact.icon;
        choice.titleText.text = artifact.artifactName;
        choice.gradeText.text = "[ " + artifact.grade.ToString() + " ]";
        choice.desText.text = artifact.description;

        // Glow 효과가 지워졌더라도 에러가 나지 않도록 null 체크 추가
        foreach (var effect in choice.gradeEffects)
        {
            if (effect != null) effect.Color = GetColorByGrade(artifact.grade);
        }

        if (choice.outLineImage != null)
            choice.outLineImage.color = GetOutLineColor(artifact.grade);
        if (choice.outLineCaseImage != null)
            choice.outLineCaseImage.color = GetOutLineColor(artifact.grade);
    }

    private Color GetOutLineColor(ArtifactGrade grade)
    {
        switch (grade)
        {
            case ArtifactGrade.Common: return Color.white;
            case ArtifactGrade.Rare: return new Color(0f, 0.65f, 1f);
            case ArtifactGrade.Epic: return new Color(0.6f, 0f, 1f);
            case ArtifactGrade.Legendary: return new Color(0f, 0.68f, 0.24f);
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
        public Image outLineCaseImage;
        public GlowFilter[] gradeEffects;
        public Image artifactImage;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI gradeText;
        public TextMeshProUGUI desText;
        [HideInInspector] public Vector2 initialAnchoredPos;
        [HideInInspector] public ArtifactData currentArtifact;
    }

    [System.Serializable]
    public struct ArtifactGradeProbability
    {
        [Range(0f, 1f)] public float common, rare, epic, legendary;
    }

    [System.Serializable]
    public struct ArtifactGradeColor
    {
        public Color common, rare, epic, legendary;
    }
}