using UnityEngine;
using UnityEngine.EventSystems;

public class BalanceTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("설정")]
    public int highlightIndex; // 강조 표시할 주사위 면 번호

    private Dice _targetDice; // 데이터 연동을 위한 주사위 참조

    private void Start()
    {
        if (GameManager.instance != null)
        {
            _targetDice = GameManager.instance.dice; // 게임매니저에서 주사위 연결
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (BalancePanel.instance != null)
        {
            BalancePanel.instance.SetHighlight(highlightIndex);
        }

        if (DashboardUI.instance != null && _targetDice != null)
        {
            if (highlightIndex >= 0 && highlightIndex < _targetDice.diceList.Length)
            {
                DiceData data = _targetDice.diceList[highlightIndex];
                float percent = _targetDice.displayPercentages[highlightIndex];

                string hexColor = $"#{data.particleColor.r:X2}{data.particleColor.g:X2}{data.particleColor.b:X2}"; // 파티클 컬러를 16진수 코드로 변환

                string title = $"주사위 : < <color={hexColor}>{highlightIndex + 1} </color>>";

                string desc = $"{data.description}\n\n" +
                              $"면 발동 확률 : <color={hexColor}>{percent:F1}%</color>";

                DashboardUI.instance.ShowTooltipCommon(title, desc);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (BalancePanel.instance != null)
        {
            BalancePanel.instance.ResetToNormal();
        }

        if (DashboardUI.instance != null)
        {
            DashboardUI.instance.HideTooltip();
        }
    }
}