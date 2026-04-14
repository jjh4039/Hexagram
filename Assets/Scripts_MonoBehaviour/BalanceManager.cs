using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BalanceManager : MonoBehaviour
{
    public static BalanceManager instance;

    [SerializeField] private CanvasGroup mainCanvasGroup;
    [SerializeField] private float fadeDuration = 0.3f; // 페이드 인/아웃에 공통으로 사용할 시간

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

    [Header("Confirm Button Settings")]
    [SerializeField] private Image confirmButtonImage;

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

    public void OpenBalanceUI()
    {
        isInitialized = false;
        selectedIndex = -1;
        currentHoverIndex = -1;

        if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0f;

        UpdateMainImage();

        Time.timeScale = 0f;

        StopAllCoroutines();
        gameObject.SetActive(true);

        if (backgroundImage != null) StartCoroutine(LoopBackgroundAnimation());

        if (mainFaceImage != null)
            floatCoroutine = StartCoroutine(FloatMainImage());

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

        if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(true);
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

    public void OnConfirmButtonClick()
    {
        if (selectedIndex == -1) return;

        if (GameManager.instance != null && GameManager.instance.dice != null)
        {
            // 이제 가중치(티켓 수)가 아니라 퍼센트(%)를 넘겨줍니다.
            float percentAmount = 5f;
            GameManager.instance.dice.AddPercentToFace(selectedIndex, percentAmount);
            Debug.Log($"[무게추] {selectedIndex + 1}번 면 확률 {percentAmount}% 증가 처리");
        }
        else
        {
            Debug.LogError("GameManager 또는 Dice 스크립트를 찾을 수 없습니다!");
        }

        CloseBalanceUI();
    }

    private void CloseBalanceUI()
    {
        if (!isInitialized) return;
        isInitialized = false;

        if (confirmButtonImage != null) confirmButtonImage.gameObject.SetActive(false);

        // 꺼질 때는 페이드아웃 코루틴 실행
        StartCoroutine(FadeOutUI());
    }

    // 부드럽게 사라지는 페이드아웃 코루틴
    private IEnumerator FadeOutUI()
    {
        // 페이드 아웃이 시작됨과 동시에 게임 시간을 다시 흐르게 합니다.
        Time.timeScale = 1.0f;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // timeScale이 1.0이 되어도 unscaledDeltaTime은 영향을 받지 않습니다.
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