using UnityEngine;

public class Sword : MonoBehaviour
{
    [SerializeField] private PlayerInput inputActions; // 직접 만든 에셋 클래스

    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        // 1단계에서 만든 것과 동일하게 초기화
        inputActions = new PlayerInput();
        spriteRenderer = GetComponent<SpriteRenderer>();
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
        RotateWeapon();
    }

    private void RotateWeapon()
    {
        float offset = 1.2f; // 플레이어와 검 사이의 간격

        // New Input System 방식으로 마우스 스크린 좌표 읽기
        Vector2 mouseScreenPos = inputActions.Player.Look.ReadValue<Vector2>();

        // 월드 좌표로 변환
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        // 피벗에서 마우스를 향하는 방향 벡터
        Vector2 lookDir = mouseWorldPos - (Vector2)transform.position;

        // 각도 계산 (Atan2는 라디안을 반환하므로 Deg로 변환)
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;

        // 360도 회전 적용
        transform.rotation = Quaternion.Euler(0, 0, angle);

       

        // 디테일: 검이 뒤집혔을 때 스프라이트 반전 처리 (가독성 유지)
        Vector3 localScale = Vector3.one;
        if (angle > 90 || angle < -90)
        {
            localScale.y = -0.8f; // 검의 위아래를 뒤집어줌
            spriteRenderer.transform.localPosition = new Vector3(offset, -2f, 0);
        }
        else
        {
            localScale.y = 0.8f;
            spriteRenderer.transform.localPosition = new Vector3(-offset, -2f, 0);
        }
        transform.localScale = localScale;

        // 유니티 각도 기준: 0도(오른쪽), 90도(위), 180도(왼쪽), 270도(아래)
        // 0도 ~ 180도 사이일 때는 캐릭터의 '뒤(위쪽)'에 있는 셈입니다.
        if (angle > 0 && angle < 180)
        {
            spriteRenderer.sortingOrder = -1;
        }
        // 그 외(180~360도)는 캐릭터의 '앞(아래쪽)'에 있는 셈입니다.
        else
        {
            spriteRenderer.sortingOrder = 1;
        }
    }
}

