using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; 

public class StatHoverTrigger : MonoBehaviour
{
    [Header("Detection Area")]
    public GameObject hoverArea; 

    [Header("Hover UI (Alpha Control)")]
    public Image selectImage; 

    [Header("Tooltip Info")]
    public string statTitle;               
    [TextArea] public string statDesc;     

    private void Start()
    {
        SetSelectAlpha(0f);

        GameObject targetArea = hoverArea != null ? hoverArea : gameObject;

        EventTrigger trigger = targetArea.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = targetArea.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener((data) => { OnHoverEnter(); });
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((data) => { OnHoverExit(); });
        trigger.triggers.Add(exitEntry);
    }

    private void OnDisable()
    {
        SetSelectAlpha(0f);
        if (DashboardUI.instance != null) DashboardUI.instance.HideTooltip();
    }

    private void OnHoverEnter()
    {
        SetSelectAlpha(1f); 
        if (DashboardUI.instance != null) DashboardUI.instance.ShowTooltipCommon(statTitle, statDesc);
    }

    private void OnHoverExit()
    {
        SetSelectAlpha(0f); 
        if (DashboardUI.instance != null) DashboardUI.instance.HideTooltip();
    }

    private void SetSelectAlpha(float alpha)
    {
        if (selectImage != null)
        {
            Color tempColor = selectImage.color;
            tempColor.a = alpha;
            selectImage.color = tempColor;
        }
    }
}