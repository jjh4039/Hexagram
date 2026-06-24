using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [Header("Scene Transition")] [SerializeField]
    private string tutorialSceneName = "Tutorial";

    [SerializeField] private string mainSceneName = "Main";
    [SerializeField] private float sceneFadeDuration = 2f;
    [SerializeField] private AudioClip startSound;

    [Header("UI Reference")] [SerializeField]
    private CanvasGroup titleLogo;

    [SerializeField] private RectTransform titleRect;
    [SerializeField] private CanvasGroup textGroup;
    [SerializeField] private RectTransform background;
    [SerializeField] private TextMeshProUGUI[] menuTexts;
    [SerializeField] private GameObject[] cursorIcons;
    [SerializeField] private SettingUIController settingUI;
    [SerializeField] private TotalGems totalGemsUI;

    [SerializeField] private CanvasGroup topUIGroup;
    [SerializeField] private CanvasGroup guideTextGroup;

    [Header("Animation Settings")] [SerializeField]
    private float logoFadeDuration = 1.5f;

    [SerializeField] private float delayBetween = 0.2f;
    [SerializeField] private float textFadeDuration = 0.5f;

    [Header("Idle Floating Settings")] [SerializeField]
    private float floatSpeed = 2f;

    [SerializeField] private float floatAmount = 3f;

    [Header("Parallax Settings")] [SerializeField]
    private float parallaxLimit = 20f;

    [SerializeField] private float parallaxSmooth = 3f;
    [SerializeField] private float autoPanSpeed = 0.5f;
    [SerializeField] private float autoPanAmount = 20f;

    [Header("Color Settings")] [SerializeField]
    private Color normalColor = Color.gray;

    [SerializeField] private Color highlightColor = Color.white;

    [Header("Sound Settings")] [SerializeField]
    private AudioClip titleBGM;

    [SerializeField] private float bgmFadeDuration = 1f;
    [SerializeField] private AudioClip moveSound;
    [SerializeField] private AudioClip selectSound;

    private int _currentIndex = 0;
    private Vector2 _bgOriginPos;
    private float _baseY;
    private bool _isInputActive = false;
    private bool _isFloatingActive = false;
    private float _introTimer = 0f;

    private void Awake()
    {
        if (background != null) _bgOriginPos = background.anchoredPosition;

        normalColor.a = 1f;
        highlightColor.a = 1f;

        if (titleLogo != null) titleLogo.alpha = 0f;
        if (textGroup != null) textGroup.alpha = 0f;
        if (topUIGroup != null) topUIGroup.alpha = 0f;
        if (guideTextGroup != null) guideTextGroup.alpha = 0f;

        if (titleRect != null) _baseY = titleRect.anchoredPosition.y;

        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.SetBlackScreen(true);
        }
    }

    private void Start()
    {
        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.ChangeInputState(InputState.UI);

            var actions = InputStateManager.Instance.Actions.UI;
            actions.MoveUI.performed += OnMoveInput;
            actions.Select.performed += OnSelectInput;
            actions.Toggle.performed += OnToggleInput;

            InputStateManager.Instance.OnInputDeviceChanged += HandleDeviceChanged;
        }

        if (SoundManager.instance != null && titleBGM != null)
        {
            SoundManager.instance.PlayBGM(titleBGM, null, bgmFadeDuration);
        }

        UpdateMenuVisuals();
        _isFloatingActive = true;
        StartCoroutine(IntroSequence());
    }

    private void OnDestroy()
    {
        if (InputStateManager.Instance != null)
        {
            if (InputStateManager.Instance.Actions != null)
            {
                var actions = InputStateManager.Instance.Actions.UI;
                actions.MoveUI.performed -= OnMoveInput;
                actions.Select.performed -= OnSelectInput;
                actions.Toggle.performed -= OnToggleInput;
            }

            InputStateManager.Instance.OnInputDeviceChanged -= HandleDeviceChanged;
        }
    }

    private void Update()
    {
        HandleParallaxBackground();
        HandleIdleFloating();
        UpdateGuideTextVisibility();
    }

    private void HandleDeviceChanged(InputDeviceType device)
    {
        if (device == InputDeviceType.Keyboard) UpdateMenuVisuals();
    }

    private void UpdateGuideTextVisibility()
    {
        bool shouldShow = _isInputActive &&
                          (settingUI == null || !settingUI.IsOpen) &&
                          (totalGemsUI == null || !totalGemsUI.IsOpen);

        if (guideTextGroup != null)
        {
            if (shouldShow)
            {
                float pulseAlpha = Mathf.Lerp(0.5f, 1f, (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f);
                guideTextGroup.alpha = pulseAlpha;
            }
            else
            {
                guideTextGroup.alpha = 0f;
            }
        }

        if (topUIGroup != null)
        {
            bool topShouldShow = _isInputActive && (settingUI == null || !settingUI.IsOpen);
            topUIGroup.alpha = topShouldShow ? 1f : 0f;
        }
    }

    private IEnumerator IntroSequence()
    {
        if (TransitionManager.Instance)
        {
            yield return StartCoroutine(TransitionManager.Instance.Co_FadeToClear(sceneFadeDuration));
        }

        float timer = 0f;

        while (timer < 1f)
        {
            timer += Time.deltaTime / logoFadeDuration;
            titleLogo.alpha = Mathf.Lerp(0f, 1f, timer);
            yield return null;
        }

        yield return new WaitForSeconds(delayBetween);

        _isInputActive = true;
        timer = 0f;

        while (timer < 1f)
        {
            timer += Time.deltaTime / textFadeDuration;
            textGroup.alpha = Mathf.Lerp(0f, 1f, timer);
            yield return null;
        }
    }

    private void HandleParallaxBackground()
    {
        if (!background) return;

        Vector2 mouseOffset = Vector2.zero;
        if (Mouse.current != null && InputStateManager.Instance != null &&
            InputStateManager.Instance.CurrentDevice == InputDeviceType.Mouse)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            mouseOffset.x = (mousePos.x - (Screen.width / 2f)) / (Screen.width / 2f) * parallaxLimit;
            mouseOffset.y = (mousePos.y - (Screen.height / 2f)) / (Screen.height / 2f) * parallaxLimit;
        }

        float autoX = Mathf.Sin(Time.time * autoPanSpeed) * autoPanAmount;
        float autoY = Mathf.Cos(Time.time * autoPanSpeed * 0.8f) * autoPanAmount;

        Vector2 targetPos = _bgOriginPos + new Vector2(mouseOffset.x + autoX, mouseOffset.y + autoY);

        background.anchoredPosition =
            Vector2.Lerp(background.anchoredPosition, targetPos, Time.deltaTime * parallaxSmooth);
    }

    private void HandleIdleFloating()
    {
        if (_isFloatingActive && titleRect)
        {
            _introTimer += Time.deltaTime;
            float floatingY = _baseY + (Mathf.Sin(_introTimer * floatSpeed) * floatAmount);
            titleRect.anchoredPosition = new Vector2(titleRect.anchoredPosition.x, floatingY);
        }
    }

    private void OnToggleInput(InputAction.CallbackContext context)
    {
        if (!_isInputActive) return;
        if (settingUI != null && settingUI.IsOpen) return;

        if (totalGemsUI != null)
        {
            if (totalGemsUI.IsOpen) totalGemsUI.CloseUI();
            else totalGemsUI.OpenUI();
        }
    }

    private void OnMoveInput(InputAction.CallbackContext context)
    {
        if (!_isInputActive) return;
        if (settingUI != null && settingUI.IsOpen) return;
        if (totalGemsUI != null && totalGemsUI.IsOpen) return;

        Vector2 moveDir = context.ReadValue<Vector2>();

        if (moveDir.x > 0.5f || moveDir.y < -0.5f) ChangeIndex(1);
        else if (moveDir.x < -0.5f || moveDir.y > 0.5f) ChangeIndex(-1);
    }

    private void OnSelectInput(InputAction.CallbackContext context)
    {
        if (!_isInputActive) return;
        if (settingUI != null && settingUI.IsOpen) return;
        if (totalGemsUI != null && totalGemsUI.IsOpen) return;

        ExecuteMenu();
    }

    public void SetIndexByMouse(int index)
    {
        if (!_isInputActive || _currentIndex == index) return;
        if (settingUI != null && settingUI.IsOpen) return;
        if (totalGemsUI != null && totalGemsUI.IsOpen) return;
        if (InputStateManager.Instance != null &&
            InputStateManager.Instance.CurrentDevice == InputDeviceType.Keyboard) return;

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
        if (settingUI != null && settingUI.IsOpen) return; // 설정 창 클릭 방어
        if (totalGemsUI != null && totalGemsUI.IsOpen) return; // 보석 창 클릭 방어

        if (SoundManager.instance != null && selectSound != null)
        {
            SoundManager.instance.PlaySFX(selectSound);
        }

        if (_currentIndex == 0)
        {
            _isInputActive = false;
            StartCoroutine(TransitionToGame());
        }
        else if (_currentIndex == 1)
        {
            if (settingUI != null) settingUI.OpenSettings();
        }
        else if (_currentIndex == 2)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    private IEnumerator TransitionToGame()
    {
        if (SoundManager.instance && startSound)
        {
            SoundManager.instance.PlaySFX(startSound);
        }

        if (SoundManager.instance)
        {
            SoundManager.instance.StopBGM(sceneFadeDuration);
        }

        // GameAnalytics 2번 정보 전송 (주사위 보석)
        if (DataManager.instance != null && DataManager.instance.data != null && AnalyticsManager.Instance != null)
        {
            GameData d = DataManager.instance.data;
            AnalyticsManager.Instance.LogUpgradeLoadout(
                d.upgradeHealthLevel,
                d.upgradeAttackLevel,
                d.upgradeBulletLevel,
                d.difficultyLevel
            );
        }

        string targetScene = tutorialSceneName;

        if (DataManager.instance && DataManager.instance.data != null)
        {
            targetScene = DataManager.instance.data.isTutorialClear ? mainSceneName : tutorialSceneName;
        }

        if (TransitionManager.Instance)
        {
            TransitionManager.Instance.LoadScene(targetScene, sceneFadeDuration, sceneFadeDuration);
        }
        else
        {
            SceneManager.LoadScene(targetScene);
        }

        yield return null;
    }
}