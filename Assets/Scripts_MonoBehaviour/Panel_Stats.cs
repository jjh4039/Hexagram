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
        moveSpeedText.text = $"{(playerStats.moveSpeed / 5f) * 100f}%";

        meleeAtkText.text = $"{playerStats.meleeAttackPower:F2}";
        rangedAtkText.text = $"{playerStats.rangeAttackPower:F2}";
        atkSpeedText.text = $"{playerStats.attackSpeed * 100f}%";

        critChanceText.text = $"{playerStats.criticalChance * 100f}%";
        critDamageText.text = $"{playerStats.GetFinalCriticalDamageMultiplier() * 100f}%";
        finalDamageText.text = $"{playerStats.finalAttackPower * 100f}%";

        diceChargeText.text = $"{playerStats.dicePassiveChargeRate}/s";
        diceAmpText.text = $"{playerStats.finalDicePower * 100f}%";
    }
}