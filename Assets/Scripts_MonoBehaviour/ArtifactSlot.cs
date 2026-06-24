using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

        if (_data != null && DashboardUI.instance != null)
        {
            DashboardUI.instance.ShowTooltip(_data);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (outlineObj != null) outlineObj.SetActive(false);

        if (DashboardUI.instance != null)
        {
            DashboardUI.instance.HideTooltip();
        }
    }
}