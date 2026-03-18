using System.Collections;
using System.Collections.Generic;
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

    [Header("Input Control (New)")]
    public bool canControl = true; // ★ 컷신에서 이 값만 false로 하면 모든 조작(이동, 공격)이 멈춥니다!
    public Vector2 mouseWorldPos { get; private set; } // 무기들이 이 좌표를 가져다 씁니다.

    [Header("Movement")]
    [SerializeField] private float defaultMoveSpeed = 5f;
    [SerializeField] private float minMoveSpeed = 1f;
    [SerializeField] private float speedChangeRate = 5f;

    private float _currentMoveSpeed; 
    private Vector2 moveInput;

    [Header("State")]
    public bool isAttacking = false;
    public bool isCharging = false;
    public bool isKnockedBack = false;

    [Header("Hit & Invincibility")]
    [SerializeField] private bool isInvincible = false;
    [SerializeField] private float invincibleTime = 1.0f;
    [SerializeField] private float blinkSpeed = 0.2f;
    [SerializeField] private float bodyContactDamage = 5f;

    [Header("Hit Feedback")]
    [SerializeField] private Material flashMaterial;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private float hitShakeDuration = 0.1f;
    [SerializeField] private float hitShakeMagnitude = 0.05f;
    [SerializeField] private float playerHitStopDuration = 0.12f;

    private Material originalMaterial;

    [Header("Contact Damage Settings")]
    [SerializeField] private float contactCheckRadius = 0.6f;
    [SerializeField] private LayerMask enemyLayer;
    private ContactFilter2D contactFilter;
    private readonly Collider2D[] contactResults = new Collider2D[8];

    [Header("Weapon Link")]
    [SerializeField] private WeaponManager weaponManager;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float dashDuration = 0.2f;

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

    [Header("Sound")]
    [SerializeField] private AudioClip sfxDash;
    [SerializeField] private AudioClip sfxHit;

    private Color paleRed = new Color(1f, 0.3f, 0.3f, 1f);
    public PlayerInput Input => inputActions;

    [System.Serializable]
    public class ActiveBuff
    {
        public DiceData buffData;
        public float remainingTime;
        public int stackCount;
    }

    [Header("--- Buff Manager ---")]
    public List<ActiveBuff> activeBuffs = new List<ActiveBuff>();

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        inputActions = new PlayerInput();
        if (stats == null) stats = GetComponent<PlayerStats>();
        anim = GetComponentInChildren<Animator>();

        if (spriteRenderer != null) originalMaterial = spriteRenderer.material;

        _currentMoveSpeed = defaultMoveSpeed;
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
        // ★ 모든 입력을 Player에서 중앙 통제합니다.
        inputActions.Player.Dash.performed += OnDash;
        inputActions.Player.Attack.performed += OnAttack;
        inputActions.Player.Swap.performed += OnSwap;
    }

    private void OnDisable()
    {
        inputActions.Disable();
        inputActions.Player.Dash.performed -= OnDash;
        inputActions.Player.Attack.performed -= OnAttack;
        inputActions.Player.Swap.performed -= OnSwap;
    }

    // ==========================================
    // ★ 입력 통제 센터 (Input Control)
    // ==========================================
    private void OnAttack(InputAction.CallbackContext context)
    {
        if (!canControl || isDashing || isKnockedBack) return;

        // ★ SendMessage 삭제! 다이렉트로 빠르게 호출합니다.
        if (weaponManager != null) weaponManager.OnAttackInput();
    }

    private void OnSwap(InputAction.CallbackContext context)
    {
        if (!canControl || isDashing || isKnockedBack) return;

        // ★ SendMessage 삭제!
        if (weaponManager != null) weaponManager.OnSwapInput();
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        // ★ [변경] 쿨타임 체크 대신 스택이 1 이상인지 확인
        if (!canControl || isAttacking || isCharging || isDashing) return;
        if (stats.currentDashStacks < 1f) return;

        // 스택 1개 소모하고 대시 시작
        stats.currentDashStacks -= 1f;
        StartCoroutine(DashRoutine());
    }

    private void Update()
    {
        if (!canControl)
        {
            moveInput = Vector2.zero;
            UpdateBuffTimers();
            return; // 컷신 중이면 이동 입력 무시
        }

        if (stats.currentDashStacks < stats.maxDashStacks)
        {
            stats.currentDashStacks += Time.deltaTime * stats.dashRechargeRate;
            stats.currentDashStacks = Mathf.Min(stats.currentDashStacks, stats.maxDashStacks);
        }

        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        LookAtMouse();
        HandleSpeedInterpolation();
        UpdateBuffTimers();
    }

    private void LookAtMouse()
    {
        Vector2 mouseScreenPos = inputActions.Player.Look.ReadValue<Vector2>();
        mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        spriteRenderer.flipX = mouseWorldPos.x < transform.position.x;
    }

    // --- 이하 로직 (Move, DashRoutine, BuffManager 등)은 아까 드린 코드와 100% 동일하므로 생략하지 않고 모두 포함 ---
    private void FixedUpdate()
    {
        if (isDashing) return;

        if (!isKnockedBack) Move();

        CheckContactDamage();
    }

    private void UpdateBuffTimers()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            activeBuffs[i].remainingTime -= Time.deltaTime;
            if (activeBuffs[i].remainingTime <= 0)
            {
                RemoveBuff(activeBuffs[i]);
                activeBuffs.RemoveAt(i);
            }
        }
    }

    public void ApplyDiceBuff(DiceData data)
    {
        ActiveBuff existingBuff = activeBuffs.Find(b => b.buffData.effectType == data.effectType);
        if (existingBuff != null)
        {
            existingBuff.remainingTime = (existingBuff.remainingTime + data.duration) / 2f;
            existingBuff.stackCount = 2;
            Debug.Log($"[버프 중첩!] {data.diceName} 연장. 남은시간: {existingBuff.remainingTime:F1}초, 효과 2배!");
        }
        else
        {
            ActiveBuff newBuff = new ActiveBuff { buffData = data, remainingTime = data.duration, stackCount = 1 };
            activeBuffs.Add(newBuff);
            Debug.Log($"[신규 버프] {data.diceName} 발동! 시간: {data.duration}초");
        }
        RecalculateStats();
        currentDiceColor = data.particleColor;
        if (weaponManager != null) weaponManager.UpdateWeaponVisuals(data.particleColor, data.muzzleFlashMaterial);
    }

    private void RemoveBuff(ActiveBuff expiredBuff)
    {
        Debug.Log($"[버프 종료] {expiredBuff.buffData.diceName}");
        RecalculateStats();
        if (activeBuffs.Count == 0 && weaponManager != null) weaponManager.UpdateWeaponVisuals(Color.white, null);
    }

    private void RecalculateStats()
    {
        if (stats == null) return;
        stats.damageMultiplier = 1.0f;
        stats.moveSpeedMultiplier = 1.0f;
        stats.attackSpeedMultiplier = 1.0f;
        stats.chargeSpeedMultiplier = 1.0f;

        foreach (var buff in activeBuffs)
        {
            float finalEffectValue = buff.buffData.effectValue * buff.stackCount;
            switch (buff.buffData.effectType)
            {
                case DiceEffectType.AttackBuff: stats.damageMultiplier += (finalEffectValue / 100f); break;
                case DiceEffectType.SpeedBuff: stats.moveSpeedMultiplier += (finalEffectValue / 100f); stats.attackSpeedMultiplier += (finalEffectValue / 100f); break;
                case DiceEffectType.GrowthBuff:
                    float growthFactor = 1.0f + (finalEffectValue / 100f);
                    stats.meleeAttackPower *= growthFactor; stats.rangeAttackPower *= growthFactor; break;
                case DiceEffectType.CriticalBuff: stats.remainingStrongAttacks += (int)finalEffectValue; break;
                case DiceEffectType.ChargingBuff: stats.chargeSpeedMultiplier += finalEffectValue; break;
                case DiceEffectType.Heal: stats.currentHealth = Mathf.Min(stats.currentHealth + (int)finalEffectValue, stats.maxHealth); buff.remainingTime = 0; break;
            }
        }
    }

    private void Move()
    {
        if (!canControl) return;

        float finalSpeed = defaultMoveSpeed * stats.moveSpeedMultiplier;

        if (isAttacking)
        {
            if (weaponManager != null)
            {
                finalSpeed *= weaponManager.GetCurrentAttackMoveMultiplier();
            }
            else
            {
                finalSpeed *= 0f; // 매니저 없으면 안전하게 멈춤
            }
        }

        // 이제 finalSpeed가 0보다 크면 공격 중에도 미세하게 움직입니다!
        rigid.linearVelocity = moveInput * finalSpeed;
    }

    private void HandleSpeedInterpolation()
    {
        float targetSpeed = isCharging ? minMoveSpeed : defaultMoveSpeed;
        _currentMoveSpeed = Mathf.Lerp(_currentMoveSpeed, targetSpeed, Time.deltaTime * speedChangeRate);
    }

    private void CheckContactDamage()
    {
        if (isInvincible) return;
        int hitCount = Physics2D.OverlapCircle(transform.position, contactCheckRadius, contactFilter, contactResults);
        if (hitCount > 0)
        {
            float finalDamage = bodyContactDamage;
            Collider2D hitCollider = contactResults[0];
            EnemyBoss boss = hitCollider.GetComponent<EnemyBoss>();
            if (boss != null) { finalDamage = boss.BaseContactDamage; if (boss.IsDashing) finalDamage *= boss.DashDamageMultiplier; }
            OnDamage(finalDamage);
        }
    }

    public void OnDamage(float damage)
    {
        if (isInvincible) return;
        if (stats != null) stats.TakeDamage((int)damage);
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(hitShakeDuration, hitShakeMagnitude);
        if (GameManager.instance != null) GameManager.instance.HitStop(playerHitStopDuration);
        if (sfxHit != null && SoundManager.instance != null) SoundManager.instance.PlaySFX(sfxHit, 0.9f);
        StartCoroutine(Co_OnHit());
    }

    IEnumerator Co_OnHit()
    {
        isInvincible = true;
        float timer = 0f;
        if (flashMaterial != null && spriteRenderer != null)
        {
            spriteRenderer.material = flashMaterial; spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(flashDuration);
            timer += flashDuration; spriteRenderer.material = originalMaterial;
        }
        bool isRed = true;
        while (timer < invincibleTime)
        {
            spriteRenderer.color = isRed ? paleRed : Color.white;
            isRed = !isRed; yield return new WaitForSeconds(blinkSpeed); timer += blinkSpeed;
        }
        if (spriteRenderer != null) { spriteRenderer.color = Color.white; spriteRenderer.material = originalMaterial; }
        isInvincible = false;
    }

    public void ApplyKnockback(Vector2 direction, float maxSpeed, float duration)
    {
        if (stats != null && stats.currentHealth <= 0) return;
        StartCoroutine(Co_KnockbackRoutine(direction, maxSpeed, duration));
    }

    private IEnumerator Co_KnockbackRoutine(Vector2 dir, float maxSpeed, float duration)
    {
        isKnockedBack = true; isAttacking = false; isCharging = false; isInvincible = true;
        if (flashMaterial != null && spriteRenderer != null) { spriteRenderer.material = flashMaterial; Invoke("RestoreMaterial", 0.1f); }
        float timer = 0f; float ghostTimer = 0f;
        while (timer < duration)
        {
            yield return new WaitForFixedUpdate();
            timer += Time.fixedDeltaTime; float t = timer / duration;
            float speedDecay = Mathf.Pow(1 - t, 2);
            rigid.linearVelocity = dir * (maxSpeed * speedDecay);
            ghostTimer += Time.fixedDeltaTime;
            if (ghostTimer > ghostInterval) { CreateGhost(); ghostTimer = 0f; }
        }
        rigid.linearVelocity = Vector2.zero; isKnockedBack = false; isInvincible = false;
    }

    private void RestoreMaterial() { if (spriteRenderer != null) spriteRenderer.material = originalMaterial; }

    private IEnumerator DashRoutine()
    {
        isDashing = true; isInvincible = true; lastDashTime = Time.time;
        Vector2 dashDir;
        if (moveInput.magnitude > 0) dashDir = moveInput.normalized;
        else dashDir = (mouseWorldPos - (Vector2)transform.position).normalized;

        if (dashDustPrefab != null) { GameObject dust = Instantiate(dashDustPrefab, transform.position, Quaternion.identity); var mainModule = dust.GetComponent<ParticleSystem>().main; mainModule.startColor = currentDiceColor; Destroy(dust, 1.0f); }
        if (sfxDash != null) SoundManager.instance.PlaySFX(sfxDash, 0.4f);
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(shakeDuration, shakeMagnitude);

        rigid.linearVelocity = dashDir * dashSpeed;
        StartCoroutine(DashGhostRoutine());
        yield return new WaitForSeconds(dashDuration);
        rigid.linearVelocity = Vector2.zero; isDashing = false; isInvincible = false;
    }

    private IEnumerator DashGhostRoutine() { while (isDashing) { CreateGhost(); yield return new WaitForSeconds(ghostInterval); } }
    private void CreateGhost()
    {
        GameObject ghostObj = new GameObject("DashGhost"); ghostObj.transform.position = transform.position; ghostObj.transform.localScale = transform.localScale;
        SpriteRenderer sr = ghostObj.AddComponent<SpriteRenderer>(); sr.sprite = spriteRenderer.sprite; sr.color = ghostColor; sr.flipX = spriteRenderer.flipX; sr.sortingLayerID = spriteRenderer.sortingLayerID; sr.sortingOrder = spriteRenderer.sortingOrder - 1;
        StartCoroutine(FadeOutAndDestroy(ghostObj, sr));
    }
    private IEnumerator FadeOutAndDestroy(GameObject obj, SpriteRenderer sr)
    {
        float timer = 0f; Color startColor = sr.color;
        while (timer < ghostFadeTime) { timer += Time.deltaTime; float alpha = Mathf.Lerp(startColor.a, 0f, timer / ghostFadeTime); sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha); yield return null; }
        Destroy(obj);
    }

    public void OnDie()
    {
        isAttacking = false; isDashing = false; isCharging = false; isInvincible = true;
        rigid.linearVelocity = Vector2.zero; rigid.simulated = false;
        canControl = false; // ★ 죽었을 때도 조작 통제
        spriteRenderer.color = Color.gray;
        if (anim != null) anim.enabled = false;
        Debug.Log("Player: 으악 죽었다...");
    }

    public void SetChargingState(bool _isCharging) { isCharging = _isCharging; if (anim != null) anim.SetBool("IsCharging", isCharging); }
    private void OnDrawGizmosSelected() { Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, contactCheckRadius); }
}