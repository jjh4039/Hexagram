using UnityEngine;

public class Sword : MonoBehaviour
{
    [SerializeField] private PlayerInput inputActions; // 직접 만든 에셋 클래스

    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator anim;

    [Header("Var")]
    private Vector2 mouseScreenPos;
    private Vector2 mouseWorldPos;

    private void Awake()
    {
        inputActions = new PlayerInput();
        inputActions.Player.Attack.performed += ctx => Attack(); // 공격 버튼 이벤트 연결

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
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

        mouseScreenPos = inputActions.Player.Look.ReadValue<Vector2>();
        mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
    }

    private void Attack()
    {
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Sword_Attack")) return; // 이미 공격 중이면 무시

        anim.SetTrigger("Attack");

        GameManager.instance.player.isAttacking = true; // 플레이어 공격 설정 (이동 제한 및 공격 전진 로직)
        GameManager.instance.player.rigid.linearVelocity = Vector2.zero; // 공격 시 기존 속도 제거

        float attackDashForce = 7f;

        Vector2 mouseScreenPos = inputActions.Player.Look.ReadValue<Vector2>();
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector2 pushDir = (mouseWorldPos - (Vector2)GameManager.instance.player.transform.position).normalized;

        GameManager.instance.player.rigid.AddForce(pushDir * attackDashForce, ForceMode2D.Impulse);

        Invoke("ResetAttackStatus", 0.2f);
    }

    private void RotateWeapon()
    {
        float offset = 0f;

        // 1. 방향 및 각도 계산
        Vector2 lookDir = mouseWorldPos - (Vector2)transform.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;

        // 2. 부모(Pivot) 회전 적용
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 3. 방향에 따른 반전 및 위치 조정
        Vector3 pivotScale = Vector3.one;
        if (angle > 90 || angle < -90) // 왼쪽
        {
            pivotScale.y = 1f; // 자식의 애니메이션 궤적까지 반전시키기 위해 부모 스케일 조절
            spriteRenderer.transform.localPosition = new Vector3(offset, 0, 0);
        }
        else // 오른쪽
        {
            pivotScale.y = -1f;
            spriteRenderer.transform.localPosition = new Vector3(offset, 0, 0);
        }

        // 부모나 중간 루트의 스케일을 조절하여 궤적 반전
        transform.localScale = pivotScale;
    }

    private void ResetAttackStatus()
    {
        GameManager.instance.player.isAttacking = false;
    }
}

