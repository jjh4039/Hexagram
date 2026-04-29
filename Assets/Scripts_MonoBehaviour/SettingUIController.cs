using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class SettingUIController : MonoBehaviour
{
    public bool IsOpen => _isOpen;                           // 외부 상태 참조용

    [Header("UI References")]
    [SerializeField] private GameObject visualRoot;          // 껐다 켤 시각적 최상위 객체
    [SerializeField] private CanvasGroup visualGroup;        // 전체 페이드용 그룹
    [SerializeField] private RectTransform bgRect;           // 크기 애니메이션용 배경
    [SerializeField] private TextMeshProUGUI[] menuTexts;    // 좌측 메뉴 이름 텍스트 배열
    [SerializeField] private TextMeshProUGUI[] valueTexts;   // 우측 설정값 텍스트 배열
    [SerializeField] private Slider[] volumeSliders;         // 볼륨 조절용 슬라이더 배열

    [Header("Animation Settings")]
    [SerializeField] private float targetBgHeight = 600f;    // 배경 최대 높이
    [SerializeField] private float bgExpandDuration = 0.25f; // 배경 확장 시간

    [Header("Floating Settings")]
    [SerializeField] private float floatAmplitude = 10f;     // 부유 진폭
    [SerializeField] private float floatSpeed = 2f;          // 부유 속도

    [Header("Colors & Sounds")]
    [SerializeField] private Color normalColor = Color.gray; // 비활성 색상
    [SerializeField] private Color selectColor = Color.white;// 선택 색상
    [SerializeField] private AudioClip sfxMove;              // 이동 사운드
    [SerializeField] private AudioClip sfxAdjust;            // 값 조절 사운드
    [SerializeField] private AudioClip sfxClose;             // 닫기 사운드

    private bool _isOpen = false;                            // 설정창 열림 여부
    private bool _isAnimating = false;                       // 애니메이션 진행 여부
    private int _currentIndex = 0;                           // 현재 선택된 메뉴 인덱스

    private Coroutine _animCoroutine;                        // 애니메이션 코루틴 캐싱
    private Vector2 _bgOriginAnchoredPos;                    // 배경 초기 위치

    private int[] _currentValues = new int[6];               // 인덱스별 임시 설정값 저장

    private readonly string[] _screenModes = { "창 모드", "전체 화면" };
    private readonly string[] _resolutions = { "1280 x 720", "1920 x 1080", "2560 x 1440" };
    private readonly string[] _shakeModes = { "OFF", "ON" };

    private void Start()
    {
        if (visualRoot != null) visualRoot.SetActive(false);
        if (bgRect != null) _bgOriginAnchoredPos = bgRect.anchoredPosition;

        LoadSettingsData();                                  // 더미 데이터 대신 실제 세이브 데이터 로드

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
            _currentValues[5] = data.cameraShake;
        }
        else
        {
            // 매니저가 없을 경우의 기본값
            _currentValues[0] = 5; 
            _currentValues[1] = 5; 
            _currentValues[2] = 5; 
            _currentValues[3] = 1; 
            _currentValues[4] = 1; 
            _currentValues[5] = 1; 
        }

        // 로드된 값을 실제 게임 시스템(소리, 해상도 등)에 즉시 적용
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
        data.cameraShake = _currentValues[5];

        DataManager.instance.SaveGame();                     // 변경된 값을 JSON으로 영구 저장
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
                ApplyResolutionAndScreenMode();              // 창 모드 또는 해상도가 바뀌면 한 번에 갱신
                break;
            case 5:
                // 카메라 흔들림(추후 연동)
                break;
        }
    }

    private void ApplyResolutionAndScreenMode()
    {
        bool isFullScreen = (_currentValues[3] == 1);
        int width = 1920;
        int height = 1080;

        if (_currentValues[4] == 0) { width = 1280; height = 720; }
        else if (_currentValues[4] == 1) { width = 1920; height = 1080; }
        else if (_currentValues[4] == 2) { width = 2560; height = 1440; }

        Screen.SetResolution(width, height, isFullScreen);   // 유니티 내장 해상도 적용 함수
    }

    // 작성자 요청에 따라 조작 딜레이 로직 취소 및 원상 복구
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
                _currentValues[_currentIndex] = _currentValues[_currentIndex] == 0 ? 1 : 0;
                valueChanged = true;
                break;
            
            case 4:
                int prevRes = _currentValues[_currentIndex];
                _currentValues[_currentIndex] = Mathf.Clamp(_currentValues[_currentIndex] + dir, 0, _resolutions.Length - 1);
                if (prevRes != _currentValues[_currentIndex]) valueChanged = true;
                break;
            
            case 5:
                _currentValues[_currentIndex] = _currentValues[_currentIndex] == 0 ? 1 : 0;
                valueChanged = true;
                break;
        }

        if (valueChanged)
        {
            if (sfxAdjust) SoundManager.instance.PlaySFX(sfxAdjust, 0.5f);
            UpdateValueText(_currentIndex);
            UpdateSlider(_currentIndex);

            ApplySettingToSystem(_currentIndex, _currentValues[_currentIndex]); // 실시간 시스템 연동
            SaveSettingsData();                                                 // 실시간 데이터 저장
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
            
            if (valueTexts[i] != null)
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
        if (valueTexts[index] == null) return;

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
                displayStr = _shakeModes[_currentValues[index]];
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