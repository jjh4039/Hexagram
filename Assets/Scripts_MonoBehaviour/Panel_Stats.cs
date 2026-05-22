using UnityEngine;
using TMPro;

public class Panel_Stats : MonoBehaviour
{
    [Header("Player Reference")]
    public PlayerStats playerStats; // 참조할 플레이어 스탯

    [Header("10 Stat Texts")]
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI moveSpeedText;
    public TextMeshProUGUI meleeAtkText;
    public TextMeshProUGUI rangedAtkText;
    public TextMeshProUGUI atkSpeedText;
    public TextMeshProUGUI critChanceText;
    public TextMeshProUGUI critDamageText;
    public TextMeshProUGUI finalDamageText;
    public TextMeshProUGUI diceChargeText;
    public TextMeshProUGUI diceAmpText;

    private void OnEnable()
    {
        UpdateStatTexts();
    }

    private void UpdateStatTexts()
    {
        if (playerStats == null) return;

        hpText.text = $"{playerStats.currentHealth} / {playerStats.maxHealth}";
        
        // "0.#" 포맷을 사용하여 정수면 깔끔하게, 소수점이 있으면 1자리까지만 표기합니다.
        moveSpeedText.text = $"{(playerStats.moveSpeed / 5f * 100f).ToString("0.#")}%";

        meleeAtkText.text = playerStats.meleeAttackPower.ToString("0.#");
        rangedAtkText.text = playerStats.rangeAttackPower.ToString("0.#");
        atkSpeedText.text = $"{(playerStats.attackSpeed * 100f).ToString("0.#")}%";

        critChanceText.text = $"{(playerStats.criticalChance * 100f).ToString("0.#")}%";
        critDamageText.text = $"{(playerStats.GetFinalCriticalDamageMultiplier() * 100f).ToString("0.#")}%";
        finalDamageText.text = $"{(playerStats.finalAttackPower * 100f).ToString("0.#")}%";

        diceChargeText.text = $"{playerStats.dicePassiveChargeRate.ToString("0.#")}/s";
        diceAmpText.text = $"{(playerStats.finalDicePower * 100f).ToString("0.#")}%";
    }
}