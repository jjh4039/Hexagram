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

    private int comboStep = 0;         // 현재 콤보 단계 (0~3)
    private float lastClickTime;       // 마지막 클릭 시점
    private readonly float comboWaitTime = 0.2f; // 콤보를 유지해주는 대기 시간

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
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Sword_Idle")) // 마우스 검 회전 (비공격 상태에서만)
        {
            RotateWeapon();
        }

        mouseScreenPos = inputActions.Player.Look.ReadValue<Vector2>();
        mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
    }

    private void Attack()
    {
        // 1. 콤보 시간 만료 체크 (일정 시간 안 누르면 1타부터 다시)
        if (Time.time - lastClickTime > comboWaitTime)
        {
            comboStep = 0;
        }

        // 2. 현재 애니메이션이 재생 중일 때 너무 빠른 연타 방지 (선택 사항)
        // 만약 3타가 끝난 직후라면 잠시 대기하게 할 수 있습니다.
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Sword_Attack 3")) return;

        // 3. 콤보 단계 상승
        comboStep++;
        RotateWeapon();
        if (comboStep > 3) comboStep = 1; // 3타 후 다시 누르면 1타로

        ExecuteAttack();
    }

    private void ExecuteAttack()
    {
        // 애니메이터 파라미터 갱신
        anim.SetInteger("comboStep", comboStep);
        anim.SetTrigger("Attack");

        lastClickTime = Time.time; // 클릭 시간 기록

        // 플레이어 이동 제어 로직
        GameManager.instance.player.isAttacking = true;
        GameManager.instance.player.rigid.linearVelocity = Vector2.zero;

        // 콤보 단계별로 다른 대시 힘 적용 (3타는 더 강력하게!)
        float force = (comboStep == 3) ? 12f : 7f;

        Vector2 pushDir = (mouseWorldPos - (Vector2)GameManager.instance.player.transform.position).normalized;
        GameManager.instance.player.rigid.AddForce(pushDir * force, ForceMode2D.Impulse);

        // 공격 상태 리셋 (애니메이션 길이에 맞춰 조정)
        CancelInvoke("ResetAttackStatus");
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

