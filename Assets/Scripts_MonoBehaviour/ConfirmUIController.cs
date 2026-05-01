using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class ConfirmUIController : MonoBehaviour
{
    public static ConfirmUIController instance;            // 전역 접근용 싱글톤

    public bool IsOpen => _isOpen;                         // 외부 상태 참조용

    [Header("UI References")]
    [SerializeField] private GameObject visualRoot;        // 껐다 켤 시각적 최상위 객체
    [SerializeField] private CanvasGroup visualGroup;      // 전체 페이드용 그룹
    [SerializeField] private RectTransform bgRect;         // 크기 애니메이션용 배경
    
    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI titleText;    // 팝업 제목 텍스트
    [SerializeField] private TextMeshProUGUI messageText;  // 팝업 본문 텍스트
    [SerializeField] private TextMeshProUGUI yesText;      // 수락 버튼 텍스트
    [SerializeField] private TextMeshProUGUI noText;       // 거절 버튼 텍스트

    [Header("Animation Settings")]
    [SerializeField] private float targetBgScale = 1f;     // 배경 최대 스케일
    [SerializeField] private float animDuration = 0.2f;    // 팝업 애니메이션 시간

    [Header("Colors & Sounds")]
    [SerializeField] private Color normalColor = Color.gray; // 비활성 색상
    [SerializeField] private Color selectColor = Color.white;// 선택 색상
    [SerializeField] private AudioClip sfxMove;            // 이동 사운드
    [SerializeField] private AudioClip sfxConfirm;         // 수락 사운드
    [SerializeField] private AudioClip sfxCancel;          // 취소/거절 사운드

    private bool _isOpen = false;                          // 팝업 열림 여부
    private bool _isAnimating = false;                     // 애니메이션 진행 여부
    private int _currentIndex = 1;                         // 0: Yes, 1: No (기본값 No)

    private Action _onConfirmAction;                       // 수락 시 실행될 함수 캐싱
    private Action _onCancelAction;                        // 거절 시 실행될 함수 캐싱

    private Coroutine _animCoroutine;                      // 애니메이션 코루틴 캐싱

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (visualRoot != null) visualRoot.SetActive(false);
    }

    // [핵심 변경] 인덱스에 따라 로직을 내장 처리하고, 외부에서는 UI 조작 등 추가 동작(additionalAction)만 전달받습니다.
    public void ShowPopupByIndex(int popupIndex, Action additionalAction = null)
    {
        string title = "";
        string msg = "";
        Action finalConfirmAction = null;

        switch (popupIndex)
        {
            case 0: // 인덱스 0: 게임 포기
                title = "게임 포기";
                msg = "정말로 이번 도전을 포기하시겠습니까?";
                finalConfirmAction = () => 
                {
                    additionalAction?.Invoke();             // 외부 UI 닫기 등 추가 동작 실행
                    Time.timeScale = 1f;                    // 일시정지 상태에서 넘어왔을 수 있으므로 강제 복구
                    if (GameManager.instance != null && GameManager.instance.player != null)
                    {
                        GameManager.instance.player.OnDie(); // 실제 사망 연출 실행
                    }
                };
                break;
                
            case 1: // 인덱스 1: 게임 종료
                title = "게임 종료";
                msg = "게임을 완전히 종료하시겠습니까?";
                finalConfirmAction = () => 
                {
                    additionalAction?.Invoke();             // 외부 UI 닫기 등 추가 동작 실행
                    #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
                    #else
                        Application.Quit();
                    #endif
                };
                break;
                
            default: // 예외 처리
                title = "확인";
                msg = "계속 진행하시겠습니까?";
                finalConfirmAction = () => { additionalAction?.Invoke(); };
                break;
        }

        ShowPopup(title, msg, finalConfirmAction);
    }

    public void ShowPopup(string title, string message, Action onConfirm, Action onCancel = null, string yesStr = "예", string noStr = "아니오")
    {
        if (_isOpen || _isAnimating) return;

        if (titleText != null) titleText.text = title;
        if (messageText != null) messageText.text = message;
        if (yesText != null) yesText.text = yesStr;
        if (noText != null) noText.text = noStr;

        _onConfirmAction = onConfirm;
        _onCancelAction = onCancel;

        _isOpen = true;
        _currentIndex = 1;

        UpdateSelectionVisuals();
        
        if (InputStateManager.Instance != null)
        {
            var actions = InputStateManager.Instance.Actions.UI;
            actions.MoveUI.performed += OnNavigate;
            actions.CloseUI.performed += OnCloseUI;
            actions.Select.performed += OnSubmit;
        }

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateOpen());
    }

    private void ClosePopup()
    {
        if (!_isOpen || _isAnimating) return;

        if (InputStateManager.Instance != null && InputStateManager.Instance.Actions != null)
        {
            var actions = InputStateManager.Instance.Actions.UI;
            actions.MoveUI.performed -= OnNavigate;
            actions.CloseUI.performed -= OnCloseUI;
            actions.Select.performed -= OnSubmit;
        }

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateClose());
    }

    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        if (!_isOpen || _isAnimating) return;
        Vector2 input = ctx.ReadValue<Vector2>();

        if (input.x > 0.5f || input.x < -0.5f)
        {
            _currentIndex = _currentIndex == 0 ? 1 : 0;
            if (sfxMove) SoundManager.instance.PlaySFX(sfxMove, 0.5f);
            UpdateSelectionVisuals();
        }
    }

    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (!_isOpen || _isAnimating) return;

        if (_currentIndex == 0)
        {
            if (sfxConfirm) SoundManager.instance.PlaySFX(sfxConfirm, 0.5f);
            _onConfirmAction?.Invoke();
        }
        else
        {
            if (sfxCancel) SoundManager.instance.PlaySFX(sfxCancel, 0.5f);
            _onCancelAction?.Invoke();
        }

        ClosePopup();
    }

    private void OnCloseUI(InputAction.CallbackContext ctx)
    {
        if (!_isOpen || _isAnimating) return;
        
        if (sfxCancel) SoundManager.instance.PlaySFX(sfxCancel, 0.5f);
        _onCancelAction?.Invoke();
        ClosePopup();
    }

    private void UpdateSelectionVisuals()
    {
        if (yesText != null) yesText.color = (_currentIndex == 0) ? selectColor : normalColor;
        if (noText != null) noText.color = (_currentIndex == 1) ? selectColor : normalColor;
        
        if (yesText != null) yesText.text = (_currentIndex == 0) ? $"> {yesText.text.Replace("> ", "").Replace(" <", "")} <" : yesText.text.Replace("> ", "").Replace(" <", "");
        if (noText != null) noText.text = (_currentIndex == 1) ? $"> {noText.text.Replace("> ", "").Replace(" <", "")} <" : noText.text.Replace("> ", "").Replace(" <", "");
    }

    private IEnumerator AnimateOpen()
    {
        _isAnimating = true;
        visualRoot.SetActive(true);

        bgRect.localScale = Vector3.one * 0.8f;
        if (visualGroup != null) visualGroup.alpha = 0f;

        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            
            bgRect.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one * targetBgScale, easedT);
            if (visualGroup != null) visualGroup.alpha = t;
            
            yield return null;
        }

        bgRect.localScale = Vector3.one * targetBgScale;
        if (visualGroup != null) visualGroup.alpha = 1f;

        _isAnimating = false;
    }

    private IEnumerator AnimateClose()
    {
        _isAnimating = true;

        float elapsed = 0f;
        Vector3 startScale = bgRect.localScale;

        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            float easedT = t * t * t;
            
            bgRect.localScale = Vector3.Lerp(startScale, Vector3.one * 0.8f, easedT);
            if (visualGroup != null) visualGroup.alpha = 1f - t;
            
            yield return null;
        }

        visualRoot.SetActive(false);
        _isOpen = false;
        _isAnimating = false;
    }
}