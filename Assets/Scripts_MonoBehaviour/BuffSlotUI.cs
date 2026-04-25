using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuffSlotUI : MonoBehaviour
{
    [SerializeField] private Image[] iconImage;
    [SerializeField] private Image cooldownFillImage;
    [SerializeField] private TextMeshProUGUI stackText;

    [Header("Icon Settings")]
    [SerializeField] private Vector3 diceIconScale = Vector3.one;                     // 주사위 아이콘 스케일
    [SerializeField] private Vector3 artifactIconScale = new Vector3(0.8f, 0.8f, 1f); // 아티팩트 아이콘 스케일

    private ActiveBuff currentBuff;

    public void Setup(ActiveBuff buff)
    {
        currentBuff = buff;
        Sprite targetIcon = null;                                                     // 표시할 아이콘 임시 변수
        Vector3 targetScale = Vector3.one;                                            // 표시할 스케일 임시 변수

        // 주사위 버프인지, 아티팩트 버프인지 판별하여 아이콘 및 스케일 할당
        if (buff.buffData && buff.buffData.icon)
        {
            targetIcon = buff.buffData.icon;
            targetScale = diceIconScale;
        }
        else if (buff.artifactData && buff.artifactData.icon)
        {
            targetIcon = buff.artifactData.icon;
            targetScale = artifactIconScale;
        }

        if (targetIcon)
        {
            iconImage[0].sprite = targetIcon;
            iconImage[1].sprite = targetIcon;

            iconImage[0].rectTransform.localScale = targetScale;
            iconImage[1].rectTransform.localScale = targetScale;
        }

        gameObject.SetActive(true);
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (currentBuff == null) return;

        // 남은 시간 게이지 갱신
        if (currentBuff.maxTime >= 9999f)
        {
            cooldownFillImage.fillAmount = 1f;                                        // 무한일 경우 게이지 풀 유지
            iconImage[1].fillAmount = 1f;
        }
        else if (currentBuff.maxTime > 0)
        {
            cooldownFillImage.fillAmount = 1 - (currentBuff.remainingTime / currentBuff.maxTime);
            iconImage[1].fillAmount = 1 - (currentBuff.remainingTime / currentBuff.maxTime);
        }

        // 1. 주사위 버프일 경우의 텍스트 처리 (기존 로직 유지)
        if (currentBuff.buffData)
        {
            float finalEffectValue = currentBuff.buffData.effectValue * currentBuff.stackCount;

            switch (currentBuff.buffData.effectType)
            {
                case DiceEffectType.StrongAttackBuff:
                    stackText.text = $"{currentBuff.remainingCount}회";
                    break;

                case DiceEffectType.AttackBuff:
                case DiceEffectType.CritDamageBuff:
                case DiceEffectType.SpeedBuff:
                    stackText.text = $"+{finalEffectValue:0}%";
                    break;

                case DiceEffectType.RangedMegaBuff:
                    stackText.text = $"+{finalEffectValue:0}00%";
                    break;

                default:
                    stackText.text = currentBuff.stackCount > 1 ? $"x{currentBuff.stackCount}" : "";
                    break;
            }
        }
        // 2. 아티팩트 버프일 경우의 텍스트 처리 (신규 로직)
        else if (currentBuff.artifactData)
        {
            float baseValue = currentBuff.artifactData.value;

            // 퍼센트 수치(0.1 등)라면 100을 곱해 표기 수치(10)로 변환
            if (currentBuff.artifactData.isPercent)
            {
                float finalPercent = (baseValue * 100f) * currentBuff.stackCount;
                stackText.text = $"+{finalPercent:0}%";
            }
            else
            {
                // 고정 수치(합연산)의 경우 배수로 표기 (예: 중첩 시 x2, x3)
                stackText.text = currentBuff.stackCount > 1 ? $"x{currentBuff.stackCount}" : "";
            }
        }
    }
}