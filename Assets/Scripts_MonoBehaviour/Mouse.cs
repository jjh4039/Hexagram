using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI; // Image 제어를 위해 추가 (선택사항)

public class VirtualCursor : MonoBehaviour
{
    private PlayerInput inputActions;
    private RectTransform rectTransform; // UI는 Transform 대신 이걸 씁니다

    private void Awake()
    {
        inputActions = new PlayerInput();
        rectTransform = GetComponent<RectTransform>(); // RectTransform 가져오기

        // 실제 시스템 마우스 커서 숨기기 (필요하면 주석 해제)
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined; // 화면 밖으로 못 나가게 가두기
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
        // 1. 마우스 화면 좌표 가져오기
        Vector2 mouseScreenPos = inputActions.Player.Look.ReadValue<Vector2>();

        // 2. 좌표 변환 없이 바로 적용 (Screen Space - Overlay 기준)
        if (rectTransform != null)
        {
            rectTransform.position = mouseScreenPos;
        }
        else
        {
            // 혹시 RectTransform이 아닐 경우를 대비한 보험
            transform.position = mouseScreenPos;
        }
    }
}