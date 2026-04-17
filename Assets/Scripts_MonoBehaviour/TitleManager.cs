using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TitleManager : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private CanvasGroup titleLogo;         // 로고 투명도 제어
    [SerializeField] private RectTransform titleRect;       // 로고 위치 제어
    [SerializeField] private CanvasGroup textGroup;         // 텍스트 투명도 제어
    [SerializeField] private Transform background;          // 마우스 연동 배경
    [SerializeField] private TextMeshProUGUI[] menuTexts;   // 메뉴 텍스트 배열
    [SerializeField] private GameObject[] cursorIcons;      // 선택 아이콘 배열

    [Header("Animation Settings")]
    [SerializeField] private float animTime = 1f;           // 연출 소요 시간
    [SerializeField] private float delayBetween = 0.5f;     // 텍스트 등장 대기 시간

    [Header("Idle Floating Settings")]
    [SerializeField] private float floatSpeed = 2f;         // 부유 속도
    [SerializeField] private float floatAmount = 10f;       // 부유 폭

    [Header("Parallax Settings")]
    [SerializeField] private float parallaxLimit = 20f;     // 마우스 기준 최대 이동 반경
    [SerializeField] private float parallaxSmooth = 3f;     // 배경 보간 속도
    [SerializeField] private float autoPanSpeed = 0.5f;     // 자동 이동 속도
    [SerializeField] private float autoPanAmount = 10f;     // 자동 이동 폭

    [Header("Color Settings")]
    [SerializeField] private Color normalColor = Color.gray;   // 기본 텍스트 색상
    [SerializeField] private Color highlightColor = Color.white; // 선택 텍스트 색상

    [Header("Sound Settings")]
    [SerializeField] private AudioClip moveSound;           // 이동 효과음
    [SerializeField] private AudioClip selectSound;         // 선택 효과음

    private PlayerInput _inputActions;                      // 입력 시스템
    private int _currentIndex = 0;                          // 현재 선택 메뉴
    private Vector3 _bgOriginPos;                           // 배경 초기 좌표
    private float _baseY;                                   // 로고 기본 Y좌표
    private bool _isInputActive = false;                    // 조작 가능 상태
    private bool _isFloatingActive = false;                 // 부유 효과 상태
    private float _introTimer = 0f;                         // 부유 효과 타이머

    private void Awake()
    {
        _inputActions = new PlayerInput();
        if (background != null) _bgOriginPos = background.position;

        normalColor.a = 1f;
        highlightColor.a = 1f;

        if (titleLogo != null) titleLogo.alpha = 0f;
        if (textGroup != null) textGroup.alpha = 0f;
        if (titleRect != null) _baseY = titleRect.anchoredPosition.y;
    }

    private void OnEnable()
    {
        _inputActions.UI.Enable();
        _inputActions.UI.MoveUI.performed += OnMoveInput;
        _inputActions.UI.Select.performed += OnSelectInput;
    }

    private void OnDisable()
    {
        _inputActions.UI.MoveUI.performed -= OnMoveInput;
        _inputActions.UI.Select.performed -= OnSelectInput;
        _inputActions.UI.Disable();
    }

    private void Start()
    {
        UpdateMenuVisuals();
        _isFloatingActive = true;
        StartCoroutine(IntroSequence());
    }

    private void Update()
    {
        HandleParallaxBackground();
        HandleIdleFloating();
    }

    private IEnumerator IntroSequence()
    {
        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime / animTime;
            titleLogo.alpha = Mathf.Lerp(0f, 1f, timer);
            yield return null;
        }

        yield return new WaitForSeconds(delayBetween);

        timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime / (animTime * 0.5f);
            textGroup.alpha = Mathf.Lerp(0f, 1f, timer);
            yield return null;
        }

        _isInputActive = true;
    }

    private void HandleParallaxBackground()
    {
        if (background == null) return;

        // 1. 마우스 위치 기반 이동값 계산 (마우스가 없을 경우 대비)
        Vector2 mouseOffset = Vector2.zero;
        if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            mouseOffset.x = (mousePos.x - (Screen.width / 2f)) / (Screen.width / 2f) * parallaxLimit;
            mouseOffset.y = (mousePos.y - (Screen.height / 2f)) / (Screen.height / 2f) * parallaxLimit;
        }

        // 2. 시간에 따른 자동 유영(Auto Pan) 이동값 계산
        // X축과 Y축의 속도를 미세하게 다르게 주어 단순 반복이 아닌 유기적인 움직임 연출
        float autoX = Mathf.Sin(Time.time * autoPanSpeed) * autoPanAmount;
        float autoY = Mathf.Cos(Time.time * autoPanSpeed * 0.8f) * autoPanAmount;

        // 3. 마우스 이동값과 자동 이동값을 합쳐서 목표 위치 설정
        Vector3 targetPos = _bgOriginPos + new Vector3(mouseOffset.x + autoX, mouseOffset.y + autoY, 0f);

        // 부드럽게 보간하며 이동
        background.position = Vector3.Lerp(background.position, targetPos, Time.deltaTime * parallaxSmooth);
    }

    private void HandleIdleFloating()
    {
        if (_isFloatingActive && titleRect != null)
        {
            _introTimer += Time.deltaTime;
            float floatingY = _baseY + (Mathf.Sin(_introTimer * floatSpeed) * floatAmount);
            titleRect.anchoredPosition = new Vector2(titleRect.anchoredPosition.x, floatingY);
        }
    }

    private void OnMoveInput(InputAction.CallbackContext context)
    {
        if (!_isInputActive) return;

        Vector2 moveDir = context.ReadValue<Vector2>();

        if (moveDir.x > 0.5f) ChangeIndex(1);
        else if (moveDir.x < -0.5f) ChangeIndex(-1);
    }

    private void OnSelectInput(InputAction.CallbackContext context)
    {
        if (!_isInputActive) return;
        ExecuteMenu();
    }

    public void SetIndexByMouse(int index)
    {
        if (!_isInputActive || _currentIndex == index) return;

        _currentIndex = index;
        PlayMoveSound();
        UpdateMenuVisuals();
    }

    private void ChangeIndex(int direction)
    {
        _currentIndex += direction;

        if (_currentIndex < 0) _currentIndex = menuTexts.Length - 1;
        if (_currentIndex >= menuTexts.Length) _currentIndex = 0;

        PlayMoveSound();
        UpdateMenuVisuals();
    }

    private void PlayMoveSound()
    {
        if (SoundManager.instance != null && moveSound != null)
        {
            SoundManager.instance.PlaySFX(moveSound);
        }
    }

    private void UpdateMenuVisuals()
    {
        for (int i = 0; i < menuTexts.Length; i++)
        {
            if (i == _currentIndex)
            {
                if (menuTexts[i] != null) menuTexts[i].color = highlightColor;
                if (cursorIcons.Length > i && cursorIcons[i] != null) cursorIcons[i].SetActive(true);
            }
            else
            {
                if (menuTexts[i] != null) menuTexts[i].color = normalColor;
                if (cursorIcons.Length > i && cursorIcons[i] != null) cursorIcons[i].SetActive(false);
            }
        }
    }

    public void ExecuteMenu()
    {
        if (!_isInputActive) return;

        if (SoundManager.instance != null && selectSound != null)
        {
            SoundManager.instance.PlaySFX(selectSound);
        }

        Debug.Log("실행됨 메뉴 번호 " + _currentIndex);

        if (_currentIndex == 0)
        {
            // 게임 시작
        }
        else if (_currentIndex == 1)
        {
            // 설정 창 띄우기
        }
        else if (_currentIndex == 2)
        {
            // 게임 종료
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}