using UnityEngine;
using TMPro;

// [역할] 스탯창 패널이 켜질 때 플레이어의 스탯 정보를 가져와 10개의 텍스트 UI에 반영합니다.
public class Panel_Stats : MonoBehaviour
{
    [Header("Player Reference")]
    public PlayerStats playerStats; // 참조할 플레이어 스탯 (게임매니저를 통해 동적으로 가져와도 됩니다)

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
        // 플레이어 정보가 없으면 에러 방지
        if (playerStats == null) return;

        // 1. 생존 및 이동 (10 / 10, 100% 형태)
        hpText.text = $"{playerStats.currentHealth} / {playerStats.maxHealth}";
        moveSpeedText.text = $"{playerStats.moveSpeed * 100f}%";

        // 2. 기본 공격
        meleeAtkText.text = $"{playerStats.meleeAttackPower}";
        rangedAtkText.text = $"{playerStats.rangeAttackPower}";
        atkSpeedText.text = $"{playerStats.attackSpeed * 100f}%";

        // 3. 치명타 및 증폭
        critChanceText.text = $"{playerStats.criticalChance * 100f}%";
        critDamageText.text = $"{playerStats.GetFinalCriticalDamageMultiplier() * 100f}%";
        finalDamageText.text = $"{playerStats.finalAttackPower}%"; // 임시 (최종 피해 증폭값으로 수정 필요)

        // 4. 주사위 (소수점 등 필요한 형태로 가공하기 쉽게 기본값만 세팅)
        diceChargeText.text = $"{playerStats.dicePassiveChargeRate}/s";
        diceAmpText.text = $"{playerStats.diceDamageMultiplier * 100f}%"; 
    }
}