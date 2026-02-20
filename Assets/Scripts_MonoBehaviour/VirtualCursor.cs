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
    private PlayerInput inputActions;
    private RectTransform rectTransform;
    private Image cursorImage;

    [Header("Cursor Settings")]
    [Tooltip(" 0: Default, 1: Aim")]
    [SerializeField] private Sprite[] cursorSprites;

    private void Awake()
    {
        inputActions = new PlayerInput();
        rectTransform = GetComponent<RectTransform>();
        cursorImage = GetComponent<Image>(); // Image 컴포넌트 참조

        // 실제 시스템 마우스 커서 숨기기
        Cursor.visible = false;

        // 마우스 잠그기
        // Cursor.lockState = CursorLockMode.Confined; 

        // 시작할 때 기본 커서로 초기화
        ChangeCursor(CursorType.Default);
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        // 마우스 화면 좌표 가져오기
        Vector2 mouseScreenPos = inputActions.Player.Look.ReadValue<Vector2>();

        // 좌표 변환 없이 바로 적용 (Screen Space - Overlay 기준)
        if (rectTransform != null)
        {
            rectTransform.position = mouseScreenPos;
        }
        else
        {
            transform.position = mouseScreenPos;
        }
    }

    // 2. 외부에서 호출하여 커서 모양과 피벗을 바꾸는 메서드
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