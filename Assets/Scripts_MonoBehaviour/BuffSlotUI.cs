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

    // ★ 상시 감지를 위한 상태 플래그
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

    // ★ 1. 매 프레임 버프 상태를 상시 감지
    private void Update()
    {
        if (isHovering)
        {
            // 버프 데이터가 사라졌거나 UI가 비활성화되었다면 즉시 툴팁 끄기
            if (currentBuff == null || !gameObject.activeSelf)
            {
                isHovering = false;
                if (BuffTooltipManager.Instance != null) BuffTooltipManager.Instance.HideTooltip();
                return;
            }

            // 스택이 바뀌는 등 실시간 데이터 변화를 툴팁에 계속 반영
            UpdateTooltipContent();
        }
    }

    // ★ 2. 버프 시간이 다 되어 UI가 꺼질 때를 대비한 안전 장치
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
        if (currentBuff == null || currentBuff.artifactData != null) return;

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

    // ★ 3. 툴팁 내용을 생성하고 띄우는 역할 분리
    private void UpdateTooltipContent()
    {
        if (BuffTooltipManager.Instance == null) return;

        string desc = "";

        if (currentBuff.buffData != null)
        {
            desc = currentBuff.buffData.description;
        }
        else if (currentBuff.isStageDuration && currentBuff.isDebuff)
        {
            switch (currentBuff.debuffType)
            {
                case StageDebuffType.DiceEffectHalf:
                    desc = $"앞으로 {currentBuff.remainingStages}스테이지 동안 획득하는 주사위 효과가 절반이 됩니다.";
                    break;
                case StageDebuffType.TakeMoreDamage:
                    desc = $"앞으로 {currentBuff.remainingStages}스테이지 동안 받는 피해가 {currentBuff.debuffValue}% 증가합니다.";
                    break;
                case StageDebuffType.CannotHeal:
                    desc = $"앞으로 {currentBuff.remainingStages}스테이지 동안 체력을 회복할 수 없습니다.";
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