using UnityEngine;
using UnityEngine.EventSystems;

public class ConfirmClickZone : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerClickHandler
{
    [SerializeField] private ConfirmUIController controller; 
    [SerializeField] private int targetIndex;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (controller != null) controller.SetIndexByMouse(targetIndex);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (controller != null) controller.SetIndexByMouse(targetIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller != null) controller.ExecuteSelection();
    }
}