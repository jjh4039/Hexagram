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
    public float damageMultiplier = 1.0f;
    public float moveSpeedMultiplier = 1.0f;
    public float attackSpeedMultiplier = 1.0f;
    public float chargeSpeedMultiplier = 1.0f;

    [Header("Weapon Link")]
    [SerializeField] private WeaponManager weaponManager;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.8f;

    [Header("Dash Visuals")]
    [SerializeField] private float ghostInterval = 0.01f;
    [SerializeField] private float ghostFadeTime = 0.4f;
    [SerializeField] private Color ghostColor = new Color(0.6f, 0.6f, 1f, 0.4f);

    [Header("Effects")]
    [SerializeField] private GameObject dashDustPrefab;
    [SerializeField] private float shakeDuration = 0.1f;
    [SerializeField] private float shakeMagnitude = 0.1f;

    private bool isDashing = false;
    private float lastDashTime = -99f;
    private Color currentDiceColor = Color.white;

    [Header("Strong")]
    public int remainingStrongAttacks = 0;

    [Header("Sound")]
    [SerializeField] private AudioClip sfxDash;

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

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Dash.performed += OnDash;
    }

    private void OnDisable()
    {
        inputActions.Disable();
        inputActions.Player.Dash.performed -= OnDash;
    }

    private void Update()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        LookAtMouse();
        HandleSpeedInterpolation();
    }

    private void FixedUpdate()
    {
        if (isDashing) return;
        if (!isAttacking) Move();
    }

    public void ApplyDiceBuff(DiceData data)
    {
        RemoveDiceBuff();
        if (weaponManager != null) weaponManager.UpdateWeaponVisuals(data.particleColor, data.muzzleFlashMaterial);
        currentDiceColor = data.particleColor;

        switch (data.effectType)
        {
            case DiceEffectType.AttackBuff:
                if (stats != null) stats.TakeDamage(1);
                damageMultiplier = 1.0f + (data.effectValue / 100f);
                break;
            case DiceEffectType.CriticalBuff:
                remainingStrongAttacks = (int)data.effectValue;
                break;
            case DiceEffectType.GrowthBuff:
                if (stats != null)
                {
                    float growthFactor = 1.0f + (data.effectValue / 100f);
                    stats.meleeAttackPower *= growthFactor;
                    stats.rangeAttackPower *= growthFactor;
                }
                break;
            case DiceEffectType.Heal:
                if (stats != null) stats.currentHealth = Mathf.Min(stats.currentHealth + (int)data.effectValue, stats.maxHealth);
                break;
            case DiceEffectType.SpeedBuff:
                moveSpeedMultiplier = 1.0f + (data.effectValue / 100f);
                attackSpeedMultiplier = 1.0f + (data.effectValue / 100f);
                break;
            case DiceEffectType.ChargingBuff:
                chargeSpeedMultiplier = data.effectValue;
                break;
        }
    }

    public void RemoveDiceBuff()
    {
        damageMultiplier = 1.0f;
        moveSpeedMultiplier = 1.0f;
        attackSpeedMultiplier = 1.0f;
        chargeSpeedMultiplier = 1.0f;
        if (weaponManager != null) weaponManager.UpdateWeaponVisuals(Color.white, null);
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        if (isAttacking || isCharging || isDashing) return;
        if (Time.time < lastDashTime + dashCooldown) return;

        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        isInvincible = true;
        lastDashTime = Time.time;

        Vector2 dashDir;
        if (moveInput.magnitude > 0)
            dashDir = moveInput.normalized;
        else
        {
            Vector2 mouseScreenPos = inputActions.Player.Look.ReadValue<Vector2>();
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            dashDir = (mouseWorldPos - (Vector2)transform.position).normalized;
        }

        if (dashDustPrefab != null)
        {
            // 1. 파티클 생성
            GameObject dust = Instantiate(dashDustPrefab, transform.position, Quaternion.identity);

            // 2. 파티클 시스템의 Main 모듈 가져오기
            var mainModule = dust.GetComponent<ParticleSystem>().main;

            // 3. 저장해둔 주사위 색상 적용!
            mainModule.startColor = currentDiceColor;

            // 4. (옵션) 1초 뒤 자동 삭제 스크립트가 없다면 여기서 처리
            Destroy(dust, 1.0f);
        }

        // 사운드 재생   
        if (sfxDash != null)
            SoundManager.instance.PlaySFX(sfxDash, 0.4f);

        if (CameraFollow.instance != null)
            CameraFollow.instance.HitShake(shakeDuration, shakeMagnitude);

        rigid.linearVelocity = dashDir * dashSpeed;

        StartCoroutine(DashGhostRoutine());

        yield return new WaitForSeconds(dashDuration);

        rigid.linearVelocity = Vector2.zero;
        isDashing = false;
        isInvincible = false;
    }

    public void OnDie()
    {
        // 1. 모든 상태 초기화
        isAttacking = false;
        isDashing = false;
        isCharging = false;

        // 2. 더 이상 데미지 안 입게 무적 처리 (선택 사항)
        isInvincible = true;

        // 3. 물리 엔진 정지 (미끄러짐 방지)
        rigid.linearVelocity = Vector2.zero;
        rigid.simulated = false; // 다른 애들이랑 충돌 안 하게 (시체 위로 지나가게)

        // 4. 입력 시스템 끄기 (더 이상 조작 불가!)
        inputActions.Disable();

        // 5. 비주얼 변경 (회색으로) & 애니메이션 멈춤
        spriteRenderer.color = Color.gray;
        if (anim != null) anim.enabled = false; // 애니메이션 멈춰서 죽은 척

        Debug.Log("Player: 으악 죽었다... (조작 불능 상태)");
    }

    private IEnumerator DashGhostRoutine()
    {
        while (isDashing)
        {
            CreateGhost();
            yield return new WaitForSeconds(ghostInterval);
        }
    }

    private void CreateGhost()
    {
        GameObject ghostObj = new GameObject("DashGhost");
        ghostObj.transform.position = transform.position;
        ghostObj.transform.localScale = transform.localScale;

        SpriteRenderer sr = ghostObj.AddComponent<SpriteRenderer>();
        sr.sprite = spriteRenderer.sprite;
        sr.color = ghostColor;
        sr.flipX = spriteRenderer.flipX;
        sr.sortingLayerID = spriteRenderer.sortingLayerID;
        sr.sortingOrder = spriteRenderer.sortingOrder - 1;

        StartCoroutine(FadeOutAndDestroy(ghostObj, sr));
    }

    private IEnumerator FadeOutAndDestroy(GameObject obj, SpriteRenderer sr)
    {
        float timer = 0f;
        Color startColor = sr.color;

        while (timer < ghostFadeTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, timer / ghostFadeTime);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        Destroy(obj);
    }

    private void HandleSpeedInterpolation()
    {
        float targetSpeed = isCharging ? minMoveSpeed : defaultMoveSpeed;
        currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, targetSpeed, Time.deltaTime * speedChangeRate);
    }

    // ★ [기존] 상태와 애니메이션 동시 변경
    public void SetChargingState(bool _isCharging)
    {
        isCharging = _isCharging; // 실제 논리적 상태 (공격 불가 여부)
        if (anim != null) anim.SetBool("IsCharging", isCharging); // 애니메이션
    }

    // ★ [신규] 애니메이션만 먼저 깨우는 함수 (공격은 여전히 불가)
    public void PlayWakeUpAnimation()
    {
        if (anim != null) anim.SetBool("IsCharging", false);
        // 주의: isCharging 변수는 건드리지 않음!
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