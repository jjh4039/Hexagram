using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuffSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownFillImage;
    [SerializeField] private TextMeshProUGUI stackText;

    private ActiveBuff currentBuff;

    public void Setup(ActiveBuff buff)
    {
        currentBuff = buff;

        if (buff.buffData.icon != null)
        {
            iconImage.sprite = buff.buffData.icon;
            cooldownFillImage.sprite = buff.buffData.icon;
        }

        gameObject.SetActive(true);
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (currentBuff == null) return;

        // 남은 시간 게이지 갱신 (Filled Image의 fillAmount 조절)
        if (currentBuff.maxTime > 0)
        {
            cooldownFillImage.fillAmount = currentBuff.remainingTime / currentBuff.maxTime;
        }

        // 스택 수에 따른 최종 효과값 계산
        float finalEffectValue = currentBuff.buffData.effectValue * currentBuff.stackCount;

        // 버프 타입별 텍스트 하드코딩 표기
        switch (currentBuff.buffData.effectType)
        {
            case DiceEffectType.StrongAttackBuff:
                // 강공격: 남은 횟수 표기 (예: "3회")
                stackText.text = $"{currentBuff.remainingCount}회";
                break;

            case DiceEffectType.AttackBuff:
            case DiceEffectType.CritDamageBuff:
            case DiceEffectType.SpeedBuff:
                // 스탯 증가류: 최종 증가 수치를 %로 표기 (예: "+50%", "+1200%")
                // ToString("0")을 사용해 소수점 아래는 깔끔하게 쳐냅니다.
                stackText.text = $"+{finalEffectValue:0}%";
                break;

            case DiceEffectType.RangedMegaBuff:
                stackText.text = $"+{finalEffectValue:0}00%";
                break;

            default:
                // 그 외의 경우 (힐 등) 스택이 2 이상일 때만 배수로 표기
                if (currentBuff.stackCount > 1)
                {
                    stackText.text = $"x{currentBuff.stackCount}";
                }
                else
                {
                    stackText.text = ""; // 1스택이면 숨김
                }
                break;
        }
    }
}