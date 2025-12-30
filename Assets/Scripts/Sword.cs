using UnityEngine;
using UnityEngine.InputSystem;

public class Sword : MonoBehaviour
{
    private WeaponManager weaponManager;
    private SpriteRenderer spriteRenderer;
    private Animator anim;

    [SerializeField] private GameObject[] slashEffects;

    [Header("Timing Settings")]
    [SerializeField] private float activeDuration = 0.25f;
    [SerializeField] private float inputBufferTime = 0.5f;

    [Header("Stats")]
    [SerializeField] private float attackSpeed = 1.0f; // 공속 배율

    private float nextAttackUnlockTime = 0f;
    private float lastInputTime = -10f;
    private float lastAttackStartTime = 0f;
    private int comboStep = 0;

    private Vector2 mouseWorldPos;

    private void Awake()
    {
        weaponManager = GetComponentInParent<WeaponManager>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        if (anim != null) anim.Rebind();
        if (weaponManager?.InputActions != null)
            weaponManager.InputActions.Player.Attack.performed += OnAttackInput;
    }

    private void OnDisable()
    {
        if (weaponManager?.InputActions != null)
            weaponManager.InputActions.Player.Attack.performed -= OnAttackInput;
    }

    private void OnAttackInput(InputAction.CallbackContext context)
    {
        lastInputTime = Time.time;
        TryAttack();
    }

    private void Update()
    {
        Vector2 mouseScreenPos = weaponManager.InputActions.Player.Look.ReadValue<Vector2>();
        mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        // ▼▼▼ [IDLE 복귀 시 리셋] ▼▼▼
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        bool isIdle = stateInfo.IsName("Base Layer.Sword_Idle") || stateInfo.IsName("Sword_Idle");

        // 1. IDLE 상태이고
        // 2. 트랜지션(0.1초 섞이는 구간)이 완전히 끝났을 때만! (꼬임 방지 핵심)
        if (isIdle && !anim.IsInTransition(0))
        {
            // 방금 공격(0.1초 내)한 게 아닐 때만 안전하게 리셋
            if (Time.time - lastAttackStartTime > 0.1f)
            {
                comboStep = 0;
                anim.speed = 1f;
                anim.ResetTrigger("Attack");
            }
            RotateWeapon();
        }

        TryAttack();
    }

    private void TryAttack()
    {
        if (Time.time - lastInputTime > inputBufferTime) return;
        if (weaponManager.IsSwapping) return;
        if (weaponManager.CurrentWeapon != WeaponManager.WeaponType.Sword) return;

        // [요청하신 수정] 3타까지 쳤으면 더 이상 공격 불가 (IDLE 복귀 대기)
        if (comboStep >= 3) return;

        // A구간(공격 중) 체크
        if (Time.time < nextAttackUnlockTime) return;

        ExecuteAttack();
    }

    private void ExecuteAttack()
    {
        lastInputTime = -10f; // 입력 소모

        float resetThreshold = 0.33f / attackSpeed;

        if (Time.time - lastAttackStartTime > resetThreshold)
        {
            comboStep = 0;
        }

        // 3타 넘어가면 0으로
        if (comboStep >= 3) comboStep = 0;

        comboStep++;

        // 시간 기록
        lastAttackStartTime = Time.time;

        anim.speed = attackSpeed;
        nextAttackUnlockTime = Time.time + (activeDuration / attackSpeed);

        // 1타 강제 실행 (씹힘 방지)
        if (comboStep == 1)
        {
            anim.Play("Sword_Attack", -1, 0f); // 1타 강제 재생
            anim.ResetTrigger("Attack");
        }
        else
        {
            anim.ResetTrigger("Attack");
            anim.SetInteger("comboStep", comboStep);
            anim.SetTrigger("Attack");
        }

        RotateWeapon();
        ApplyPhysics();
        SpawnSlashEffect();
    }

    private void ApplyPhysics()
    {
        GameManager.instance.player.isAttacking = true;
        GameManager.instance.player.rigid.linearVelocity = Vector2.zero;
        float force = (comboStep == 3) ? 1.5f : 0.75f;
        Vector2 pushDir = (mouseWorldPos - (Vector2)GameManager.instance.player.transform.position).normalized;
        GameManager.instance.player.rigid.AddForce(pushDir * force, ForceMode2D.Impulse);
        CancelInvoke("ResetAttackStatus");
        Invoke("ResetAttackStatus", 0.2f / attackSpeed);
    }
    private void SpawnSlashEffect()
    {
        Vector2 dir = (mouseWorldPos - (Vector2)transform.position).normalized;
        GameObject currentEffect = slashEffects[comboStep - 1];
        float spawnOffset = 0.4f + (comboStep * 0.25f);
        currentEffect.transform.position = (Vector2)transform.position + (dir * spawnOffset);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (transform.localScale.y < 0) currentEffect.transform.rotation = Quaternion.Euler(0, 0, angle + 95f);
        else currentEffect.transform.rotation = Quaternion.Euler(0, 0, angle - 95f);
        Vector3 effectScale = Vector3.one * 1.5f;
        if (transform.localScale.y < 0) effectScale.y *= -1;
        currentEffect.transform.localScale = effectScale;
        currentEffect.SetActive(false);
        currentEffect.SetActive(true);
    }
    private void RotateWeapon()
    {
        float offset = 0f;
        Vector2 lookDir = mouseWorldPos - (Vector2)transform.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        Vector3 pivotScale = Vector3.one;
        if (angle > 90 || angle < -90) { pivotScale.y = 1f; spriteRenderer.transform.localPosition = new Vector3(offset, 0, 0); }
        else { pivotScale.y = -1f; spriteRenderer.transform.localPosition = new Vector3(offset, 0, 0); }
        transform.localScale = pivotScale;
    }
    private void ResetAttackStatus()
    {
        GameManager.instance.player.isAttacking = false;
    }
}