using UnityEngine;
using UnityEngine.EventSystems;

public class SettingAdjustButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private SettingUIController controller;
    [SerializeField] private int direction = 1; // 1은 값 증가, -1은 값 감소

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller != null && controller.IsOpen)
        {
            controller.AdjustValue(direction);
        }
    }
}