using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// 아티팩트 슬롯에 마우스를 올렸을 때의 시각적 반응과 툴팁을 담당합니다.
public class ArtifactSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject outlineObj; 

    private ArtifactData _data;

    public void Setup(ArtifactData data)
    {
        _data = data;
        if (outlineObj) outlineObj.SetActive(false); 

        if (_data)
        {
            iconImage.sprite = _data.icon;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.enabled = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) 
    {
        if (outlineObj != null) outlineObj.SetActive(true);

        // ★ 수정: DashboardUI가 존재하는지 안전 검사 추가
        if (_data != null && DashboardUI.instance != null)
        {
               DashboardUI.instance.ShowTooltip(_data);
        }
    }

    public void OnPointerExit(PointerEventData eventData)  
    {
        if (outlineObj != null) outlineObj.SetActive(false);

        // ★ 수정: DashboardUI 안전 검사 추가
        if (DashboardUI.instance != null)
        {
            DashboardUI.instance.HideTooltip();
        }
    }
}