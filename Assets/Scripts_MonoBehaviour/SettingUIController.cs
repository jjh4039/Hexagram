using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class SettingUIController : MonoBehaviour
{
    public bool IsOpen => _isOpen;                           

    [Header("UI References")]
    [SerializeField] private GameObject visualRoot;          
    [SerializeField] private CanvasGroup visualGroup;        
    [SerializeField] private RectTransform bgRect;           
    [SerializeField] private TextMeshProUGUI[] menuTexts;    
    [SerializeField] private TextMeshProUGUI[] valueTexts;   
    [SerializeField] private Slider[] volumeSliders;         

    [Header("Animation Settings")]
    [SerializeField] private float targetBgHeight = 600f;    
    [SerializeField] private float bgExpandDuration = 0.25f; 

    [Header("Floating Settings")]
    [SerializeField] private float floatAmplitude = 10f;     
    [SerializeField] private float floatSpeed = 2f;          

    [Header("Colors & Sounds")]
    [SerializeField] private Color normalColor = Color.gray; 
    [SerializeField] private Color selectColor = Color.white;
    [SerializeField] private AudioClip sfxMove;              
    [SerializeField] private AudioClip sfxAdjust;            
    [SerializeField] private AudioClip sfxClose;             

    private bool _isOpen;                            
    private bool _isAnimating;                       
    private int _currentIndex;                           

    private Coroutine _animCoroutine;                        
    private Vector2 _bgOriginAnchoredPos;                    

    private int[] _currentValues = new int[6];               

    private readonly string[] _screenModes = { "창 모드", "테두리 없음", "전체 화면" };
    private readonly string[] _resolutions = { "1280 x 720", "1920 x 1080", "2560 x 1440" };
    private readonly string[] _vSyncModes = { "OFF", "ON" };

    private void Start()
    {
        if (visualRoot != null) visualRoot.SetActive(false);
        if (bgRect != null) _bgOriginAnchoredPos = bgRect.anchoredPosition;

        LoadSettingsData();                                  

        if (InputStateManager.Instance != null)
        {
            var actions = InputStateManager.Instance.Actions.UI;
            actions.MoveUI.performed += OnNavigate;
            actions.CloseUI.performed += OnCloseUI;
        }
    }

    private void OnDestroy()
    {
        if (InputStateManager.Instance != null && InputStateManager.Instance.Actions != null)
        {
            var actions = InputStateManager.Instance.Actions.UI;
            actions.MoveUI.performed -= OnNavigate;
            actions.CloseUI.performed -= OnCloseUI;
        }
    }

    private void Update()
    {
        if (_isOpen && bgRect != null)
        {
            float newY = _bgOriginAnchoredPos.y + Mathf.Sin(Time.unscaledTime * floatSpeed) * floatAmplitude;
            bgRect.anchoredPosition = new Vector2(_bgOriginAnchoredPos.x, newY);
        }
    }

    private void LoadSettingsData()
    {
        if (DataManager.instance != null)
        {
            GameData data = DataManager.instance.data;
            _currentValues[0] = data.masterVolume;
            _currentValues[1] = data.bgmVolume;
            _currentValues[2] = data.sfxVolume;
            _currentValues[3] = data.screenMode;
            _currentValues[4] = data.resolution;
            _currentValues[5] = data.vSync;
        }
        else
        {
            _currentValues[0] = 5; 
            _currentValues[1] = 5; 
            _currentValues[2] = 5; 
            _currentValues[3] = 1; 
            _currentValues[4] = 1; 
            _currentValues[5] = 1; 
        }

        for (int i = 0; i < 6; i++)
        {
            ApplySettingToSystem(i, _currentValues[i]);
        }
    }

    private void SaveSettingsData()
    {
        if (DataManager.instance == null) return;

        GameData data = DataManager.instance.data;
        data.masterVolume = _currentValues[0];
        data.bgmVolume = _currentValues[1];
        data.sfxVolume = _currentValues[2];
        data.screenMode = _currentValues[3];
        data.resolution = _currentValues[4];
        data.vSync = _currentValues[5];

        DataManager.instance.SaveGame();                     
    }

    private void ApplySettingToSystem(int index, int value)
    {
        switch (index)
        {
            case 0:
                if (SoundManager.instance != null) SoundManager.instance.SetMasterVolume(value / 10f);
                break;
            case 1:
                if (SoundManager.instance != null) SoundManager.instance.SetBGMVolume(value / 10f);
                break;
            case 2:
                if (SoundManager.instance != null) SoundManager.instance.SetSFXVolume(value / 10f);
                break;
            case 3:
            case 4:
            case 5:
                ApplyResolutionAndScreenMode();
                break;
        }
    }

    private void ApplyResolutionAndScreenMode()
    {
        FullScreenMode mode = FullScreenMode.Windowed;                  
        if (_currentValues[3] == 1) mode = FullScreenMode.FullScreenWindow;     
        else if (_currentValues[3] == 2) mode = FullScreenMode.ExclusiveFullScreen; 

        int width = 1920;
        int height = 1080;

        if (_currentValues[4] == 0) { width = 1280; height = 720; }
        else if (_currentValues[4] == 1) { width = 1920; height = 1080; }
        else if (_currentValues[4] == 2) { width = 2560; height = 1440; }

        Screen.SetResolution(width, height, mode); 
        
        QualitySettings.vSyncCount = _currentValues[5]; 
        
        if (QualitySettings.vSyncCount == 0)
            Application.targetFrameRate = 144; // VSync가 꺼져있을 때만 144 프레임 제한 적용
        else
            Application.targetFrameRate = -1;  // VSync가 켜져있으면 엔진이 모니터 주사율에 자동 동기화
    }


    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        if (!_isOpen || _isAnimating) return;
        Vector2 input = ctx.ReadValue<Vector2>();

        if (input.y > 0.5f) ChangeSelection(-1);
        else if (input.y < -0.5f) ChangeSelection(1);
        else if (input.x > 0.5f) AdjustValue(1);
        else if (input.x < -0.5f) AdjustValue(-1);
    }

    private void OnCloseUI(InputAction.CallbackContext ctx)
    {
        if (!_isOpen || _isAnimating) return;
        CloseSettings();
    }

    public void OpenSettings()
    {
        _isOpen = true;
        _currentIndex = 0;

        UpdateAllVisuals();
        
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateOpen());
    }

    public void CloseSettings()
    {
        if (sfxClose) SoundManager.instance.PlaySFX(sfxClose, 0.3f);

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateClose());
    }

    private void ChangeSelection(int dir)
    {
        _currentIndex = (_currentIndex + dir + menuTexts.Length) % menuTexts.Length;
        if (sfxMove) SoundManager.instance.PlaySFX(sfxMove, 0.5f);
        UpdateSelectionVisuals();
    }

    private void AdjustValue(int dir)
    {
        bool valueChanged = false;

        switch (_currentIndex)
        {
            case 0:
            case 1:
            case 2:
                int prevVol = _currentValues[_currentIndex];
                _currentValues[_currentIndex] = Mathf.Clamp(_currentValues[_currentIndex] + dir, 0, 10);
                if (prevVol != _currentValues[_currentIndex]) valueChanged = true;
                break;

            case 3:
                int prevMode = _currentValues[_currentIndex];
                _currentValues[_currentIndex] = (_currentValues[_currentIndex] + dir + _screenModes.Length) % _screenModes.Length;
                if (prevMode != _currentValues[_currentIndex]) valueChanged = true;
                break;

            case 4:
                int prevRes = _currentValues[_currentIndex];
                _currentValues[_currentIndex] = Mathf.Clamp(_currentValues[_currentIndex] + dir, 0, _resolutions.Length - 1);
                if (prevRes != _currentValues[_currentIndex]) valueChanged = true;
                break;
            
            case 5:
                _currentValues[_currentIndex] = (_currentValues[_currentIndex] == 0) ? 1 : 0;
                valueChanged = true;
                break;
        }

        if (valueChanged)
        {
            if (sfxAdjust) SoundManager.instance.PlaySFX(sfxAdjust, 0.5f);
            UpdateValueText(_currentIndex);
            UpdateSlider(_currentIndex);

            ApplySettingToSystem(_currentIndex, _currentValues[_currentIndex]); 
            SaveSettingsData();                                                 
        }
    }

    private void UpdateAllVisuals()
    {
        UpdateSelectionVisuals();
        for (int i = 0; i < menuTexts.Length; i++)
        {
            UpdateValueText(i);
            UpdateSlider(i);
        }
    }

    private void UpdateSelectionVisuals()
    {
        for (int i = 0; i < menuTexts.Length; i++)
        {
            if (menuTexts[i] == null) continue;
            bool isSelected = (i == _currentIndex);
            
            menuTexts[i].color = isSelected ? selectColor : normalColor;
            
            if (i < valueTexts.Length && valueTexts[i] != null) // 배열 범위 초과 에러 방지
            {
                valueTexts[i].color = isSelected ? selectColor : normalColor;
                UpdateValueText(i);
            }

            if (i <= 2 && i < volumeSliders.Length && volumeSliders[i] != null)
            {
                Image[] sliderImages = volumeSliders[i].GetComponentsInChildren<Image>();
                foreach (Image img in sliderImages)
                {
                    img.color = isSelected ? selectColor : normalColor;
                }
            }
        }
    }

    private void UpdateValueText(int index)
    {
        if (index >= valueTexts.Length || valueTexts[index] == null) return;

        string displayStr = "";
        switch (index)
        {
            case 0:
            case 1:
            case 2:
                displayStr = $"{_currentValues[index] * 10}%";
                break;
            case 3:
                displayStr = _screenModes[_currentValues[index]];
                break;
            case 4:
                displayStr = _resolutions[_currentValues[index]];
                break;
            case 5:
                displayStr = _vSyncModes[_currentValues[index]];
                break;
        }

        bool isSelected = (index == _currentIndex);
        valueTexts[index].text = isSelected ? $"<< {displayStr} >>" : displayStr;
    }

    private void UpdateSlider(int index)
    {
        if (index > 2 || volumeSliders.Length <= index || volumeSliders[index] == null) return;
        volumeSliders[index].value = _currentValues[index];
    }

    private IEnumerator AnimateOpen()
    {
        _isAnimating = true;
        visualRoot.SetActive(true);

        bgRect.sizeDelta = new Vector2(bgRect.sizeDelta.x, 0f);
        if (visualGroup != null) visualGroup.alpha = 0f;

        float elapsed = 0f;

        while (elapsed < bgExpandDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / bgExpandDuration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            bgRect.sizeDelta = new Vector2(bgRect.sizeDelta.x, Mathf.Lerp(0f, targetBgHeight, easedT));
            
            if (visualGroup != null) visualGroup.alpha = t;
            yield return null;
        }

        bgRect.sizeDelta = new Vector2(bgRect.sizeDelta.x, targetBgHeight);
        if (visualGroup != null) visualGroup.alpha = 1f;

        _isAnimating = false;
    }

    private IEnumerator AnimateClose()
    {
        _isAnimating = true;

        float elapsed = 0f;
        float startHeight = bgRect.sizeDelta.y;

        while (elapsed < bgExpandDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / bgExpandDuration);
            float easedT = t * t * t;
            bgRect.sizeDelta = new Vector2(bgRect.sizeDelta.x, Mathf.Lerp(startHeight, 0f, easedT));
            
            if (visualGroup != null) visualGroup.alpha = 1f - t;
            yield return null;
        }

        visualRoot.SetActive(false);
        bgRect.anchoredPosition = _bgOriginAnchoredPos;

        _isOpen = false;
        _isAnimating = false;
    }
}