using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI instance;

    [Header("UI Objects")]
    public GameObject inventoryPopup;
    public Transform slotGrid;
    public GameObject slotPrefab;

    [Header("Tooltip Info")]
    public GameObject tooltipGroup;     // ★ [추가] 텍스트들을 묶고 있는 부모 오브젝트 (이게 마우스를 따라다님)
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;

    [Header("Tooltip Settings")]
    public Vector2 tooltipOffset = new Vector2(15f, -15f); // 마우스 포인터에서 얼마나 떨어뜨릴지

    [Header("Input System")]
    private PlayerInput inputActions;

    public bool isOpen = false;
    private bool isTooltipActive = false; // 툴팁이 켜져있는지 체크

    private void Awake()
    {
        instance = this;
        inventoryPopup.SetActive(false);

        // 시작할 때 툴팁 그룹 꺼두기
        if (tooltipGroup != null) tooltipGroup.SetActive(false);

        inputActions = new PlayerInput();
    }

    private void Update()
    {
        // ★ [추가] 툴팁이 켜져있으면 마우스 따라다니기
        // (Time.timeScale이 0이어도 Update는 돌아가므로 작동함)
        if (isOpen && isTooltipActive && tooltipGroup != null)
        {
            FollowMouse();
        }
    }

    private void FollowMouse()
    {
        // 마우스 위치 가져오기 (New Input System)
        Vector2 mousePos = Mouse.current.position.ReadValue();

        // 툴팁 위치 갱신 (마우스 위치 + 오프셋)
        tooltipGroup.transform.position = mousePos + tooltipOffset;
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Inventory.performed += OnInventoryToggle;
    }

    private void OnDisable()
    {
        inputActions.Player.Inventory.performed -= OnInventoryToggle;
        inputActions.Disable();
    }

    private void OnInventoryToggle(InputAction.CallbackContext context)
    {
        ToggleInventory();
    }

    public void ToggleInventory()
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            inventoryPopup.SetActive(true);
            Time.timeScale = 0f;
            RefreshUI();
        }
        else
        {
            inventoryPopup.SetActive(false);
            HideTooltip(); // 창 닫을 때 툴팁도 같이 끄기
            Time.timeScale = 1f;
        }
    }

    public void RefreshUI()
    {
        foreach (Transform child in slotGrid)
        {
            Destroy(child.gameObject);
        }

        List<ArtifactData> artifacts = ArtifactManager.instance.myArtifacts;

        foreach (ArtifactData data in artifacts)
        {
            GameObject newSlot = Instantiate(slotPrefab, slotGrid);
            ArtifactSlot slotLogic = newSlot.GetComponent<ArtifactSlot>();
            slotLogic.Setup(data);
        }
    }

    // ★ 툴팁 켜기 (내용 채우고, 부모 오브젝트 켜기)
    public void ShowTooltip(ArtifactData data)
    {
        if (tooltipGroup == null) return;

        isTooltipActive = true;
        tooltipGroup.SetActive(true); // 보이기

        nameText.text = data.artifactName;

        string colorHex = "#FFFFFF";
        if (data.grade == ArtifactGrade.Rare) colorHex = "#FFFF00";
        else if (data.grade == ArtifactGrade.Legendary) colorHex = "#FF0000";

        descText.text = $"<color={colorHex}>[{data.grade}]</color>\n{data.description}";
    }

    // ★ 툴팁 끄기
    public void HideTooltip()
    {
        if (tooltipGroup == null) return;

        isTooltipActive = false;
        tooltipGroup.SetActive(false); // 숨기기
    }
}