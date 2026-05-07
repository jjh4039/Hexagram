using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuffSlotUI : MonoBehaviour
{
    [SerializeField] private Image[] iconImage;
    [SerializeField] private Image cooldownFillImage;
    [SerializeField] private TextMeshProUGUI stackText;

    [Header("Icon Settings")] [SerializeField]
    private Vector3 diceIconScale = Vector3.one; // 주사위 아이콘 스케일

    [SerializeField] private Vector3 artifactIconScale = new Vector3(0.8f, 0.8f, 1f); // 아티팩트 아이콘 스케일

    private ActiveBuff currentBuff;

    public void Setup(ActiveBuff buff)
    {
        currentBuff = buff;
        Sprite targetIcon = null; // 표시할 아이콘 임시 변수
        Vector3 targetScale = Vector3.one; // 표시할 스케일 임시 변수

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

        // ★ 수정됨: 1f(100% 가림)가 아니라 0f(그림자 없음)로 설정해야 밝게 보입니다.
        if (currentBuff.isInfinite)
        {
            cooldownFillImage.fillAmount = 0f;
            iconImage[1].fillAmount = 0f;
        }
        else if (currentBuff.maxTime > 0)
        {
            cooldownFillImage.fillAmount = 1 - (currentBuff.remainingTime / currentBuff.maxTime);
            iconImage[1].fillAmount = 1 - (currentBuff.remainingTime / currentBuff.maxTime);
        }

        if (currentBuff.isInfinite)
        {
            stackText.text = ""; // 무한 지속 버프는 텍스트 미출력
        }
        else if (currentBuff.buffData)
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
        else if (currentBuff.artifactData)
        {
            float baseValue = currentBuff.artifactData.value;
            if (currentBuff.artifactData.isPercent) baseValue *= 100f; // 소수점 입력 시 100을 곱함

            float finalValue = baseValue * currentBuff.stackCount;
            stackText.text = $"+{finalValue:0}%"; // 아티팩트 버프는 퍼센트로 출력
        }
    }
}