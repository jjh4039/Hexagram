using UnityEngine;
using UnityEngine.InputSystem;

public class Sword : MonoBehaviour
{
    private WeaponManager weaponManager;
    private SpriteRenderer spriteRenderer;
    private Animator anim;

    [SerializeField] private GameObject[] slashEffects;
    [SerializeField] private Transform swordImageTr;

    private Vector2 mouseWorldPos;
    private int comboStep = 0;
    private float lastClickTime;
    private readonly float comboWaitTime = 0.2f;

    private void Awake()
    {
        weaponManager = GetComponentInParent<WeaponManager>(); // 부모 매니저 참조
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        // [핵심] 공격 버튼 이벤트 연결 (여기서 한 번만 진행하거나 OnEnable/Disable 활용)
        weaponManager.InputActions.Player.Attack.performed += ctx => Attack();
    }

    private void Update()
    {
        // 마우스 위치 갱신
        Vector2 mouseScreenPos = weaponManager.InputActions.Player.Look.ReadValue<Vector2>();
        mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        // 비공격 상태에서만 회전
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Sword_Idle"))
        {
            RotateWeapon();
        }
    }

    private void OnEnable()
    {
        // 활성화 시 애니메이션 초기화 (위치, 회전 초기화)
        if (anim != null)
        {
            anim.Rebind();
            anim.Play("Sword_Idle", -1, 0f);

            anim.Update(0f);
        }
    }

    private void Attack()
    {
        // [주의] 스왑되어 비활성화된 상태일 때는 공격이 무시되어야 함
        if (!gameObject.activeInHierarchy) return;

        if (Time.time - lastClickTime > comboWaitTime) comboStep = 0;
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Sword_Attack 3")) return;

        comboStep++;
        RotateWeapon();
        if (comboStep > 3) comboStep = 1;

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
        float force = (comboStep == 3) ? 3f : 1.5f;

        Vector2 pushDir = (mouseWorldPos - (Vector2)GameManager.instance.player.transform.position).normalized;
        GameManager.instance.player.rigid.AddForce(pushDir * force, ForceMode2D.Impulse);

        // 공격 상태 리셋 (애니메이션 길이에 맞춰 조정)
        CancelInvoke("ResetAttackStatus");
        Invoke("ResetAttackStatus", 0.2f);

        // 검기 생성
        GameObject currentEffect = slashEffects[comboStep - 1];

        float spawnOffset = 0.4f + (comboStep * 0.25f);
        currentEffect.transform.position = (Vector2)transform.position + (pushDir * spawnOffset);

        float angle = Mathf.Atan2(pushDir.y, pushDir.x) * Mathf.Rad2Deg;

        if (transform.localScale.y < 0)
        {
            currentEffect.transform.rotation = Quaternion.Euler(0, 0, angle + 95f);
        }
        else
        {
            currentEffect.transform.rotation = Quaternion.Euler(0, 0, angle - 95f);
        }

        Vector3 effectScale = Vector3.one * 1.5f;

        if (transform.localScale.y < 0) effectScale.y *= -1;

        currentEffect.transform.localScale = effectScale;

        currentEffect.SetActive(false);
        currentEffect.SetActive(true);
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

