using UnityEngine;
using UnityEngine.EventSystems;

public class BalanceTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("설정")]
    public int highlightIndex; // 몇 번째 스프라이트를 보여줄지 (0~6)

    [Header("툴팁 내용")]
    public string title;       // 예: "질서의 영역 (I)"
    [TextArea] public string description; // 예: "확률 16%"

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 1. 이미지 교체 요청
        if (BalancePanel.instance != null)
        {
            BalancePanel.instance.SetHighlight(highlightIndex);
        }

        // 2. 툴팁 띄우기
        if (DashboardUI.instance != null)
        {
            DashboardUI.instance.ShowTooltipCommon(title, description);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 1. 이미지 원상복구 요청
        if (BalancePanel.instance != null)
        {
            BalancePanel.instance.ResetToNormal();
        }

        // 2. 툴팁 끄기
        if (DashboardUI.instance != null)
        {
            DashboardUI.instance.HideTooltip();
        }
    }
}