using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class BuffSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image[] iconImage;
    [SerializeField] private Image cooldownFillImage;
    [SerializeField] private TextMeshProUGUI stackText;

    [Header("Icon Settings")]
    [SerializeField] private Vector3 diceIconScale = Vector3.one;
    [SerializeField] private Vector3 artifactIconScale = new Vector3(0.8f, 0.8f, 1f);

    private ActiveBuff currentBuff;

    private bool isHovering = false;

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

        if (buff.isDebuff)
        {
            iconImage[1].color = new Color(1f, 0.3f, 0.3f, 1f);
        }
        else
        {
            iconImage[1].color = new Color(0.25f, 0.25f, 0.25f, 1f);
        }

        gameObject.SetActive(true);
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (currentBuff == null) return;

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

        if (currentBuff.isStageDuration)
        {
            if (currentBuff.debuffType == StageDebuffType.TakeMoreDamage)
            {
                stackText.text = $"-{currentBuff.debuffValue:0}%";
            }
            else
            {
                stackText.text = $"{currentBuff.remainingStages}회";
            }
            stackText.color = new Color(1f, 0.3f, 0.3f, 1f);
        }
        else if (currentBuff.isInfinite)
        {
            stackText.text = "";
        }
        else if (currentBuff.buffData)
        {
            stackText.color = Color.white;
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

    private void Update()
    {
        if (isHovering)
        {
            if (currentBuff == null || !gameObject.activeSelf)
            {
                isHovering = false;
                if (BuffTooltipManager.Instance != null) BuffTooltipManager.Instance.HideTooltip();
                return;
            }

            UpdateTooltipContent();
        }
    }

    private void OnDisable()
    {
        if (isHovering)
        {
            isHovering = false;
            if (BuffTooltipManager.Instance != null)
            {
                BuffTooltipManager.Instance.HideTooltip();
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentBuff == null) return;

        isHovering = true;
        UpdateTooltipContent();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (BuffTooltipManager.Instance != null)
        {
            BuffTooltipManager.Instance.HideTooltip();
        }
    }

    private void UpdateTooltipContent()
    {
        if (BuffTooltipManager.Instance == null) return;

        string desc = "";

        // 주사위 버프 설명
        if (currentBuff.buffData != null)
        {
            desc = currentBuff.buffData.description;
        }
        // 아티팩트 버프 설명
        else if (currentBuff.artifactData != null)
        {
            desc = currentBuff.artifactData.description;
        }
        // 스테이지 디버프
        else if (currentBuff.isStageDuration && currentBuff.isDebuff)
        {
            switch (currentBuff.debuffType)
            {
                case StageDebuffType.DiceEffectHalf:
                    desc = $"앞으로 {currentBuff.remainingStages}스테이지(전투) 동안\n획득하는 주사위 효과가 절반이 됩니다.";
                    break;
                case StageDebuffType.TakeMoreDamage:
                    desc = $"앞으로 {currentBuff.remainingStages}스테이지(전투) 동안\n받는 피해가 {currentBuff.debuffValue}% 증가합니다.";
                    break;
                case StageDebuffType.CannotHeal:
                    desc = $"앞으로 {currentBuff.remainingStages}스테이지(전투) 동안\n체력을 회복할 수 없습니다.";
                    break;
            }
        }

        if (!string.IsNullOrEmpty(desc))
        {
            BuffTooltipManager.Instance.ShowTooltip(desc);
        }
        else
        {
            BuffTooltipManager.Instance.HideTooltip();
        }
    }
}