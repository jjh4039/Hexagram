using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class BalanceManager : MonoBehaviour
{
    public static BalanceManager instance;

    [Header("UI Fade Settings")]
    [SerializeField] private CanvasGroup mainCanvasGroup; // 전체 배경 캔버스 그룹
    [SerializeField] private CanvasGroup contentCanvasGroup; // 헥사그램, 우측 패널 등 내부 컨텐츠 캔버스 그룹
    [SerializeField] private float fadeDuration = 0.3f;

    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image mainFaceImage;
    [SerializeField] private Sprite normalSprite;

    [Header("Main Image Floating Settings")]
    [SerializeField] private float floatSpeed = 3f;
    [SerializeField] private float floatAmplitude = 2f;
    private Vector2 mainImageInitialPos;
    private Coroutine floatCoroutine;

    [Header("Background Loop Settings")]
    [SerializeField] private float bgScaleMin = 0.9f;
    [SerializeField] private float bgScaleMax = 1.1f;
    [SerializeField] private float bgRotationRange = 3f;
    [SerializeField] private float bgAnimSpeed = 0.3f;

    [Header("Face Zone Settings")]
    [SerializeField] private FaceChoice[] faceChoices;

    [Header("Probability UI Settings (6 Texts)")]
    [SerializeField] private TextMeshProUGUI[] probabilityTexts;
    [SerializeField] private Color normalTextColor = new Color(0.88f, 0.88f, 0.88f);
    [SerializeField] private Color increaseTextColor = new Color(0.5f, 0.78f, 0.52f);
    [SerializeField] private Color decreaseTextColor = new Color(0.9f, 0.45f, 0.45f);

    [Header("Right Info Panel Settings")]
    [SerializeField] private GameObject infoPanelObject;
    [SerializeField] private TextMeshProUGUI faceDescText;
    [SerializeField] private Image diceIconImage;
    [SerializeField] private Image diceIconBackground;
    [SerializeField] private TextMeshProUGUI transitionProbText;
    [SerializeField] private Image confirmButtonImage;

    [Header("Default Info State")]
    [SerializeField] private string defaultDescription = "확률을 높일 주사위의 면을 선택하세요.";
    [SerializeField] private Color defaultColor = Color.gray; 

    // 사운드 관련 변수 추가
    [Header("Audio")]
    [SerializeField] private AudioClip sfxIntro;
    [SerializeField] private AudioClip sfxSelect;
    [SerializeField] private AudioClip sfxDecision;

    private float currentWeightPercent = 5f;

    private int selectedIndex = -1;
    private int currentHoverIndex = -1;
    private bool isInitialized = false;

    private void Awake()
    {
        instance = this;

        if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0f;
        if (contentCanvasGroup != null) contentCanvasGroup.alpha = 0f;

        if (mainFaceImage != null)
            mainImageInitialPos = mainFaceImage.rectTransform.anchoredPosition;

        gameObject.SetActive(false);
    }

    public void OpenBalanceUI(float incomingWeightPercent)
    {
        currentWeightPercent = incomingWeightPercent;

        isInitialized = false;
        selectedIndex = -1;
        currentHoverIndex = -1;

        if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0f;
        if (contentCanvasGroup != null) contentCanvasGroup.alpha = 0f;

        UpdateMainImage();
        UpdateProbabilityUI();
        UpdateInfoPanel();

        Time.timeScale = 0f;

        // UI 오픈 사운드 재생
        if (sfxIntro != null && SoundManager.instance != null) 
            SoundManager.instance.PlaySFX(sfxIntro, 0.6f, 0.1f);

        StopAllCoroutines();
        gameObject.SetActive(true);

        if (backgroundImage != null) StartCoroutine(LoopBackgroundAnimation());
        if (mainFaceImage != null) floatCoroutine = StartCoroutine(FloatMainImage());

        StartCoroutine(FadeInUI());
    }

    // 배경이 먼저 켜지고, 이후에 컨텐츠가 나타나는 순차 페이드인
    private IEnumerator FadeInUI()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (mainCanvasGroup != null)
                mainCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (contentCanvasGroup != null)
                contentCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        if (contentCanvasGroup != null) contentCanvasGroup.alpha = 1f;

        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized) return;

        CheckMouseHover();
        CheckMouseClick();
    }

    private void CheckMouseHover()
    {
        if (selectedIndex != -1) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        int newHoverIndex = -1;

        for (int i = 0; i < faceChoices.Length; i++)
        {
            if (faceChoices[i].hoverSensor != null &&
                RectTransformUtility.RectangleContainsScreenPoint(faceChoices[i].hoverSensor.rectTransform, mousePos, null))
            {
                newHoverIndex = i;
                break;
            }
        }

        if (currentHoverIndex != newHoverIndex)
        {
            currentHoverIndex = newHoverIndex;
            UpdateMainImage();
        }
    }

    private void CheckMouseClick()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            if (confirmButtonImage != null && confirmButtonImage.gameObject.activeInHierarchy)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(confirmButtonImage.rectTransform, mousePos, null))
                {
                    OnConfirmButtonClick();
                    return;
                }
            }

            for (int i = 0; i < faceChoices.Length; i++)
            {
                if (faceChoices[i].hoverSensor != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(faceChoices[i].hoverSensor.rectTransform, mousePos, null))
                {
                    SelectFace(i);
                    break;
                }
            }
        }
    }

    private void SelectFace(int newIndex)
    {
        if (selectedIndex == newIndex) return;

        selectedIndex = newIndex;

        // 선택 변경 시 사운드 재생
        if (sfxSelect != null && SoundManager.instance != null) 
            SoundManager.instance.PlaySFX(sfxSelect, 0.6f, 0.1f);

        UpdateMainImage();
        UpdateProbabilityUI();
        UpdateInfoPanel();
    }

    private void UpdateMainImage()
    {
        if (mainFaceImage == null) return;

        if (selectedIndex != -1)
        {
            mainFaceImage.sprite = faceChoices[selectedIndex].highlightSprite;
        }
        else if (currentHoverIndex != -1)
        {
            mainFaceImage.sprite = faceChoices[currentHoverIndex].highlightSprite;
        }
        else
        {
            mainFaceImage.sprite = normalSprite;
        }
    }

    private void UpdateProbabilityUI()
    {
        if (GameManager.instance == null || GameManager.instance.dice == null) return;
        if (probabilityTexts == null || probabilityTexts.Length < 6) return;

        float[] currentPercentages = GameManager.instance.dice.displayPercentages;

        if (selectedIndex == -1)
        {
            for (int i = 0; i < 6; i++)
            {
                if (probabilityTexts[i] != null)
                {
                    probabilityTexts[i].text = currentPercentages[i].ToString("F1") + "%";
                    probabilityTexts[i].color = normalTextColor;
                }
            }
        }
        else
        {
            float[] predictedPercentages = GameManager.instance.dice.GetPredictedPercentages(selectedIndex, currentWeightPercent);

            for (int i = 0; i < 6; i++)
            {
                if (probabilityTexts[i] != null)
                {
                    probabilityTexts[i].text = predictedPercentages[i].ToString("F1") + "%";

                    if (i == selectedIndex)
                        probabilityTexts[i].color = increaseTextColor;
                    else
                        probabilityTexts[i].color = decreaseTextColor;
                }
            }
        }
    }

    private void UpdateInfoPanel()
    {
        if (infoPanelObject == null) return;

        infoPanelObject.SetActive(true);

        if (selectedIndex != -1 && GameManager.instance != null && GameManager.instance.dice != null)
        {
            DiceData currentData = GameManager.instance.dice.diceList[selectedIndex];

            float currentPercent = GameManager.instance.dice.displayPercentages[selectedIndex];
            float[] predicted = GameManager.instance.dice.GetPredictedPercentages(selectedIndex, currentWeightPercent);
            float predictedPercent = predicted[selectedIndex];

            if (currentData != null)
            {
                if (faceDescText != null) faceDescText.text = currentData.shortDescription;

                if (diceIconImage != null)
                {
                    diceIconImage.gameObject.SetActive(true);
                    diceIconImage.sprite = currentData.icon;
                }

                if (diceIconBackground != null)
                {
                    diceIconBackground.gameObject.SetActive(true);
                    diceIconBackground.color = currentData.particleColor;
                }
            }

            // 고정 초록색 대신 currentData의 고유 색상(particleColor) 적용
            if (transitionProbText != null)
            {
                Color highlightColor = currentData != null ? currentData.particleColor : increaseTextColor;
                transitionProbText.text = $"{currentPercent:F1}% -> <color=#{ColorUtility.ToHtmlStringRGB(highlightColor)}>{predictedPercent:F1}%</color>";
            }

            if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(true);
        }
        else
        {
            if (faceDescText != null) faceDescText.text = defaultDescription;

            if (diceIconImage != null) diceIconImage.gameObject.SetActive(false);

            if (diceIconBackground != null)
            {
                diceIconBackground.gameObject.SetActive(true);
                diceIconBackground.color = defaultColor;
            }

            if (transitionProbText != null) transitionProbText.text = "";

            if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(false);
        }
    }

    public void OnConfirmButtonClick()
    {
        if (selectedIndex == -1) return;

        // 선택 확정 시 사운드 재생
        if (sfxDecision != null && SoundManager.instance != null) 
            SoundManager.instance.PlaySFX(sfxDecision, 0.7f, 0.2f);

        if (GameManager.instance != null && GameManager.instance.dice != null)
        {
            GameManager.instance.dice.AddPercentToFace(selectedIndex, currentWeightPercent);
        }

        CloseBalanceUI();
    }

    private void CloseBalanceUI()
    {
        if (!isInitialized) return;
        isInitialized = false;

        if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(false);

        StartCoroutine(FadeOutUI());
    }

    // 닫힐 때는 배경과 내부가 동시에 깔끔하게 페이드아웃
    private IEnumerator FadeOutUI()
    {
        Time.timeScale = 1.0f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float currentAlpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            
            if (mainCanvasGroup != null) mainCanvasGroup.alpha = currentAlpha;
            if (contentCanvasGroup != null) contentCanvasGroup.alpha = currentAlpha;
            
            yield return null;
        }

        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0f;
        if (contentCanvasGroup != null) contentCanvasGroup.alpha = 0f;
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
            float newY = mainImageInitialPos.y + Mathf.Sin(Time.unscaledTime * floatSpeed) * floatAmplitude;
            mainFaceImage.rectTransform.anchoredPosition = new Vector2(mainImageInitialPos.x, newY);
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