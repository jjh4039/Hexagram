using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class BalanceManager : MonoBehaviour
{
    public static BalanceManager instance;

    [Header("UI Fade Settings")] [SerializeField]
    private CanvasGroup mainCanvasGroup;

    [SerializeField] private CanvasGroup contentCanvasGroup;
    [SerializeField] private float fadeDuration = 0.3f;

    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image mainFaceImage;
    [SerializeField] private Sprite normalSprite;

    [Header("Main Image Floating Settings")] [SerializeField]
    private float floatSpeed = 3f;

    [SerializeField] private float floatAmplitude = 2f;
    private Vector2 _mainImageInitialPos;
    private Coroutine _floatCoroutine;

    [Header("Background Loop Settings")] [SerializeField]
    private float bgScaleMin = 0.9f;

    [SerializeField] private float bgScaleMax = 1.1f;
    [SerializeField] private float bgRotationRange = 3f;
    [SerializeField] private float bgAnimSpeed = 0.3f;

    [Header("Face Zone Settings")] [SerializeField]
    private FaceChoice[] faceChoices;

    [Header("Probability UI Settings (6 Texts)")] [SerializeField]
    private TextMeshProUGUI[] probabilityTexts;

    [SerializeField] private Color normalTextColor = new Color(0.88f, 0.88f, 0.88f);
    [SerializeField] private Color increaseTextColor = new Color(0.5f, 0.78f, 0.52f);
    [SerializeField] private Color decreaseTextColor = new Color(0.9f, 0.45f, 0.45f);

    [Header("Right Info Panel Settings")] [SerializeField]
    private GameObject infoPanelObject;

    [SerializeField] private TextMeshProUGUI faceDescText;
    [SerializeField] private Image diceIconImage;
    [SerializeField] private Image diceIconBackground;
    [SerializeField] private TextMeshProUGUI transitionProbText;
    [SerializeField] private Image confirmButtonImage;

    [Header("Default Info State")] [SerializeField]
    private string defaultDescription = "확률을 높일 주사위의 면을 선택하세요.";

    [SerializeField] private Color defaultColor = Color.gray;

    [Header("Audio")] [SerializeField] private AudioClip sfxIntro;
    [SerializeField] private AudioClip sfxSelect;
    [SerializeField] private AudioClip sfxDecision;

    private float _currentWeightPercent = 5f;

    private int _selectedIndex = -1;
    private int _currentHoverIndex = -1;
    private bool _isInitialized = false;

    private void Awake()
    {
        instance = this;

        if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0f;
        if (contentCanvasGroup != null) contentCanvasGroup.alpha = 0f;

        if (mainFaceImage != null)
            _mainImageInitialPos = mainFaceImage.rectTransform.anchoredPosition;

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_isInitialized)
        {
            Time.timeScale = 1.0f;
            if (InputStateManager.Instance != null) InputStateManager.Instance.CloseUI();
        }
    }

    public void OpenBalanceUI(float incomingWeightPercent)
    {
        if (InputStateManager.Instance != null && !InputStateManager.Instance.TryOpenUI()) return;

        _currentWeightPercent = incomingWeightPercent;

        _isInitialized = false;
        _selectedIndex = -1;
        _currentHoverIndex = -1;

        if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0f;
        if (contentCanvasGroup != null) contentCanvasGroup.alpha = 0f;

        UpdateMainImage();
        UpdateProbabilityUI();
        UpdateInfoPanel();

        Time.timeScale = 0f;

        if (sfxIntro != null && SoundManager.instance != null)
            SoundManager.instance.PlaySFX(sfxIntro, 0.6f, 0.1f);

        StopAllCoroutines();
        gameObject.SetActive(true);

        if (backgroundImage != null) StartCoroutine(LoopBackgroundAnimation());
        if (mainFaceImage != null) _floatCoroutine = StartCoroutine(FloatMainImage());

        StartCoroutine(FadeInUI());
    }

    private IEnumerator FadeInUI()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (mainCanvasGroup)
                mainCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        if (mainCanvasGroup) mainCanvasGroup.alpha = 1f;

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (contentCanvasGroup)
                contentCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        if (contentCanvasGroup) contentCanvasGroup.alpha = 1f;

        _isInitialized = true;
    }

    private void Update()
    {
        if (!_isInitialized) return;

        CheckMouseHover();
        CheckMouseClick();
    }

    private void CheckMouseHover()
    {
        if (_selectedIndex != -1) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        int newHoverIndex = -1;

        for (int i = 0; i < faceChoices.Length; i++)
        {
            if (faceChoices[i].hoverSensor &&
                RectTransformUtility.RectangleContainsScreenPoint(faceChoices[i].hoverSensor.rectTransform, mousePos,
                    null))
            {
                newHoverIndex = i;
                break;
            }
        }

        if (_currentHoverIndex != newHoverIndex)
        {
            _currentHoverIndex = newHoverIndex;
            UpdateMainImage();
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
                    OnConfirmButtonClick();
                    return;
                }
            }

            for (int i = 0; i < faceChoices.Length; i++)
            {
                if (faceChoices[i].hoverSensor &&
                    RectTransformUtility.RectangleContainsScreenPoint(faceChoices[i].hoverSensor.rectTransform,
                        mousePos, null))
                {
                    SelectFace(i);
                    break;
                }
            }
        }
    }

    private void SelectFace(int newIndex)
    {
        if (_selectedIndex == newIndex) return;

        _selectedIndex = newIndex;

        if (sfxSelect && SoundManager.instance)
            SoundManager.instance.PlaySFX(sfxSelect, 0.6f);

        UpdateMainImage();
        UpdateProbabilityUI();
        UpdateInfoPanel();
    }

    private void UpdateMainImage()
    {
        if (!mainFaceImage) return;

        if (_selectedIndex != -1)
        {
            mainFaceImage.sprite = faceChoices[_selectedIndex].highlightSprite;
        }
        else if (_currentHoverIndex != -1)
        {
            mainFaceImage.sprite = faceChoices[_currentHoverIndex].highlightSprite;
        }
        else
        {
            mainFaceImage.sprite = normalSprite;
        }
    }

    private void UpdateProbabilityUI()
    {
        if (!GameManager.instance || !GameManager.instance.dice) return;
        if (probabilityTexts == null || probabilityTexts.Length < 6) return;

        float[] currentPercentages = GameManager.instance.dice.displayPercentages;

        if (_selectedIndex == -1)
        {
            for (int i = 0; i < 6; i++)
            {
                if (probabilityTexts[i])
                {
                    probabilityTexts[i].text = currentPercentages[i].ToString("F1") + "%";
                    probabilityTexts[i].color = normalTextColor;
                }
            }
        }
        else
        {
            float[] predictedPercentages =
                GameManager.instance.dice.GetPredictedPercentages(_selectedIndex, _currentWeightPercent);

            for (int i = 0; i < 6; i++)
            {
                if (probabilityTexts[i])
                {
                    probabilityTexts[i].text = predictedPercentages[i].ToString("F1") + "%";

                    if (i == _selectedIndex)
                        probabilityTexts[i].color = increaseTextColor;
                    else
                        probabilityTexts[i].color = decreaseTextColor;
                }
            }
        }
    }

    private void UpdateInfoPanel()
    {
        if (!infoPanelObject) return;

        infoPanelObject.SetActive(true);

        if (_selectedIndex != -1 && GameManager.instance && GameManager.instance.dice)
        {
            DiceData currentData = GameManager.instance.dice.diceList[_selectedIndex];

            float currentPercent = GameManager.instance.dice.displayPercentages[_selectedIndex];
            float[] predicted =
                GameManager.instance.dice.GetPredictedPercentages(_selectedIndex, _currentWeightPercent);
            float predictedPercent = predicted[_selectedIndex];

            if (currentData)
            {
                if (faceDescText) faceDescText.text = currentData.shortDescription;

                if (diceIconImage)
                {
                    diceIconImage.gameObject.SetActive(true);
                    diceIconImage.sprite = currentData.icon;
                }

                if (diceIconBackground)
                {
                    diceIconBackground.gameObject.SetActive(true);
                    diceIconBackground.color = currentData.particleColor;
                }
            }

            if (transitionProbText)
            {
                Color highlightColor = currentData ? currentData.particleColor : increaseTextColor;
                transitionProbText.text =
                    $"{currentPercent:F1}% -> <color=#{ColorUtility.ToHtmlStringRGB(highlightColor)}>{predictedPercent:F1}%</color>";
            }

            if (confirmButtonImage) confirmButtonImage.gameObject.SetActive(true);
        }
        else
        {
            if (faceDescText) faceDescText.text = defaultDescription;

            if (diceIconImage) diceIconImage.gameObject.SetActive(false);

            if (diceIconBackground)
            {
                diceIconBackground.gameObject.SetActive(true);
                diceIconBackground.color = defaultColor;
            }

            if (transitionProbText) transitionProbText.text = "";

            if (confirmButtonImage) confirmButtonImage.gameObject.SetActive(false);
        }
    }

    public void OnConfirmButtonClick()
    {
        if (_selectedIndex == -1) return;

        if (sfxDecision && SoundManager.instance)
            SoundManager.instance.PlaySFX(sfxDecision, 0.7f, 0.2f);

        if (GameManager.instance && GameManager.instance.dice)
        {
            GameManager.instance.dice.AddPercentToFace(_selectedIndex, _currentWeightPercent);

            if (AnalyticsManager.Instance)
            {
                AnalyticsManager.Instance.LogBalanceSelection(_selectedIndex, _currentWeightPercent);
                AnalyticsManager.Instance.LogDiceBuildState(GameManager.instance.dice.displayPercentages);
            }
        }

        CloseBalanceUI();
    }

    private void CloseBalanceUI()
    {
        if (!_isInitialized) return;
        _isInitialized = false;

        if (confirmButtonImage) confirmButtonImage.gameObject.SetActive(false);

        StartCoroutine(FadeOutUI());
    }

    private IEnumerator FadeOutUI()
    {
        Time.timeScale = 1.0f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float currentAlpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));

            if (mainCanvasGroup) mainCanvasGroup.alpha = currentAlpha;
            if (contentCanvasGroup) contentCanvasGroup.alpha = currentAlpha;

            yield return null;
        }

        if (mainCanvasGroup) mainCanvasGroup.alpha = 0f;
        if (contentCanvasGroup) contentCanvasGroup.alpha = 0f;

        if (InputStateManager.Instance) InputStateManager.Instance.CloseUI();

        gameObject.SetActive(false);
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

    private IEnumerator FloatMainImage()
    {
        while (true)
        {
            float newY = _mainImageInitialPos.y + Mathf.Sin(Time.unscaledTime * floatSpeed) * floatAmplitude;
            mainFaceImage.rectTransform.anchoredPosition = new Vector2(_mainImageInitialPos.x, newY);
            yield return null;
        }
    }

    [System.Serializable]
    public struct FaceChoice
    {
        public Image hoverSensor;
        public Sprite highlightSprite;
    }
}