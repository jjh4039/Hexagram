using UnityEngine;
using UnityEngine.EventSystems;

public class PauseClickZone : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private PauseUIController controller;   
    [SerializeField] private int targetIndex;                // 0: 계속, 1: 설정, 2: 포기, 3: 종료

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (controller != null) controller.SetIndexByMouse(targetIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller != null) controller.ExecuteSelection();
    }
}