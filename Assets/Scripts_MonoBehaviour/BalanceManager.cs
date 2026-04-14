using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class BalanceManager : MonoBehaviour
{
    public static BalanceManager instance;

    [SerializeField] private CanvasGroup mainCanvasGroup;
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
    [SerializeField] private Color defaultColor = Color.gray; // 빈 케이스를 위해 다시 추가됨

    private float currentWeightPercent = 5f;

    private int selectedIndex = -1;
    private int currentHoverIndex = -1;
    private bool isInitialized = false;

    private void Awake()
    {
        instance = this;

        if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0f;

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

        UpdateMainImage();
        UpdateProbabilityUI();
        UpdateInfoPanel();

        Time.timeScale = 0f;

        StopAllCoroutines();
        gameObject.SetActive(true);

        if (backgroundImage != null) StartCoroutine(LoopBackgroundAnimation());
        if (mainFaceImage != null) floatCoroutine = StartCoroutine(FloatMainImage());

        StartCoroutine(FadeInUI());
    }

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
            // 호버 시 정보창 업데이트 안함
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

        // 1. 특정 면이 클릭(선택) 되었을 때
        if (selectedIndex != -1 && GameManager.instance != null && GameManager.instance.dice != null)
        {
            DiceData currentData = GameManager.instance.dice.diceList[selectedIndex];

            float currentPercent = GameManager.instance.dice.displayPercentages[selectedIndex];
            float[] predicted = GameManager.instance.dice.GetPredictedPercentages(selectedIndex, currentWeightPercent);
            float predictedPercent = predicted[selectedIndex];

            if (currentData != null)
            {
                if (faceDescText != null) faceDescText.text = currentData.shortDescription;

                // 안쪽 주사위 아이콘 활성화
                if (diceIconImage != null)
                {
                    diceIconImage.gameObject.SetActive(true);
                    diceIconImage.sprite = currentData.icon;
                }

                // 바깥쪽 케이스 활성화 및 색상 지정
                if (diceIconBackground != null)
                {
                    diceIconBackground.gameObject.SetActive(true);
                    diceIconBackground.color = currentData.particleColor;
                }
            }

            if (transitionProbText != null)
            {
                transitionProbText.text = $"{currentPercent:F1}% -> <color=#{ColorUtility.ToHtmlStringRGB(increaseTextColor)}>{predictedPercent:F1}%</color>";
            }

            if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(true);
        }
        // 2. 아무것도 선택되지 않은 디폴트 상태
        else
        {
            if (faceDescText != null) faceDescText.text = defaultDescription;

            // ★ 안쪽 주사위 아이콘만 비활성화 ★
            if (diceIconImage != null) diceIconImage.gameObject.SetActive(false);

            // ★ 바깥쪽 케이스는 켜두고 기본 색상 적용 ★
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

    private IEnumerator FadeOutUI()
    {
        Time.timeScale = 1.0f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (mainCanvasGroup != null)
                mainCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            yield return null;
        }

        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0f;
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