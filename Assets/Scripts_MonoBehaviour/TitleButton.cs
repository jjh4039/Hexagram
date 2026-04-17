using UnityEngine;
using UnityEngine.EventSystems;
using TMPro; // TextMeshPro 네임스페이스 추가

[RequireComponent(typeof(TextMeshProUGUI))] // 이 스크립트를 넣으면 무조건 TMP가 있어야 함을 보장
public class TitleButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private TitleManager titleManager;     // 씬에 있는 매니저 연결
    [SerializeField] private int targetIndex;               // 이 텍스트가 담당하는 메뉴 번호

    private TextMeshProUGUI _tmpText;                       // 감지할 텍스트 컴포넌트

    private void Awake()
    {
        _tmpText = GetComponent<TextMeshProUGUI>();

        // 텍스트 컴포넌트가 있다면 코드로 강제로 레이캐스트 타겟을 활성화합니다.
        if (_tmpText != null)
        {
            _tmpText.raycastTarget = true;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 마우스가 들어왔을 때, 이 오브젝트가 유효한 텍스트인지 한 번 더 검증
        if (titleManager != null && _tmpText != null)
        {
            titleManager.SetIndexByMouse(targetIndex);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (titleManager != null && _tmpText != null)
        {
            titleManager.ExecuteMenu();
        }
    }
}