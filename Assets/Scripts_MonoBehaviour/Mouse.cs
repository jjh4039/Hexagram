using UnityEngine;
using UnityEngine.InputSystem;

public class VirtualCursor : MonoBehaviour
{
    private PlayerInput inputActions; // 아까 만든 에셋 클래스 이름

    private void Awake()
    {
        inputActions = new PlayerInput();

        // Cursor.visible = false; 마우스 커서 숨기기

    }

    private void OnEnable()
    {
        inputActions.Enable(); // 입력 활성화
    }

    private void OnDisable()
    {
        inputActions.Disable(); // 입력 비활성화
    }

    private void Update()
    {
        // 마우스 위치 로직
        Vector2 mouseScreenPos = inputActions.Player.Look.ReadValue<Vector2>();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 10f));

        transform.position = worldPos;
    }
}
