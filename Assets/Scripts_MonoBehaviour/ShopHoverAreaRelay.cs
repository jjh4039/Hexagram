using UnityEngine;
using UnityEngine.EventSystems;

public class ShopHoverAreaRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum HoverAreaType
    {
        Main,
        Reroll
    }

    [SerializeField] private ShopStatOptionHoverSystem owner;
    [SerializeField] private HoverAreaType areaType;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner != null)
            owner.SetHover(areaType, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (owner != null)
            owner.SetHover(areaType, false);
    }
}