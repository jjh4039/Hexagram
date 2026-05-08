using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoUI : MonoBehaviour
{
    public Slider healthSlider; // 화면에 표시되는 체력바
    public TextMeshProUGUI healthText; // 현재 체력과 최대 체력 문자
    public GameObject safeZoneIndicator; // 안전지대 상태를 알리는 UI 오브젝트

    private void Start()
    {
        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.OnGamePhaseChanged += HandleGamePhaseChanged;
            safeZoneIndicator.SetActive(InputStateManager.Instance.CurrentPhase == GamePhase.SafeZone);
        }
    }

    private void OnDestroy()
    {
        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.OnGamePhaseChanged -= HandleGamePhaseChanged;
        }
    }

    private void HandleGamePhaseChanged(GamePhase newPhase)
    {
        if (safeZoneIndicator != null)
        {
            safeZoneIndicator.SetActive(newPhase == GamePhase.SafeZone);
        }
    }

    public void Update()
    {
        healthSlider.maxValue = GameManager.instance.stats.maxHealth;
        healthSlider.value = GameManager.instance.stats.currentHealth;
        healthText.text = GameManager.instance.stats.currentHealth + " / " + GameManager.instance.stats.maxHealth;
    }
}