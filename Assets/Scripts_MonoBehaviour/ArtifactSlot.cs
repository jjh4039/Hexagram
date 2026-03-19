using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ArtifactSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject outlineObj; // ★ [추가] 아웃라인 오브젝트

    private ArtifactData _data;

    public void Setup(ArtifactData data)
    {
        _data = data;
        if (outlineObj != null) outlineObj.SetActive(false); // 시작할 땐 끄기

        if (_data != null)
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
        // ★ 마우스 들어오면 아웃라인 켜기
        if (outlineObj != null) outlineObj.SetActive(true);

        if (_data != null)
        {
            DashboardUI.instance.ShowTooltip(_data);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // ★ 마우스 나가면 아웃라인 끄기
        if (outlineObj != null) outlineObj.SetActive(false);

        DashboardUI.instance.HideTooltip();
    }
}