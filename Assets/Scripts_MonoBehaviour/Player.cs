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

    [Header("Input Control")]
    public bool canControl = true;
    public Vector2 mouseWorldPos { get; private set; }

    [Header("Movement")]
    private Vector2 _moveInput;

    [Header("State")]
    public bool isAttacking = false;
    public bool isKnockedBack = false;

    [Header("Hit & Invincibility")]
    [SerializeField] private bool isInvincible = false;
    [SerializeField] private float invincibleTime = 0.8f;
    [SerializeField] private float blinkSpeed = 0.2f;
    [SerializeField] private float bodyContactDamage = 5f;

    [Header("Hit Feedback")]
    [SerializeField] private Material flashMaterial;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private float hitShakeDuration = 0.1f;
    [SerializeField] private float hitShakeMagnitude = 0.05f;
    [SerializeField] private float playerHitStopDuration = 0.12f;

    private Material _originalMaterial;

    [Header("Contact Damage Settings")]
    [SerializeField] private float contactCheckRadius = 0.3f;
    [SerializeField] private LayerMask enemyLayer;
    private ContactFilter2D _contactFilter;
    private readonly Collider2D[] _contactResults = new Collider2D[8];

    [Header("Weapon Link")]
    [SerializeField] private WeaponManager weaponManager;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.1f;

    [Header("Dash Visuals")]
    [SerializeField] private float ghostInterval = 0.03f;
    [SerializeField] private float ghostFadeTime = 0.4f;
    [SerializeField] private Color ghostColor = new Color(0.6f, 0.6f, 1f, 0.4f);

    [Header("Effects")]
    [SerializeField] private GameObject dashDustPrefab;
    [SerializeField] private float shakeDuration = 0.05f;
    [SerializeField] private float shakeMagnitude = 0.02f;

    private bool _isDashing = false;
    private float _lastDashTime = -99f;
    private Color _currentDiceColor = Color.white;

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

        if (stats == null)
            stats = GetComponent<PlayerStats>();

        anim = GetComponentInChildren<Animator>();

        if (spriteRenderer != null)
            _originalMaterial = spriteRenderer.material;

        rigid.gravityScale = 0;
        rigid.interpolation = RigidbodyInterpolation2D.Interpolate;
        rigid.sleepMode = RigidbodySleepMode2D.NeverSleep;

        _contactFilter = new ContactFilter2D();
        _contactFilter.SetLayerMask(enemyLayer);
        _contactFilter.useLayerMask = true;
        _contactFilter.useTriggers = true;
    }

    private void OnEnable()
    {
        inputActions.Enable();
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

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (!canControl || _isDashing || isKnockedBack)
            return;

        if (weaponManager != null)
            weaponManager.OnAttackInput();
    }

    private void OnSwap(InputAction.CallbackContext context)
    {
        if (!canControl || _isDashing || isKnockedBack)
            return;

        if (weaponManager != null)
            weaponManager.OnSwapInput();
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        if (!canControl || isAttacking || _isDashing)
            return;

        if (stats.currentDashStacks < 1f)
            return;

        stats.currentDashStacks -= 1f;
        StartCoroutine(DashRoutine());
    }

    private void Update()
    {
        if (!canControl)
        {
            _moveInput = Vector2.zero;
            UpdateBuffTimers();
            return;
        }

        if (stats.currentDashStacks < stats.maxDashStacks)
        {
            stats.currentDashStacks += Time.deltaTime * stats.dashRechargeRate;
            stats.currentDashStacks = Mathf.Min(stats.currentDashStacks, stats.maxDashStacks);
        }

        _moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        LookAtMouse();
        UpdateBuffTimers();
    }

    private void LookAtMouse()
    {
        Vector2 mouseScreenPos = inputActions.Player.Look.ReadValue<Vector2>();
        mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        spriteRenderer.flipX = mouseWorldPos.x < transform.position.x;
    }

    private void FixedUpdate()
    {
        if (_isDashing)
            return;

        if (!isKnockedBack)
            Move();

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
            ActiveBuff newBuff = new ActiveBuff
            {
                buffData = data,
                remainingTime = data.duration,
                stackCount = 1
            };

            activeBuffs.Add(newBuff);
            Debug.Log($"[신규 버프] {data.diceName} 발동! 시간: {data.duration}초");
        }

        RecalculateStats();
        _currentDiceColor = data.particleColor;

        if (weaponManager != null)
            weaponManager.UpdateWeaponVisuals(data.particleColor, data.muzzleFlashMaterial);
    }

    private void RemoveBuff(ActiveBuff expiredBuff)
    {
        Debug.Log($"[버프 종료] {expiredBuff.buffData.diceName}");
        RecalculateStats();

        if (activeBuffs.Count == 0 && weaponManager != null)
            weaponManager.UpdateWeaponVisuals(Color.white, null);
    }

    private void RecalculateStats()
    {
        if (stats == null)
            return;

        stats.damageMultiplier = 1.0f;
        stats.moveSpeedMultiplier = 1.0f;
        stats.attackSpeedMultiplier = 1.0f;
        stats.chargeSpeedMultiplier = 1.0f;

        foreach (var buff in activeBuffs)
        {
            float finalEffectValue = buff.buffData.effectValue * buff.stackCount;

            switch (buff.buffData.effectType)
            {
                case DiceEffectType.AttackBuff:
                    stats.damageMultiplier += (finalEffectValue / 100f);
                    break;

                case DiceEffectType.SpeedBuff:
                    stats.moveSpeedMultiplier += (finalEffectValue / 100f);
                    stats.attackSpeedMultiplier += (finalEffectValue / 100f);
                    break;

                case DiceEffectType.GrowthBuff:
                    float growthFactor = 1.0f + (finalEffectValue / 100f);
                    stats.meleeAttackPower *= growthFactor;
                    stats.rangeAttackPower *= growthFactor;
                    break;

                case DiceEffectType.CriticalBuff:
                    stats.remainingStrongAttacks += (int)finalEffectValue;
                    break;

                case DiceEffectType.ChargingBuff:
                    stats.chargeSpeedMultiplier += finalEffectValue;
                    break;

                case DiceEffectType.Heal:
                    stats.currentHealth = Mathf.Min(stats.currentHealth + (int)finalEffectValue, stats.maxHealth);
                    buff.remainingTime = 0;
                    break;
            }
        }
    }

    private void Move()
    {
        if (!canControl)
            return;

        float finalSpeed = stats.moveSpeed * stats.moveSpeedMultiplier;

        if (isAttacking)
        {
            if (weaponManager)
                finalSpeed *= weaponManager.GetCurrentAttackMoveMultiplier();
            else
                finalSpeed *= 0f;
        }

        rigid.linearVelocity = _moveInput * finalSpeed;
    }

    private void CheckContactDamage()
    {
        if (isInvincible)
            return;

        int hitCount = Physics2D.OverlapCircle(transform.position, contactCheckRadius, _contactFilter, _contactResults);

        if (hitCount > 0)
        {
            float finalDamage = bodyContactDamage;
            Collider2D hitCollider = _contactResults[0];
            EnemyBoss boss = hitCollider.GetComponent<EnemyBoss>();

            if (boss != null)
            {
                finalDamage = boss.BaseContactDamage;

                if (boss.IsDashing)
                    finalDamage *= boss.DashDamageMultiplier;
            }

            OnDamage(finalDamage);
        }
    }

    public void OnDamage(float damage)
    {
        if (isInvincible)
            return;

        if (stats != null)
            stats.TakeDamage((int)damage);

        if (CameraFollow.instance != null)
            CameraFollow.instance.HitShake(hitShakeDuration, hitShakeMagnitude);

        if (GameManager.instance != null)
            GameManager.instance.HitStop(playerHitStopDuration);

        if (sfxHit != null && SoundManager.instance != null)
            SoundManager.instance.PlaySFX(sfxHit, 0.9f);

        StartCoroutine(Co_OnHit());
    }

    IEnumerator Co_OnHit()
    {
        isInvincible = true;
        float timer = 0f;

        if (flashMaterial != null && spriteRenderer != null)
        {
            spriteRenderer.material = flashMaterial;
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(flashDuration);
            timer += flashDuration;
            spriteRenderer.material = _originalMaterial;
        }

        bool isRed = true;

        while (timer < invincibleTime)
        {
            spriteRenderer.color = isRed ? paleRed : Color.white;
            isRed = !isRed;
            yield return new WaitForSeconds(blinkSpeed);
            timer += blinkSpeed;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            spriteRenderer.material = _originalMaterial;
        }

        isInvincible = false;
    }

    public void ApplyKnockback(Vector2 direction, float maxSpeed, float duration)
    {
        if (stats != null && stats.currentHealth <= 0)
            return;

        StartCoroutine(Co_KnockbackRoutine(direction, maxSpeed, duration));
    }

    private IEnumerator Co_KnockbackRoutine(Vector2 dir, float maxSpeed, float duration)
    {
        isKnockedBack = true;
        isAttacking = false;
        isInvincible = true;

        if (flashMaterial != null && spriteRenderer != null)
        {
            spriteRenderer.material = flashMaterial;
            Invoke(nameof(RestoreMaterial), 0.1f);
        }

        float timer = 0f;
        float ghostTimer = 0f;

        while (timer < duration)
        {
            yield return new WaitForFixedUpdate();

            timer += Time.fixedDeltaTime;
            float t = timer / duration;
            float speedDecay = Mathf.Pow(1 - t, 2);

            rigid.linearVelocity = dir * (maxSpeed * speedDecay);

            ghostTimer += Time.fixedDeltaTime;

            if (ghostTimer > ghostInterval)
            {
                CreateGhost();
                ghostTimer = 0f;
            }
        }

        rigid.linearVelocity = Vector2.zero;
        isKnockedBack = false;
        isInvincible = false;
    }

    private void RestoreMaterial()
    {
        if (spriteRenderer != null)
            spriteRenderer.material = _originalMaterial;
    }

    private IEnumerator DashRoutine()
    {
        _isDashing = true;
        isInvincible = true;
        _lastDashTime = Time.time;

        Vector2 dashDir;

        if (_moveInput.magnitude > 0)
            dashDir = _moveInput.normalized;
        else
            dashDir = (mouseWorldPos - (Vector2)transform.position).normalized;

        if (dashDustPrefab != null)
        {
            GameObject dust = Instantiate(dashDustPrefab, transform.position, Quaternion.identity);
            var mainModule = dust.GetComponent<ParticleSystem>().main;
            mainModule.startColor = _currentDiceColor;
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
        _isDashing = false;
        isInvincible = false;
    }

    private IEnumerator DashGhostRoutine()
    {
        while (_isDashing)
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

    public void OnDie()
    {
        isAttacking = false;
        _isDashing = false;
        isInvincible = true;
        rigid.linearVelocity = Vector2.zero;
        rigid.simulated = false;
        canControl = false;
        spriteRenderer.color = Color.gray;

        if (anim != null)
            anim.enabled = false;

        Debug.Log("Player: 으악 죽었다...");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, contactCheckRadius);
    }
}