using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class DashboardUI : MonoBehaviour
{
    public static DashboardUI instance;

    [Header("Main Objects")]
    public GameObject dashboardPanel;   // 전체 팝업 (검은 배경 + 판)
    public Transform artifactGrid;      // ★ 아티팩트가 생성될 그리드 (Panel_Artifacts 안의 Grid)
    public GameObject slotPrefab;       // 슬롯 프리팹

    [Header("Tooltip")]
    public GameObject tooltipGroup;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public Vector2 tooltipOffset = new Vector2(15f, -15f);

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
        // ★ Player 맵의 Inventory 액션(Tab) 구독
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
            RefreshArtifacts(); // 켜질 때 슬롯 갱신
        }
        else
        {
            dashboardPanel.SetActive(false);
            HideTooltip();
            Time.timeScale = 1f;
        }
    }

    public void RefreshArtifacts()
    {
        // 기존 슬롯 삭제
        foreach (Transform child in artifactGrid) Destroy(child.gameObject);

        // 매니저에서 데이터 가져와서 생성
        foreach (ArtifactData data in ArtifactManager.instance.myArtifacts)
        {
            GameObject newSlot = Instantiate(slotPrefab, artifactGrid);
            newSlot.GetComponent<ArtifactSlot>().Setup(data);
        }
    }

    public void ShowTooltip(ArtifactData data)
    {
        if (tooltipGroup == null) return;
        isTooltipActive = true;
        tooltipGroup.SetActive(true);
        nameText.text = data.artifactName;

        string colorHex = (data.grade == ArtifactGrade.Legendary) ? "#FF0000" :
                          (data.grade == ArtifactGrade.Rare) ? "#FFFF00" : "#FFFFFF";

        descText.text = $"<color={colorHex}>[{data.grade}]</color>\n{data.description}";
    }

    public void HideTooltip()
    {
        if (tooltipGroup == null) return;
        isTooltipActive = false;
        tooltipGroup.SetActive(false);
    }
}