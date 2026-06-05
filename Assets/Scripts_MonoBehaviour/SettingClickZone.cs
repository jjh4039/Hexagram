using UnityEngine;
using UnityEngine.EventSystems;

public class SettingClickZone : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private SettingUIController controller; 
    [SerializeField] private int targetIndex;                // 이 UI가 담당하는 설정 번호

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (controller != null && controller.IsOpen)
        {
            controller.SetIndexByMouse(targetIndex);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller != null && controller.IsOpen)
        {
            controller.SetIndexByMouse(targetIndex);
        }
    }
}