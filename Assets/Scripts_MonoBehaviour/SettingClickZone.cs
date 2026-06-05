using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SettingClickZone : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private SettingUIController controller; 
    [SerializeField] private int targetIndex;                // 이 UI가 담당하는 설정 번호

    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

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