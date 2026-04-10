using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Image 컴포넌트를 사용하기 위해 추가

// [역할] 지정된 UI 오브젝트의 마우스 호버 이벤트를 감지하여 하이라이트 이미지의 투명도 조절 및 툴팁을 출력합니다.
public class StatHoverTrigger : MonoBehaviour
{
    [Header("Detection Area")]
    [Tooltip("마우스를 감지할 UI 오브젝트 (투명 이미지 등). 비워두면 이 스크립트가 붙은 오브젝트를 사용합니다.")]
    public GameObject hoverArea; 

    [Header("Hover UI (Alpha Control)")]
    [Tooltip("마우스를 올렸을 때 투명도가 1이 될 하이라이트 이미지 컴포넌트")]
    public Image selectImage; // 게임 오브젝트 대신 Image 컴포넌트를 직접 받습니다.

    [Header("Tooltip Info")]
    public string statTitle;               // 툴팁 제목
    [TextArea] public string statDesc;     // 툴팁 설명

    private void Start()
    {
        // 시작 시 하이라이트 이미지의 알파값을 0(투명)으로 설정합니다.
        SetSelectAlpha(0f);

        // 1. 감지할 타겟 설정 (할당 안 했으면 자기 자신으로 세팅)
        GameObject targetArea = hoverArea != null ? hoverArea : gameObject;

        // 2. 타겟 오브젝트에 EventTrigger가 없으면 추가
        EventTrigger trigger = targetArea.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = targetArea.AddComponent<EventTrigger>();
        }

        // 3. PointerEnter (마우스 진입) 이벤트 연결
        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener((data) => { OnHoverEnter(); });
        trigger.triggers.Add(enterEntry);

        // 4. PointerExit (마우스 이탈) 이벤트 연결
        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((data) => { OnHoverExit(); });
        trigger.triggers.Add(exitEntry);
    }

    // 마우스가 영역에 들어왔을 때 실행
    private void OnHoverEnter()
    {
        SetSelectAlpha(1f); // 불투명하게(1)
        if (DashboardUI.instance != null) DashboardUI.instance.ShowTooltipCommon(statTitle, statDesc);
    }

    // 마우스가 영역에서 나갔을 때 실행
    private void OnHoverExit()
    {
        SetSelectAlpha(0f); // 투명하게(0)
        if (DashboardUI.instance != null) DashboardUI.instance.HideTooltip();
    }

    // 투명도를 설정하는 헬퍼 함수
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