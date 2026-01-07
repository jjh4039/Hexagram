using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] public Rigidbody2D rigid;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PlayerInput inputActions;
    [SerializeField] private Animator anim;
    [SerializeField] private PlayerStats stats;

    [Header("Movement")]
    [SerializeField] private float defaultMoveSpeed = 5f;
    [SerializeField] private float minMoveSpeed = 1f;
    [SerializeField] private float speedChangeRate = 5f;

    private float currentMoveSpeed;
    private Vector2 moveInput;

    [Header("State")]
    public bool isAttacking = false;
    public bool isCharging = false;

    [Header("Hit & Invincibility")]
    [SerializeField] private bool isInvincible = false;
    [SerializeField] private float invincibleTime = 1.0f;
    [SerializeField] private float blinkSpeed = 0.2f;
    [SerializeField] private float bodyContactDamage = 5f;

    [Header("--- Dice Buff Status (New!) ---")]
    // 주사위 효과로 변하는 배율들 (기본값 1.0)
    public float damageMultiplier = 1.0f;     // 공격력 배율 (빨강)
    public float moveSpeedMultiplier = 1.0f;  // 이동 속도 배율 (파랑)
    public float attackSpeedMultiplier = 1.0f;// 공격 속도 배율 (파랑)
    public float chargeSpeedMultiplier = 1.0f;// 충전 속도 배율 (보라)

    [Header("Weapon Link")]
    [SerializeField] private WeaponManager weaponManager;

    // 다음 2회 강한 공격 (주황)
    public int remainingStrongAttacks = 0;

    private Color paleRed = new Color(1f, 0.3f, 0.3f, 1f);

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        inputActions = new PlayerInput();
        stats = GetComponent<PlayerStats>();
        anim = GetComponentInChildren<Animator>();

        currentMoveSpeed = defaultMoveSpeed;

        rigid.gravityScale = 0;
        rigid.interpolation = RigidbodyInterpolation2D.Interpolate;

        rigid.sleepMode = RigidbodySleepMode2D.NeverSleep;
    }

    private void OnEnable() { inputActions.Enable(); }
    private void OnDisable() { inputActions.Disable(); }

    private void Update()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        LookAtMouse();
        HandleSpeedInterpolation();
    }

    private void FixedUpdate()
    {
        if (!isAttacking) Move();
    }

    // ★ [핵심] 주사위 효과 적용 로직
    public void ApplyDiceBuff(DiceData data)
    {
        // 1. 기존 일시적 버프 초기화 (중첩 방지)
        RemoveDiceBuff();

        Debug.Log($"[Dice Effect] 타입: {data.effectType} / 수치: {data.effectValue}");

        // ★ [수정] 매니저에게 색상 변경 요청 (총이 꺼져 있어도 작동함)
        if (weaponManager != null)
        {
            weaponManager.UpdateWeaponVisuals(data.particleColor, data.muzzleFlashMaterial);
        }

        switch (data.effectType)
        {
            case DiceEffectType.AttackBuff: // 1. (빨강) 체력 1 감소, 공격력 10% 증가
                if (stats != null) stats.TakeDamage(1); // 체력 감소
                damageMultiplier = 1.0f + (data.effectValue / 100f); // 10 -> 1.1배
                break;

            case DiceEffectType.CriticalBuff: // 2. (주황) 다음 N회 강한 공격
                remainingStrongAttacks = (int)data.effectValue; // 2회
                break;

            case DiceEffectType.GrowthBuff: // 3. (노랑) 영구 공격력 상승
                if (stats != null)
                {
                    float growthFactor = 1.0f + (data.effectValue / 100f); // 3 -> 1.03배
                    stats.meleeAttackPower *= growthFactor;
                    stats.rangeAttackPower *= growthFactor;
                    Debug.Log($"영구 성장! 근거리: {stats.meleeAttackPower}, 원거리: {stats.rangeAttackPower}");
                }
                break;

            case DiceEffectType.Heal: // 4. (초록) 체력 회복
                if (stats != null)
                {
                    // 최대 체력을 넘지 않도록 회복
                    stats.currentHealth = Mathf.Min(stats.currentHealth + (int)data.effectValue, stats.maxHealth);
                    Debug.Log($"체력 회복: 현재 {stats.currentHealth}");
                }
                break;

            case DiceEffectType.SpeedBuff: // 5. (파랑) 이속, 공속 증가
                moveSpeedMultiplier = 1.0f + (data.effectValue / 100f);
                attackSpeedMultiplier = 1.0f + (data.effectValue / 100f);
                break;

            case DiceEffectType.ChargingBuff: // 6. (보라) 충전 속도 증가
                chargeSpeedMultiplier = data.effectValue; // 6 입력 시 6배
                break;
        }
    }

    // ★ 버프 해제 (지속 시간이 끝나면 호출됨)
    public void RemoveDiceBuff()
    {
        damageMultiplier = 1.0f;
        moveSpeedMultiplier = 1.0f;
        attackSpeedMultiplier = 1.0f;
        chargeSpeedMultiplier = 1.0f;
        // remainingStrongAttacks는 횟수제라 여기서 초기화하지 않음 (전략적 선택)

        if (weaponManager != null)
        {
            // 일단 null을 보내면 재질은 안 바꿈 (색상만 빨강 복구)
            weaponManager.UpdateWeaponVisuals(Color.white, null);
        }
    }

    private void HandleSpeedInterpolation()
    {
        float targetSpeed = isCharging ? minMoveSpeed : defaultMoveSpeed;
        currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, targetSpeed, Time.deltaTime * speedChangeRate);
    }

    public void SetChargingState(bool _isCharging)
    {
        isCharging = _isCharging;
        if (anim != null) anim.SetBool("IsCharging", isCharging);
    }

    public void SetDiceAnimation(int diceIndex)
    {
        if (anim != null) anim.SetFloat("DiceType", (float)diceIndex);
    }

    public void OnDamage(float damage)
    {
        if (isInvincible) return;
        if (stats != null) stats.TakeDamage((int)damage);
        StartCoroutine(Co_OnHit());
    }

    private void OnCollisionStay2D(Collision2D collision) { CheckContactDamage(collision.gameObject); }
    private void OnTriggerStay2D(Collider2D collision) { CheckContactDamage(collision.gameObject); }
    private void CheckContactDamage(GameObject target)
    {
        if (isInvincible) return;
        if (target.CompareTag("Enemy")) OnDamage(bodyContactDamage);
    }

    IEnumerator Co_OnHit()
    {
        isInvincible = true;
        float timer = 0f;
        bool isRed = false;
        while (timer < invincibleTime)
        {
            spriteRenderer.color = isRed ? Color.white : paleRed;
            isRed = !isRed;
            yield return new WaitForSeconds(blinkSpeed);
            timer += blinkSpeed;
        }
        spriteRenderer.color = Color.white;
        isInvincible = false;
    }

    private void Move()
    {
        // ★ 이동 속도에 배율(moveSpeedMultiplier) 적용
        if (moveInput.magnitude > 0)
        {
            float finalSpeed = currentMoveSpeed * moveSpeedMultiplier;
            rigid.linearVelocity = moveInput.normalized * finalSpeed;
        }
        else
        {
            rigid.linearVelocity = Vector2.zero;
        }
    }

    private void LookAtMouse()
    {
        Vector2 mouseScreenPos = inputActions.Player.Look.ReadValue<Vector2>();
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        spriteRenderer.flipX = mousePos.x < transform.position.x;
    }
}