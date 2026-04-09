using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] public Rigidbody2D rigid;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator anim;
    [SerializeField] private PlayerStats stats;

    [Header("Manager Link")]
    [SerializeField] public BuffManager buffManager; // 추가됨
    [SerializeField] private WeaponManager weaponManager;

    [Header("Input Control")]
    public bool canControl = true;
    private Vector2 mouseWorldPos { get; set; }
    private Vector2 _moveInput;
    private PlayerInput _inputActions;

    [Header("State")]
    public bool isAttacking = false;
    public bool isKnockedBack = false;

    [Header("Hit And Invincibility")]
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

    [Header("Strong Attack")]
    [SerializeField] public float defaultStrongAttackMultiplier = 2f;

    private bool _isDashing = false;
    private float _lastDashTime = -99f;

    [Header("Sound")]
    [SerializeField] private AudioClip sfxDash;
    [SerializeField] private AudioClip sfxHit;

    private Color paleRed = new Color(1f, 0.3f, 0.3f, 1f);
    public PlayerInput Input => _inputActions;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _inputActions = new PlayerInput();

        if (stats == null) stats = GetComponent<PlayerStats>();
        if (buffManager == null) buffManager = GetComponent<BuffManager>();

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

        if (stats != null)
        {
            stats.ResetDiceRuntimeStats();
        }
    }

    private void OnEnable()
    {
        _inputActions.Enable();
        _inputActions.Player.Dash.performed += OnDash;
        _inputActions.Player.Attack.performed += OnAttack;
        _inputActions.Player.Swap.performed += OnSwap;
    }

    private void OnDisable()
    {
        _inputActions.Disable();
        _inputActions.Player.Dash.performed -= OnDash;
        _inputActions.Player.Attack.performed -= OnAttack;
        _inputActions.Player.Swap.performed -= OnSwap;
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (!canControl || _isDashing || isKnockedBack) return;
        if (weaponManager != null) weaponManager.OnAttackInput();
    }

    private void OnSwap(InputAction.CallbackContext context)
    {
        if (!canControl || _isDashing || isKnockedBack) return;
        if (weaponManager != null) weaponManager.OnSwapInput();
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        if (!canControl || isAttacking || _isDashing) return;
        if (stats.currentDashStacks < 1f) return;

        stats.currentDashStacks -= 1f;
        StartCoroutine(DashRoutine());
    }

    private void Update()
    {
        if (!canControl)
        {
            _moveInput = Vector2.zero;
            return;
        }

        if (stats.currentDashStacks < stats.maxDashStacks)
        {
            stats.currentDashStacks += Time.deltaTime * stats.dashRechargeRate;
            stats.currentDashStacks = Mathf.Min(stats.currentDashStacks, stats.maxDashStacks);
        }

        _moveInput = _inputActions.Player.Move.ReadValue<Vector2>();
        LookAtMouse();
    }

    private void LookAtMouse()
    {
        Vector2 mouseScreenPos = _inputActions.Player.Look.ReadValue<Vector2>();
        mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        spriteRenderer.flipX = mouseWorldPos.x < transform.position.x;
    }

    private void FixedUpdate()
    {
        if (_isDashing) return;

        if (!isKnockedBack) Move();
        CheckContactDamage();
    }

    // 무기 등에서 Ranged Multiplier를 가져갈 때 사용
    public float GetDiceRangedDamageMultiplier()
    {
        return stats != null ? stats.diceRangedDamageMultiplier : 1f;
    }

    // 무기 등에서 Strong Attack 스택을 소모할 때 사용 (BuffManager로 연결)
    public bool TryConsumeStrongAttack(out float strongAttackMultiplier)
    {
        if (buffManager != null)
        {
            return buffManager.TryConsumeStrongAttack(out strongAttackMultiplier);
        }
        strongAttackMultiplier = 1f;
        return false;
    }

    private void Move()
    {
        if (!canControl) return;

        float finalSpeed = stats.moveSpeed * stats.diceMoveSpeedMultiplier;

        if (isAttacking)
        {
            if (weaponManager) finalSpeed *= weaponManager.GetCurrentAttackMoveMultiplier();
            else finalSpeed *= 0f;
        }

        rigid.linearVelocity = _moveInput * finalSpeed;
    }

    private void CheckContactDamage()
    {
        if (isInvincible) return;

        int hitCount = Physics2D.OverlapCircle(transform.position, contactCheckRadius, _contactFilter, _contactResults);

        if (hitCount > 0)
        {
            float finalDamage = bodyContactDamage;
            Collider2D hitCollider = _contactResults[0];
            EnemyBoss boss = hitCollider.GetComponent<EnemyBoss>();

            if (boss != null)
            {
                finalDamage = boss.BaseContactDamage;
                if (boss.IsDashing) finalDamage *= boss.DashDamageMultiplier;
            }

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
        if (stats != null && stats.currentHealth <= 0) return;
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
            float speedDecay = Mathf.Pow(1 - t, 2f);

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
        if (spriteRenderer != null) spriteRenderer.material = _originalMaterial;
    }

    private IEnumerator DashRoutine()
    {
        _isDashing = true;
        isInvincible = true;
        _lastDashTime = Time.time;

        Vector2 dashDir;

        if (_moveInput.magnitude > 0) dashDir = _moveInput.normalized;
        else dashDir = (mouseWorldPos - (Vector2)transform.position).normalized;

        // BuffManager에서 색상을 가져옵니다
        Color dashColor = buffManager != null ? buffManager.GetCurrentDiceColor() : Color.white;

        if (dashDustPrefab != null)
        {
            GameObject dust = Instantiate(dashDustPrefab, transform.position, Quaternion.identity);
            ParticleSystem ps = dust.GetComponent<ParticleSystem>();

            if (ps != null)
            {
                var main = ps.main;
                main.startColor = dashColor;
            }

            Destroy(dust, 1.0f);
        }

        if (sfxDash != null && SoundManager.instance != null) SoundManager.instance.PlaySFX(sfxDash, 0.4f);
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(shakeDuration, shakeMagnitude);

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

        if (anim != null) anim.enabled = false;

        Debug.Log("Player: Dead");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, contactCheckRadius);
    }
}