using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuffSlotUI : MonoBehaviour
{
    [SerializeField] private Image[] iconImage;
    [SerializeField] private Image cooldownFillImage;
    [SerializeField] private TextMeshProUGUI stackText;

    [Header("Icon Settings")]
    [SerializeField] private Vector3 diceIconScale = Vector3.one;
    [SerializeField] private Vector3 artifactIconScale = new Vector3(0.8f, 0.8f, 1f);

    private ActiveBuff currentBuff;

    public void Setup(ActiveBuff buff)
    {
        currentBuff = buff;
        Sprite targetIcon = null;
        Vector3 targetScale = Vector3.one;

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
        else if (buff.debuffType != StageDebuffType.None && buff.debuffIcon)
        {
            // ★ 신규 추가: 디버프 아이콘 설정
            targetIcon = buff.debuffIcon;
            targetScale = artifactIconScale;
        }

        if (targetIcon)
        {
            iconImage[0].sprite = targetIcon;
            iconImage[1].sprite = targetIcon;

            iconImage[0].rectTransform.localScale = targetScale;
            iconImage[1].rectTransform.localScale = targetScale;
        }

        // ★ 신규 추가: 디버프일 경우 테두리/아이콘을 붉은색으로 강조
        if (buff.isDebuff)
        {
            iconImage[1].color = new Color(1f, 0.3f, 0.3f, 1f); // 눈에 띄는 붉은색
        }
        else
        {
            iconImage[1].color = Color.white;
        }

        gameObject.SetActive(true);
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (currentBuff == null) return;

        // ★ 수정됨: 스테이지 지속 디버프라면 Fill 쿨타임 효과를 꺼버립니다 (그림자 없음)
        if (currentBuff.isInfinite || currentBuff.isStageDuration)
        {
            cooldownFillImage.fillAmount = 0f;
            iconImage[1].fillAmount = 0f;
        }
        else if (currentBuff.maxTime > 0)
        {
            cooldownFillImage.fillAmount = 1 - (currentBuff.remainingTime / currentBuff.maxTime);
            iconImage[1].fillAmount = 1 - (currentBuff.remainingTime / currentBuff.maxTime);
        }

        // 텍스트 출력 로직
        if (currentBuff.isStageDuration)
        {
            // ★ 신규 추가: 디버프는 남은 스테이지 횟수를 출력 (예: "3턴", "3방")
            stackText.text = $"{currentBuff.remainingStages}회";
            stackText.color = new Color(1f, 0.3f, 0.3f, 1f); // 텍스트도 붉은색으로
        }
        else if (currentBuff.isInfinite)
        {
            stackText.text = "";
        }
        else if (currentBuff.buffData)
        {
            stackText.color = Color.white; // 일반 버프는 흰색 복구
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
        else if (currentBuff.artifactData)
        {
            stackText.color = Color.white;
            float baseValue = currentBuff.artifactData.value;
            if (currentBuff.artifactData.isPercent) baseValue *= 100f;

            float finalValue = baseValue * currentBuff.stackCount;
            stackText.text = $"+{finalValue:0}%";
        }
    }
}