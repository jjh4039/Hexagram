using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class DashboardUI : MonoBehaviour
{
    public static DashboardUI instance;

    [Header("Main Objects")]
    public GameObject dashboardPanel;
    public CanvasGroup dashboardCG;
    public Transform artifactGrid;
    public GameObject slotPrefab;

    [Header("Indicator")]
    [SerializeField] private GameObject inventoryIndicator; 

    [Header("Animation Settings")]
    public float fadeDuration = 0.2f;
    public Vector3 startScale = new Vector3(0.9f, 0.9f, 1f);

    [Header("Tooltip")]
    public GameObject tooltipGroup;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public Vector2 tooltipOffset = new Vector2(15f, -15f);

    [Header("Tooltip Colors")]
    public string hexLegendary = "#FFD000";
    public string hexEpic = "#B591D1";
    public string hexRare = "#4AA8D8";
    public string hexNormal = "#FFFFFF";

    [Header("Sound Effects")]
    [SerializeField] private AudioClip sfxOpen;
    [SerializeField] private AudioClip sfxClose;
    [SerializeField] private AudioClip sfxHover;

    public bool isOpen = false;
    private bool isTooltipActive = false;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        instance = this;

        if (dashboardCG == null) dashboardCG = dashboardPanel.GetComponent<CanvasGroup>();
        if (dashboardCG != null)
        {
            dashboardCG.alpha = 0f;
            dashboardCG.blocksRaycasts = false;
        }

        dashboardPanel.SetActive(false);
        if (tooltipGroup != null) tooltipGroup.SetActive(false);
    }

    private void Start()
    {
        if (InputStateManager.Instance == null) return;

        var actions = InputStateManager.Instance.Actions;

        actions.Normal.Inventory.performed += OnInventoryPressed;
        actions.Combat.Inventory.performed += OnInventoryPressed;
        actions.UI.CloseInventory.performed += OnCloseUIPressed;
    }

    private void OnDestroy()
    {
        if (InputStateManager.Instance == null) return;

        var actions = InputStateManager.Instance.Actions;

        actions.Normal.Inventory.performed -= OnInventoryPressed;
        actions.Combat.Inventory.performed -= OnInventoryPressed;
        actions.UI.CloseInventory.performed -= OnCloseUIPressed;
    }

    private void Update()
    {
        if (isOpen && isTooltipActive && tooltipGroup)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            tooltipGroup.transform.position = mousePos + tooltipOffset;
        }

        UpdateIndicator();
    }

    private void UpdateIndicator()
    {
        if (!inventoryIndicator || !InputStateManager.Instance) return;

        bool canOpen = !isOpen && InputStateManager.Instance.CurrentInputState == InputState.Normal;
        
        if (inventoryIndicator.activeSelf != canOpen)
        {
            inventoryIndicator.SetActive(canOpen);
        }
    }

    private void OnInventoryPressed(InputAction.CallbackContext context)
    {
        if (isOpen) return;

        if (InputStateManager.Instance.TryOpenUI())
        {
            OpenDashboard();
        }
        else
        {
            if (PlayerFeedbackUI.Instance != null)
                PlayerFeedbackUI.Instance.ShowWarning(0);
        }
    }

    private void OnCloseUIPressed(InputAction.CallbackContext context)
    {
        if (isOpen)
        {
            CloseDashboard();
            InputStateManager.Instance.CloseUI();
        }
    }

    public void OpenDashboard()
    {
        isOpen = true;
        dashboardPanel.SetActive(true);
        Time.timeScale = 0f;

        RefreshArtifacts();
        if (SoundManager.instance) SoundManager.instance.PlaySFX(sfxOpen, 1.0f);

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(true));
    }

    public void CloseDashboard()
    {
        isOpen = false;
        HideTooltip();

        if (SoundManager.instance) SoundManager.instance.PlaySFX(sfxClose, 1.0f);

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(false));
    }

    private IEnumerator FadeRoutine(bool show)
    {
        float timer = 0f;
        float startAlpha = dashboardCG.alpha;
        float targetAlpha = show ? 1f : 0f;

        Vector3 fromScale = show ? startScale : Vector3.one;
        Vector3 toScale = show ? Vector3.one : startScale;

        dashboardCG.blocksRaycasts = show;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / fadeDuration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f);

            dashboardCG.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            dashboardPanel.transform.localScale = Vector3.Lerp(fromScale, toScale, t);

            yield return null;
        }

        dashboardCG.alpha = targetAlpha;
        dashboardPanel.transform.localScale = toScale;

        if (!show)
        {
            dashboardPanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    public void RefreshArtifacts()
    {
        if (artifactGrid == null) return;

        for (int i = artifactGrid.childCount - 1; i >= 0; i--)
        {
            Transform child = artifactGrid.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
        
        if (ArtifactManager.Instance == null) return;

        foreach (ArtifactData data in ArtifactManager.Instance.myArtifacts)
        {
            GameObject newSlot = Instantiate(slotPrefab, artifactGrid);
            newSlot.transform.localScale = Vector3.one;
            newSlot.GetComponent<ArtifactSlot>().Setup(data);
        }
    }

    public void ShowTooltip(ArtifactData data)
    {
        if (tooltipGroup == null) return;
        if (SoundManager.instance) SoundManager.instance.PlaySFX(sfxHover, 0.3f, 0.1f);

        isTooltipActive = true;
        tooltipGroup.SetActive(true);
        nameText.text = data.artifactName;

        string colorHex = (data.grade == ArtifactGrade.Legendary) ? hexLegendary :
                          (data.grade == ArtifactGrade.Epic) ? hexEpic :
                          (data.grade == ArtifactGrade.Rare) ? hexRare : hexNormal;

        descText.text = $"<color={colorHex}>[ {data.grade} ]</color>\n\n{data.description}";
    }

    public void ShowTooltipCommon(string title, string content)
    {
        if (tooltipGroup == null) return;
        if (SoundManager.instance) SoundManager.instance.PlaySFX(sfxHover, 0.3f, 0.1f);

        isTooltipActive = true;
        tooltipGroup.SetActive(true);
        nameText.text = title;
        descText.text = content;
    }

    public void HideTooltip()
    {
        if (tooltipGroup == null) return;
        isTooltipActive = false;
        tooltipGroup.SetActive(false);
    }
}