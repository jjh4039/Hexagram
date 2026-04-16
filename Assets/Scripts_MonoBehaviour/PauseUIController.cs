using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PauseUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseRoot;           // 일시정지 최상위 오브젝트
    [SerializeField] private RectTransform bgRect;           // 크기 애니메이션용 배경
    [SerializeField] private CanvasGroup textGroup;          // 페이드용 텍스트 그룹
    [SerializeField] private TextMeshProUGUI[] menuTexts;    // 선택지 텍스트 배열
    [SerializeField] private TextMeshProUGUI progressText;   // 진행도 텍스트 UI
    [SerializeField] private TextMeshProUGUI playTimeText;   // 플레이타임 텍스트 UI

    [Header("Animation Settings")]
    [SerializeField] private float targetBgHeight = 400f;    // 배경 최대 높이
    [SerializeField] private float bgExpandDuration = 0.25f; // 배경 확장 시간
    [SerializeField] private float textFadeDuration = 0.2f;  // 텍스트 페이드 시간

    [Header("Floating Settings")]
    [SerializeField] private float floatAmplitude = 10f;     // 부유 진폭
    [SerializeField] private float floatSpeed = 2f;          // 부유 속도

    [Header("Colors & Sounds")]
    [SerializeField] private Color normalColor = Color.gray; // 비활성 색상
    [SerializeField] private Color selectColor = Color.white;// 선택 색상
    [SerializeField] private AudioClip sfxMove;              // 이동 사운드
    [SerializeField] private AudioClip sfxSubmit;            // 결정 사운드
    [SerializeField] private AudioClip sfxOpen;              // 메뉴 오픈 사운드

    private bool _isPaused = false;                          // 일시정지 상태 여부
    private bool _isAnimating = false;                       // 애니메이션 진행 여부
    private int _currentIndex = 0;                           // 현재 선택된 메뉴 인덱스

    private Coroutine _animCoroutine;                        // 애니메이션 코루틴 캐싱
    private Vector2 _bgOriginAnchoredPos;                    // 배경 초기 위치

    private void Start()
    {
        if (pauseRoot != null) pauseRoot.SetActive(false);
        if (bgRect != null) _bgOriginAnchoredPos = bgRect.anchoredPosition;

        if (InputStateManager.Instance != null)
        {
            var actions = InputStateManager.Instance.Actions;

            actions.Normal.Pause.performed += OnPauseToggleInput;
            actions.Combat.Pause.performed += OnPauseToggleInput;

            actions.UI.MoveUI.performed += OnNavigate;
            actions.UI.Select.performed += OnSubmit;
            actions.UI.CloseUI.performed += OnCloseUI;
        }
    }

    private void OnDestroy()
    {
        if (InputStateManager.Instance != null)
        {
            var actions = InputStateManager.Instance.Actions;
            actions.Normal.Pause.performed -= OnPauseToggleInput;
            actions.Combat.Pause.performed -= OnPauseToggleInput;

            actions.UI.MoveUI.performed -= OnNavigate;
            actions.UI.Select.performed -= OnSubmit;
            actions.UI.CloseUI.performed -= OnCloseUI;
        }
    }

    private void Update()
    {
        if (_isPaused && bgRect != null)
        {
            float newY = _bgOriginAnchoredPos.y + Mathf.Sin(Time.unscaledTime * floatSpeed) * floatAmplitude;
            bgRect.anchoredPosition = new Vector2(_bgOriginAnchoredPos.x, newY);
        }
    }

    private void OnPauseToggleInput(InputAction.CallbackContext ctx)
    {
        if (_isAnimating) return;

        if (!_isPaused) PauseGame();
        else ResumeGame();
    }

    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        if (!_isPaused || _isAnimating) return;
        Vector2 input = ctx.ReadValue<Vector2>();

        if (input.y > 0.5f) ChangeSelection(-1);
        else if (input.y < -0.5f) ChangeSelection(1);
    }

    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (!_isPaused || _isAnimating) return;
        ExecuteSelection();
    }

    private void OnCloseUI(InputAction.CallbackContext ctx)
    {
        if (!_isPaused || _isAnimating) return;
        ResumeGame();
    }

    private void PauseGame()
    {
        if (InputStateManager.Instance != null)
            InputStateManager.Instance.ChangeInputState(InputState.UI);

        _isPaused = true;
        _currentIndex = 0;

        UpdateSelectionVisuals();
        UpdateInfoTexts();

        if (sfxOpen) SoundManager.instance.PlaySFX(sfxOpen, 0.6f);

        Time.timeScale = 0f;

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateOpen());
    }

    private void ResumeGame()
    {
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateClose());
    }

    private void ChangeSelection(int dir)
    {
        _currentIndex = (_currentIndex + dir + menuTexts.Length) % menuTexts.Length;
        if (sfxMove) SoundManager.instance.PlaySFX(sfxMove, 0.5f);
        UpdateSelectionVisuals();
    }

    private void UpdateSelectionVisuals()
    {
        for (int i = 0; i < menuTexts.Length; i++)
        {
            if (menuTexts[i] == null) continue;
            bool isSelected = (i == _currentIndex);
            menuTexts[i].color = isSelected ? selectColor : normalColor;
            menuTexts[i].rectTransform.localScale = isSelected ? Vector3.one * 1.1f : Vector3.one;
        }
    }

    private void UpdateInfoTexts()
    {
        if (GameManager.instance == null) return;

        string seasonStr = GameManager.instance.currentSeason switch
        {
            Season.Spring => "봄",
            Season.Summer => "여름",
            Season.Autumn => "가을",
            Season.Winter => "겨울",
            _ => ""
        };

        if (progressText != null)
        {
            progressText.text = $"진행도 : {seasonStr} - {GameManager.instance.currentProgress}%";
        }

        if (playTimeText != null)
        {
            int min = Mathf.FloorToInt(GameManager.instance.currentPlayTime / 60f);
            int sec = Mathf.FloorToInt(GameManager.instance.currentPlayTime % 60f);
            playTimeText.text = $"PlayTime : {min}m {sec}s";
        }
    }

    private void ExecuteSelection()
    {
        if (sfxSubmit) SoundManager.instance.PlaySFX(sfxSubmit, 0.6f);

        switch (_currentIndex)
        {
            case 0: ResumeGame(); break;
            case 1: Debug.Log("Settings"); break;
            case 2: Debug.Log("Give Up"); break;
            case 3: Debug.Log("Quit"); break;
        }
    }

    private IEnumerator AnimateOpen()
    {
        _isAnimating = true;
        pauseRoot.SetActive(true);

        bgRect.sizeDelta = new Vector2(bgRect.sizeDelta.x, 0f);
        textGroup.alpha = 0f;

        float elapsed = 0f;

        while (elapsed < bgExpandDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / bgExpandDuration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            bgRect.sizeDelta = new Vector2(bgRect.sizeDelta.x, Mathf.Lerp(0f, targetBgHeight, easedT));
            yield return null;
        }
        bgRect.sizeDelta = new Vector2(bgRect.sizeDelta.x, targetBgHeight);

        elapsed = 0f;
        while (elapsed < textFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            textGroup.alpha = Mathf.Clamp01(elapsed / textFadeDuration);
            yield return null;
        }
        textGroup.alpha = 1f;

        _isAnimating = false;
    }

    private IEnumerator AnimateClose()
    {
        _isAnimating = true;

        float elapsed = 0f;
        float startHeight = bgRect.sizeDelta.y;

        while (elapsed < textFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            textGroup.alpha = 1f - Mathf.Clamp01(elapsed / textFadeDuration);
            yield return null;
        }
        textGroup.alpha = 0f;

        elapsed = 0f;
        while (elapsed < bgExpandDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / bgExpandDuration);
            float easedT = t * t * t;
            bgRect.sizeDelta = new Vector2(bgRect.sizeDelta.x, Mathf.Lerp(startHeight, 0f, easedT));
            yield return null;
        }

        pauseRoot.SetActive(false);
        bgRect.anchoredPosition = _bgOriginAnchoredPos;

        Time.timeScale = 1f;

        if (InputStateManager.Instance != null)
            InputStateManager.Instance.CloseUI();

        _isPaused = false;
        _isAnimating = false;
    }
}