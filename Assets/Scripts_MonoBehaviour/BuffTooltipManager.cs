using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class BuffTooltipManager : MonoBehaviour
{
    public static BuffTooltipManager Instance;

    [Header("UI References")]
    [SerializeField] private GameObject tooltipObj;
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private TextMeshProUGUI descText; // ★ 제목 텍스트 삭제, 설명만 남김

    [Header("Settings")]
    [SerializeField] private Vector2 offset = new Vector2(15f, -15f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (tooltipObj != null)
        {
            tooltipObj.SetActive(false);
        }
    }

    private void Update()
    {
        if (tooltipObj != null && tooltipObj.activeSelf && Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            tooltipRect.position = mousePos + offset;
        }
    }

    public void ShowTooltip(string desc)
    {
        if (tooltipObj == null) return;

        descText.text = desc;
        tooltipObj.SetActive(true);

        if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            tooltipRect.position = mousePos + offset;
        }
    }

    public void HideTooltip()
    {
        if (tooltipObj != null)
        {
            tooltipObj.SetActive(false);
        }
    }
}