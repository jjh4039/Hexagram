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
    private RectTransform rectTransform;
    private Image cursorImage;

    [Header("Cursor Settings")]
    [Tooltip(" 0: Default, 1: Aim")]
    [SerializeField] private Sprite[] cursorSprites;

    private void Awake()
    {
        // [수정] 독자적인 inputActions = new PlayerInput(); 생성 삭제
        rectTransform = GetComponent<RectTransform>();
        cursorImage = GetComponent<Image>();

        Cursor.visible = false;
        
        // 마우스 잠그기 (필요 시 주석 해제)
        // Cursor.lockState = CursorLockMode.Confined; 

        ChangeCursor(CursorType.Default);
    }

    // [수정] OnEnable, OnDisable에서 개별 입력 시스템 켜고 끄던 로직 삭제

    private void Update()
    {
        Vector2 mouseScreenPos = Vector2.zero;

        // [수정] 매니저를 통해 현재 상태에 맞는 마우스 좌표를 가져옵니다.
        if (InputStateManager.Instance != null)
        {
            var state = InputStateManager.Instance.CurrentInputState;
            var actions = InputStateManager.Instance.Actions;

            if (state == InputState.Normal)
                mouseScreenPos = actions.Normal.Look.ReadValue<Vector2>();
            else if (state == InputState.Combat)
                mouseScreenPos = actions.Combat.Look.ReadValue<Vector2>();
            else if (Mouse.current != null)
                mouseScreenPos = Mouse.current.position.ReadValue(); // UI 상태 등 맵이 꺼졌을 때의 안전장치
        }
        else if (Mouse.current != null)
        {
            mouseScreenPos = Mouse.current.position.ReadValue(); // 매니저가 없을 때의 초기값
        }

        // 좌표 적용
        if (rectTransform != null)
        {
            rectTransform.position = mouseScreenPos;
        }
        else
        {
            transform.position = mouseScreenPos;
        }
    }

    public void ChangeCursor(CursorType type)
    {
        if (cursorImage == null || cursorSprites.Length == 0) return;

        switch (type)
        {
            case CursorType.Default:
                if (cursorSprites.Length > 0) cursorImage.sprite = cursorSprites[0];
                rectTransform.pivot = new Vector2(0f, 1f);
                break;

            case CursorType.Aim:
                if (cursorSprites.Length > 1) cursorImage.sprite = cursorSprites[1];
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                break;

            default:
                Debug.Log("정의되지 않은 커서 타입입니다.");
                break;
        }
    }
}