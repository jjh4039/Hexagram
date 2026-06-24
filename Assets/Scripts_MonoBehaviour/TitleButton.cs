using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TitleButton : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerClickHandler
{
    [SerializeField] private TitleManager titleManager; // 씬에 있는 매니저 연결
    [SerializeField] private int targetIndex; // 이 텍스트가 담당하는 메뉴 번호

    private TextMeshProUGUI _tmpText; // 감지할 텍스트 컴포넌트

    private void Awake()
    {
        _tmpText = GetComponent<TextMeshProUGUI>();

        if (_tmpText != null)
        {
            _tmpText.raycastTarget = true;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (titleManager != null && _tmpText != null)
        {
            titleManager.SetIndexByMouse(targetIndex);
        }
    }

    public void OnPointerMove(PointerEventData eventData)
    {
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