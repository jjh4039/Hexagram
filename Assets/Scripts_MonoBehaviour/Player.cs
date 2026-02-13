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

    // 새 충돌 처리
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
            OnDamage(bodyContactDamage);
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
        StartCoroutine(Co_OnHit());
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
