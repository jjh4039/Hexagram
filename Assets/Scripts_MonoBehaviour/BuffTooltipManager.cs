using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class BuffTooltipManager : MonoBehaviour
{
    public static BuffTooltipManager Instance;

    [Header("UI References")] [SerializeField]
    private GameObject tooltipObj;

    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private Image backgroundImage; // 툴팁 배경 이미지

    [Header("Settings")] [SerializeField] private Vector2 offset = new Vector2(15f, -15f);
    [SerializeField] private Color buffColor = new Color(0.15f, 0.15f, 0.15f, 0.95f); // 버프일 때 배경색 (어두운 회색 등)
    [SerializeField] private Color debuffColor = new Color(0.4f, 0.1f, 0.1f, 0.95f); // 디버프일 때 배경색 (어두운 붉은색 등)

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

    public void ShowTooltip(string desc, bool isDebuff = false)
    {
        if (tooltipObj == null) return;

        descText.text = desc;

        if (backgroundImage != null)
        {
            backgroundImage.color = isDebuff ? debuffColor : buffColor;
        }

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