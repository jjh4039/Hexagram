using UnityEngine;
using UnityEngine.EventSystems;

public class ShopHoverAreaRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public enum HoverAreaType { Main, Reroll }

    [SerializeField] private ShopStatOptionHoverSystem parentSystem;
    [SerializeField] private HoverAreaType areaType;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (parentSystem != null)
            parentSystem.SetHover(areaType, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (parentSystem != null)
            parentSystem.SetHover(areaType, false);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (parentSystem == null || eventData.button != PointerEventData.InputButton.Left) return;

        if (areaType == HoverAreaType.Main)
        {
            parentSystem.OnClickMain();   // 메인 클릭 -> 구매 실행
        }
        else if (areaType == HoverAreaType.Reroll)
        {
            parentSystem.OnClickReroll(); // 리롤 클릭 -> 리롤 실행
        }
    }
}