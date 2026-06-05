using UnityEngine;
using UnityEngine.EventSystems;

public class ConfirmClickZone : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private ConfirmUIController controller; 
    [SerializeField] private int targetIndex;                // 0: 예, 1: 아니오

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (controller != null) controller.SetIndexByMouse(targetIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller != null) controller.ExecuteSelection();
    }
}