using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class DashboardUI : MonoBehaviour
{
    public static DashboardUI instance;

    [Header("Main Objects")]
    public GameObject dashboardPanel;
    public Transform artifactGrid;
    public GameObject slotPrefab;

    [Header("Tooltip")]
    public GameObject tooltipGroup;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public Vector2 tooltipOffset = new Vector2(15f, -15f);

    [Header("Sound Effects")] // ★ [추가] 사운드 클립 변수
    [SerializeField] private AudioClip sfxOpen;   // 1. 창 열릴 때 (철컥!)
    [SerializeField] private AudioClip sfxClose;  // 1. 창 닫힐 때 (탁.)
    [SerializeField] private AudioClip sfxHover;  // 2. 툴팁 뜰 때 (틱/띠링)

    private PlayerInput inputActions;
    public bool isOpen = false;
    private bool isTooltipActive = false;

    private void Awake()
    {
        instance = this;
        dashboardPanel.SetActive(false);
        if (tooltipGroup != null) tooltipGroup.SetActive(false);
        inputActions = new PlayerInput();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Inventory.performed += OnToggle;
    }

    private void OnDisable()
    {
        inputActions.Player.Inventory.performed -= OnToggle;
        inputActions.Disable();
    }

    private void Update()
    {
        if (isOpen && isTooltipActive && tooltipGroup != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            tooltipGroup.transform.position = mousePos + tooltipOffset;
        }
    }

    private void OnToggle(InputAction.CallbackContext context)
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            dashboardPanel.SetActive(true);
            Time.timeScale = 0f;
            RefreshArtifacts();

            // ★ [추가] 열림 소리 재생 (볼륨 1.0f)
            SoundManager.instance.PlaySFX(sfxOpen, 1.0f);
        }
        else
        {
            dashboardPanel.SetActive(false);
            HideTooltip();
            Time.timeScale = 1f;

            // ★ [추가] 닫힘 소리 재생
            SoundManager.instance.PlaySFX(sfxClose, 1.0f);
        }
    }

    public void RefreshArtifacts()
    {
        foreach (Transform child in artifactGrid) Destroy(child.gameObject);
        foreach (ArtifactData data in ArtifactManager.instance.myArtifacts)
        {
            GameObject newSlot = Instantiate(slotPrefab, artifactGrid);
            newSlot.GetComponent<ArtifactSlot>().Setup(data);
        }
    }

    // 아티팩트용 툴팁
    public void ShowTooltip(ArtifactData data)
    {
        if (tooltipGroup == null) return;

        // ★ [추가] 툴팁이 '새로' 켜질 때만 소리 재생 (이미 켜져있는데 내용만 바뀌는 경우 제외)
        // 만약 내용 바뀔 때마다 소리나게 하려면 if문 빼고 재생하세요.
        if (!isTooltipActive)
        {
            SoundManager.instance.PlaySFX(sfxHover, 0.3f, 0.1f); // 볼륨 살짝 작게 (0.6)
        }

        isTooltipActive = true;
        tooltipGroup.SetActive(true);
        nameText.text = data.artifactName;
        string colorHex = (data.grade == ArtifactGrade.Legendary) ? "#FF0000" :
                          (data.grade == ArtifactGrade.Rare) ? "#FFFF00" : "#FFFFFF";
        descText.text = $"<color={colorHex}>[{data.grade}]</color>\n\n{data.description}";
    }

    // 밸런스/스탯용 공용 툴팁
    public void ShowTooltipCommon(string title, string content)
    {
        if (tooltipGroup == null) return;

        // ★ [추가] 툴팁 소리 재생
        if (!isTooltipActive)
        {
            SoundManager.instance.PlaySFX(sfxHover, 0.3f, 0.1f);
        }

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