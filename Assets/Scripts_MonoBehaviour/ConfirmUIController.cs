using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class ConfirmUIController : MonoBehaviour
{
    public static ConfirmUIController Instance;            

    public bool IsOpen => _isOpen;                         

    [Header("UI References")]
    [SerializeField] private GameObject visualRoot;        
    [SerializeField] private CanvasGroup visualGroup;      
    [SerializeField] private RectTransform bgRect;         
    
    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI titleText;    
    [SerializeField] private TextMeshProUGUI messageText;  
    [SerializeField] private TextMeshProUGUI yesText;      
    [SerializeField] private TextMeshProUGUI noText;       

    [Header("Animation Settings")]
    [SerializeField] private float targetBgScale = 1f;     
    [SerializeField] private float animDuration = 0.2f;    

    [Header("Colors & Sounds")]
    [SerializeField] private Color normalColor = Color.gray; 
    [SerializeField] private Color selectColor = Color.white;
    [SerializeField] private AudioClip sfxMove;            
    [SerializeField] private AudioClip sfxConfirm;         
    [SerializeField] private AudioClip sfxCancel;          

    private bool _isOpen;                          
    private bool _isAnimating;                     
    private int _currentIndex = 1;                         

    private Action _onConfirmAction;                       
    private Action _onCancelAction;                        

    private Coroutine _animCoroutine;                      
    private bool _isSubscribed; // ★ 수정: 이벤트 중복 해제 방지용 플래그

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (visualRoot != null) visualRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        // 씬 전환 시 에러 방지
        UnsubscribeInputs();
    }

    public void ShowPopupByIndex(int popupIndex, Action additionalAction = null)
    {
        string title;
        string msg;
        Action finalConfirmAction ;

        switch (popupIndex)
        {
            case 0: 
                title = "게임 포기";
                msg = "정말로 이번 도전을 포기하시겠습니까?";
                finalConfirmAction = () => 
                {
                    additionalAction?.Invoke();             
                    Time.timeScale = 1f;                    
                    if (GameManager.instance != null && GameManager.instance.player != null)
                    {
                        GameManager.instance.player.OnDie(); 
                    }
                };
                break;
                
            case 1: 
                title = "게임 종료";
                msg = "게임을 완전히 종료하시겠습니까?";
                finalConfirmAction = () => 
                {
                    additionalAction?.Invoke();             
                    #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
                    #else
                        Application.Quit();
                    #endif
                };
                break;
                
            default: 
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
        
        if (InputStateManager.Instance != null && !_isSubscribed)
        {
            var actions = InputStateManager.Instance.Actions.UI;
            actions.MoveUI.performed += OnNavigate;
            actions.CloseUI.performed += OnCloseUI;
            actions.Select.performed += OnSubmit;
            _isSubscribed = true;
        }

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateOpen());
    }

    private void ClosePopup()
    {
        if (!_isOpen || _isAnimating) return;

        UnsubscribeInputs();

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateClose());
    }

    private void UnsubscribeInputs()
    {
        if (_isSubscribed && InputStateManager.Instance != null && InputStateManager.Instance.Actions != null)
        {
            var actions = InputStateManager.Instance.Actions.UI;
            actions.MoveUI.performed -= OnNavigate;
            actions.CloseUI.performed -= OnCloseUI;
            actions.Select.performed -= OnSubmit;
            _isSubscribed = false;
        }
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
        if (visualGroup) visualGroup.alpha = 0f;

        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            
            bgRect.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one * targetBgScale, easedT);
            if (visualGroup) visualGroup.alpha = t;
            
            yield return null;
        }

        bgRect.localScale = Vector3.one * targetBgScale;
        if (visualGroup) visualGroup.alpha = 1f;

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
            if (visualGroup) visualGroup.alpha = 1f - t;
            
            yield return null;
        }

        visualRoot.SetActive(false);
        _isOpen = false;
        _isAnimating = false;
    }
}