using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum CursorType
{
    Default,   
    Aim,   
}

public class VirtualCursor : MonoBehaviour
{
    private RectTransform _rectTransform; // 커서의 UI 위치 및 피벗 제어용
    private Image _cursorImage; // 커서 이미지를 표시할 컴포넌트

    [Header("Cursor Settings")]
    [Tooltip(" 0: Default, 1: Aim")]
    [SerializeField] private Sprite[] cursorSprites; // 커서 상태별 이미지 배열

    public CursorType CurrentCursorType { get; private set; } // 현재 활성화된 커서 상태

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _cursorImage = GetComponent<Image>();

        Cursor.visible = false;
        
        ChangeCursor(CursorType.Default);
    }

    private void Update()
    {
        Vector2 mouseScreenPos = Vector2.zero; // 현재 마우스 스크린 좌표

        if (InputStateManager.Instance != null)
        {
            var state = InputStateManager.Instance.CurrentInputState;
            var actions = InputStateManager.Instance.Actions;

            if (state == InputState.Normal)
                mouseScreenPos = actions.Normal.Look.ReadValue<Vector2>();
            else if (state == InputState.Combat)
                mouseScreenPos = actions.Combat.Look.ReadValue<Vector2>();
            else if (Mouse.current != null)
            {
                mouseScreenPos = Mouse.current.position.ReadValue(); 
                ChangeCursor(default);
            }
        }
        else if (Mouse.current != null)
        {
            mouseScreenPos = Mouse.current.position.ReadValue(); 
        }

        if (_rectTransform)
        {
            _rectTransform.position = mouseScreenPos;
        }
        else
        {
            transform.position = mouseScreenPos;
        }
    }

    public void ChangeCursor(CursorType type)
    {
        if (!_cursorImage || cursorSprites.Length == 0) return;
        CurrentCursorType = type; // 외부에서 참조할 수 있도록 상태 저장

        switch (type)
        {
            case CursorType.Default:
                if (cursorSprites.Length > 0) _cursorImage.sprite = cursorSprites[0];
                _rectTransform.pivot = new Vector2(0f, 1f);
                break;

            case CursorType.Aim:
                if (cursorSprites.Length > 1) _cursorImage.sprite = cursorSprites[1];
                _rectTransform.pivot = new Vector2(0.5f, 0.5f);
                break;

            default:
                Debug.Log("정의되지 않은 커서 타입입니다.");
                break;
        }
    }
}