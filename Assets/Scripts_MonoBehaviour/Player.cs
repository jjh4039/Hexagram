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

    // ★ [1번, 3번 구현을 위해 새로 추가된 변수들]
    [Header("Hit Feedback (New)")]
    [SerializeField] private Material flashMaterial;      // 만들어두신 플래시 머티리얼 할당!
    [SerializeField] private float flashDuration = 0.1f;  // 번쩍! 하는 시간 (0.1초면 충분합니다)
    [SerializeField] private float hitShakeDuration = 0.1f;  // 피격 시 카메라 진동 시간
    [SerializeField] private float hitShakeMagnitude = 0.05f; // 피격 시 카메라 진동 강도
    [SerializeField] private float playerHitStopDuration = 0.12f;

    private Material originalMaterial; // 원래 머티리얼로 되돌리기 위한 저장소

    [Header("Contact Damage Settings")]
    [SerializeField] private float contactCheckRadius = 0.6f;
    [SerializeField] private LayerMask enemyLayer;
    private ContactFilter2D contactFilter;
    private readonly Collider2D[] contactResults = new Collider2D[8];

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
    [SerializeField] private AudioClip sfxHit;

    private Color paleRed = new Color(1f, 0.3f, 0.3f, 1f);

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        inputActions = new PlayerInput();
        stats = GetComponent<PlayerStats>();
        anim = GetComponentInChildren<Animator>();

        // ★ 시작할 때 플레이어의 원래 머티리얼(기본값)을 저장해 둡니다.
        if (spriteRenderer != null)
        {
            originalMaterial = spriteRenderer.material;
        }

        currentMoveSpeed = defaultMoveSpeed;
        rigid.gravityScale = 0;
        rigid.interpolation = RigidbodyInterpolation2D.Interpolate;
        rigid.sleepMode = RigidbodySleepMode2D.NeverSleep;

        contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(enemyLayer);
        contactFilter.useLayerMask = true;
        contactFilter.useTriggers = true;
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

        if (!isAttacking)
            Move();

        CheckContactDamage();
    }

    private void CheckContactDamage()
    {
        if (isInvincible) return;

        int hitCount = Physics2D.OverlapCircle(
            transform.position,
            contactCheckRadius,
            contactFilter,
            contactResults
        );

        if (hitCount > 0)
        {
            float finalDamage = bodyContactDamage; // 기본은 플레이어에 설정된 데미지

            // 첫 번째로 충돌한 적의 정보를 가져옴
            Collider2D hitCollider = contactResults[0];

            // 만약 충돌한 적이 '보스'라면?
            EnemyBoss boss = hitCollider.GetComponent<EnemyBoss>();
            if (boss != null)
            {
                // 1. 보스의 기본 데미지를 가져옴
                finalDamage = boss.BaseContactDamage;

                // 2. 보스가 돌진 중이면 데미지를 뻥튀기!
                if (boss.IsDashing)
                {
                    finalDamage *= boss.DashDamageMultiplier;
                    Debug.Log($"플레이어가 보스의 '돌진'에 맞았습니다! 데미지 증폭: {finalDamage}");
                }
                else
                {
                    Debug.Log($"플레이어가 보스와 부딪혔습니다. 기본 데미지: {finalDamage}");
                }
            }

            // 계산된 최종 데미지 입기
            OnDamage(finalDamage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, contactCheckRadius);
    }

    public void ApplyDiceBuff(DiceData data)
    {
        RemoveDiceBuff();
        if (weaponManager != null)
            weaponManager.UpdateWeaponVisuals(data.particleColor, data.muzzleFlashMaterial);

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
                if (stats != null)
                    stats.currentHealth = Mathf.Min(
                        stats.currentHealth + (int)data.effectValue,
                        stats.maxHealth
                    );
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

        if (weaponManager != null)
            weaponManager.UpdateWeaponVisuals(Color.white, null);
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
            GameObject dust = Instantiate(dashDustPrefab, transform.position, Quaternion.identity);
            var mainModule = dust.GetComponent<ParticleSystem>().main;
            mainModule.startColor = currentDiceColor;
            Destroy(dust, 1.0f);
        }

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
        isAttacking = false;
        isDashing = false;
        isCharging = false;

        isInvincible = true;

        rigid.linearVelocity = Vector2.zero;
        rigid.simulated = false;

        inputActions.Disable();

        spriteRenderer.color = Color.gray;
        if (anim != null) anim.enabled = false;

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

    public void SetChargingState(bool _isCharging)
    {
        isCharging = _isCharging;
        if (anim != null) anim.SetBool("IsCharging", isCharging);
    }

    public void PlayWakeUpAnimation()
    {
        if (anim != null) anim.SetBool("IsCharging", false);
    }

    public void SetDiceAnimation(int diceIndex)
    {
        if (anim != null) anim.SetFloat("DiceType", (float)diceIndex);
    }

    public void OnDamage(float damage)
    {
        if (isInvincible) return;
        if (stats != null) stats.TakeDamage((int)damage);

        // 카메라 셰이크 
        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.HitShake(hitShakeDuration, hitShakeMagnitude);
        }

        // ★ [새로 추가된 로직] 플레이어 피격 시 강렬한 히트 스탑 발생!
        if (GameManager.instance != null)
        {
            GameManager.instance.HitStop(playerHitStopDuration);
        }

        if (sfxHit != null && SoundManager.instance != null)
        {
            SoundManager.instance.PlaySFX(sfxHit, 0.9f);
        }

        StartCoroutine(Co_OnHit());
    }

    // ★ [수정됨] 피격 연출의 꽃 (화이트 플래시 -> 빨간 깜빡임)
    IEnumerator Co_OnHit()
    {
        isInvincible = true;
        float timer = 0f;

        // 1. 화이트 플래시 (머티리얼 교체)
        if (flashMaterial != null && spriteRenderer != null)
        {
            spriteRenderer.material = flashMaterial;
            spriteRenderer.color = Color.white; // 혹시나 붉은색이 남아있을까봐 하얗게 초기화

            yield return new WaitForSeconds(flashDuration);
            timer += flashDuration;

            // 플래시가 끝나면 원래 머티리얼로 복구!
            spriteRenderer.material = originalMaterial;
        }

        // 2. 이후 기존처럼 남은 시간 동안 붉은색 깜빡임 진행
        bool isRed = true; // 하얀색 플래시 직후니까 바로 빨간색부터 보여주면 아주 자연스럽습니다.

        while (timer < invincibleTime)
        {
            spriteRenderer.color = isRed ? paleRed : Color.white;
            isRed = !isRed;
            yield return new WaitForSeconds(blinkSpeed);
            timer += blinkSpeed;
        }

        // 연출 종료 후 완전 초기화
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            spriteRenderer.material = originalMaterial; // 안전장치
        }

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
