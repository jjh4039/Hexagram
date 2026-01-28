using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ArtifactSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image frameImage;

    private ArtifactData _data;

    public void Setup(ArtifactData data)
    {
        _data = data;

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

    // ∏∂øÏΩ∫ ø√∏≤ -> ƒ—¡‡!
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_data != null)
        {
            DashboardUI.instance.ShowTooltip(_data);
        }
    }

    // ∏∂øÏΩ∫ ≥ª∏≤ -> ≤®¡‡!
    public void OnPointerExit(PointerEventData eventData)
    {
        DashboardUI.instance.HideTooltip();
    }
}