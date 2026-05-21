using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PauseUIController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("체크 시 튜토리얼 모드로 간주하고 '게임 포기' 버튼을 비활성화합니다.")]
    [SerializeField] private bool isTutorialMode = false; 

    [Header("UI References")]
    [SerializeField] private GameObject pauseRoot;           
    [SerializeField] private RectTransform bgRect;           
    [SerializeField] private CanvasGroup textGroup;          
    [SerializeField] private TextMeshProUGUI[] menuTexts;    
    [SerializeField] private TextMeshProUGUI progressText;   
    [SerializeField] private TextMeshProUGUI playTimeText;   
    [SerializeField] private SettingUIController settingUI;  

    [Header("Animation Settings")]
    [SerializeField] private float targetBgHeight = 400f;    
    [SerializeField] private float bgExpandDuration = 0.25f; 
    [SerializeField] private float textFadeDuration = 0.2f;  

    [Header("Floating Settings")]
    [SerializeField] private float floatAmplitude = 10f;     
    [SerializeField] private float floatSpeed = 2f;          

    [Header("Colors & Sounds")]
    [SerializeField] private Color normalColor = Color.gray; 
    [SerializeField] private Color selectColor = Color.white;
    [SerializeField] private Color disableColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); 
    [SerializeField] private AudioClip sfxMove;              
    [SerializeField] private AudioClip sfxSubmit;            
    [SerializeField] private AudioClip sfxOpen;              

    private bool _isPaused = false;                          
    private bool _isAnimating = false;                       
    private int _currentIndex = 0;                           

    private Coroutine _animCoroutine;                        
    private Vector2 _bgOriginAnchoredPos;                    

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
        if (InputStateManager.Instance != null && InputStateManager.Instance.Actions != null)
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
        if (settingUI != null && settingUI.IsOpen) return;
        if (ConfirmUIController.Instance != null && ConfirmUIController.Instance.IsOpen) return;

        if (!_isPaused) PauseGame();
        else ResumeGame();
    }

    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        if (!_isPaused || _isAnimating) return;
        if (settingUI != null && settingUI.IsOpen) return;
        if (ConfirmUIController.Instance != null && ConfirmUIController.Instance.IsOpen) return;

        Vector2 input = ctx.ReadValue<Vector2>();

        if (input.y > 0.5f) ChangeSelection(-1);
        else if (input.y < -0.5f) ChangeSelection(1);
    }

    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (!_isPaused || _isAnimating) return;
        if (settingUI != null && settingUI.IsOpen) return;
        if (ConfirmUIController.Instance != null && ConfirmUIController.Instance.IsOpen) return;

        ExecuteSelection();
    }

    private void OnCloseUI(InputAction.CallbackContext ctx)
    {
        if (!_isPaused || _isAnimating) return;
        if (settingUI != null && settingUI.IsOpen) return;
        if (ConfirmUIController.Instance != null && ConfirmUIController.Instance.IsOpen) return;

        if (sfxSubmit) SoundManager.instance.PlaySFX(sfxSubmit, 0.2f);
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

        if (sfxOpen) SoundManager.instance.PlaySFX(sfxOpen, 0.2f);

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

        if (isTutorialMode && _currentIndex == 2)
        {
            _currentIndex = (_currentIndex + dir + menuTexts.Length) % menuTexts.Length;
        }

        if (sfxMove) SoundManager.instance.PlaySFX(sfxMove, 0.5f);
        UpdateSelectionVisuals();
    }

    private void UpdateSelectionVisuals()
    {
        for (int i = 0; i < menuTexts.Length; i++)
        {
            if (menuTexts[i] == null) continue;

            if (isTutorialMode && i == 2)
            {
                menuTexts[i].color = disableColor;
                menuTexts[i].rectTransform.localScale = Vector3.one;
                continue;
            }

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
        if (isTutorialMode && _currentIndex == 2) return;

        if (sfxSubmit) SoundManager.instance.PlaySFX(sfxSubmit, 0.2f);

        switch (_currentIndex)
        {
            case 0: 
                ResumeGame(); 
                break;
            case 1: 
                if (settingUI != null) settingUI.OpenSettings(); 
                break;
            case 2: // 게임 포기
                if (ConfirmUIController.Instance != null)
                {
                    // ★ UI가 닫히지 않고 바로 씬이 넘어가도 시간이 멈추지 않게 방어
                    ConfirmUIController.Instance.ShowPopupByIndex(0, () => { Time.timeScale = 1f; });
                }
                break;
            case 3: // 게임 종료
                if (ConfirmUIController.Instance != null)
                {
                    ConfirmUIController.Instance.ShowPopupByIndex(1, () => { Time.timeScale = 1f; });
                }
                break;
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