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
    [SerializeField] public BuffManager buffManager;
    [SerializeField] private WeaponManager weaponManager;

    [Header("Input Control")]
    public bool canControl = true;

    public Vector2 mouseWorldPos { get; set; }
    private Vector2 _moveInput;

    public PlayerInput Input => InputStateManager.Instance.Actions;

    [Header("State")]
    public bool isAttacking = false;
    public bool isKnockedBack = false;
    public bool isRecoiling = false;
    public bool isTutorial = false;
    public bool IsDashing => _isDashing;

    [Header("Hit And Invincibility")]
    [SerializeField] public bool isInvincible = false;
    [SerializeField] private float invincibleTime = 0.8f;
    [SerializeField] private float blinkSpeed = 0.2f;
    [SerializeField] private float bodyContactDamage = 5f;

    [Header("Hit Feedback")]
    [SerializeField] private Material flashMaterial;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private float hitShakeDuration = 0.2f;
    [SerializeField] private float hitShakeMagnitude = 0.15f;
    [SerializeField] private float playerHitStopDuration = 0.16f;

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

    [Header("Object Pool Settings")]
    [SerializeField] private Transform poolParent;
    [SerializeField] private int ghostPoolSize = 20;
    [SerializeField] private int dustPoolSize = 5;

    private Queue<GameObject> _ghostPool = new Queue<GameObject>();
    private Transform _ghostPoolContainer;
    private Queue<GameObject> _dustPool = new Queue<GameObject>();
    private Transform _dustPoolContainer;

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

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

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

        InitPools();
    }

    private void Start()
    {
        if (InputStateManager.Instance == null) return;

        var actions = InputStateManager.Instance.Actions;

        actions.Normal.Dash.performed += OnDash;
        actions.Normal.Attack.performed += OnAttack;
        actions.Normal.Swap.performed += OnSwap;

        actions.Combat.Dash.performed += OnDash;
        actions.Combat.Attack.performed += OnAttack;
        actions.Combat.Swap.performed += OnSwap;

        InputStateManager.Instance.OnInputStateChanged += HandleInputStateChanged;
    }

    private void OnDestroy()
    {
        if (InputStateManager.Instance == null) return;

        var actions = InputStateManager.Instance.Actions;

        actions.Normal.Dash.performed -= OnDash;
        actions.Normal.Attack.performed -= OnAttack;
        actions.Normal.Swap.performed -= OnSwap;

        actions.Combat.Dash.performed -= OnDash;
        actions.Combat.Attack.performed -= OnAttack;
        actions.Combat.Swap.performed -= OnSwap;

        InputStateManager.Instance.OnInputStateChanged -= HandleInputStateChanged;
    }

    private void HandleInputStateChanged(InputState newState)
    {
        if (newState == InputState.UI)
        {
            _moveInput = Vector2.zero;
            // ★ 수정: 대시나 넉백 중일 때는 코루틴이 물리력을 관리하게 두고 여기서 건드리지 않습니다.
            if (!_isDashing && !isKnockedBack) 
            {
                rigid.linearVelocity = Vector2.zero;
            }
        }
        // ★ 수정: UI를 껐다고 해서 강제로 isInvincible = false로 만들면 대시 무적이 풀리는 치명적 버그가 발생하므로 삭제!
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (InputStateManager.Instance.CurrentInputState == InputState.UI) return; 
        
        bool isNormalMap = context.action.actionMap.name == "Normal";
        bool isCombatMap = context.action.actionMap.name == "Combat";
        
        if (isNormalMap && InputStateManager.Instance.CurrentInputState != InputState.Normal) return;
        if (isCombatMap && InputStateManager.Instance.CurrentInputState != InputState.Combat) return;

        if (!canControl || _isDashing || isKnockedBack) return;
        if (weaponManager != null) weaponManager.OnAttackInput();
    }

    private void OnSwap(InputAction.CallbackContext context)
    {
        if (InputStateManager.Instance.CurrentInputState == InputState.UI) return;

        bool isNormalMap = context.action.actionMap.name == "Normal";
        bool isCombatMap = context.action.actionMap.name == "Combat";
        
        if (isNormalMap && InputStateManager.Instance.CurrentInputState != InputState.Normal) return;
        if (isCombatMap && InputStateManager.Instance.CurrentInputState != InputState.Combat) return;

        if (!canControl || _isDashing || isKnockedBack) return;
        if (weaponManager != null) weaponManager.OnSwapInput();
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        if (InputStateManager.Instance.CurrentInputState == InputState.UI) return;
        if (!canControl || _isDashing) return;

        bool isSafeZone = (InputStateManager.Instance.CurrentPhase == GamePhase.SafeZone) && !isTutorial;

        if (!isSafeZone && stats.currentDashStacks < 1f) return;

        if (!isSafeZone)
        {
            stats.currentDashStacks -= 1f;
        }

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

        if (InputStateManager.Instance.CurrentInputState == InputState.Normal)
            _moveInput = Input.Normal.Move.ReadValue<Vector2>();
        else if (InputStateManager.Instance.CurrentInputState == InputState.Combat)
            _moveInput = Input.Combat.Move.ReadValue<Vector2>();
        else
            _moveInput = Vector2.zero;

        LookAtMouse();
    }

    private void LookAtMouse()
    {
        if (InputStateManager.Instance.CurrentInputState == InputState.UI) return;

        Vector2 mouseScreenPos = Vector2.zero;

        if (InputStateManager.Instance.CurrentInputState == InputState.Normal)
            mouseScreenPos = Input.Normal.Look.ReadValue<Vector2>();
        else if (InputStateManager.Instance.CurrentInputState == InputState.Combat)
            mouseScreenPos = Input.Combat.Look.ReadValue<Vector2>();

        mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        spriteRenderer.flipX = mouseWorldPos.x < transform.position.x;
    }

    private void FixedUpdate()
    {
        if (!canControl)
        {
            if (_isDashing)
            {
                _isDashing = false;
                isInvincible = false;
            }
            isKnockedBack = false;
            rigid.linearVelocity = Vector2.zero;
            return;
        }

        if (_isDashing) return;

        if (!isKnockedBack && !isRecoiling) Move();
        CheckContactDamage();
    }

    public float GetDiceRangedDamageMultiplier()
    {
        return stats != null ? stats.diceRangedDamageMultiplier : 1f;
    }

    public bool TryConsumeStrongAttack(out float strongAttackMultiplier)
    {
        if (buffManager)
        {
            return buffManager.TryConsumeStrongAttack(out strongAttackMultiplier);
        }
        strongAttackMultiplier = 1f;
        return false;
    }

    private void Move()
    {
        if (!canControl)
        {
            rigid.linearVelocity = Vector2.zero;
            return;
        }

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
            Enemy enemy = hitCollider.GetComponent<Enemy>();

            if (boss)
            {
                finalDamage = boss.BaseContactDamage;
                if (boss.IsDashing) finalDamage *= boss.DashDamageMultiplier;
            }
            else if (enemy)
            {
                finalDamage = enemy.ContactDamage;
            }

            OnDamage(finalDamage);
        }
    }

    public void OnDamage(float damage)
    {
        if (isInvincible) return;

        if (stats) stats.TakeDamage((int)damage);

        if (CameraFollow.Instance) CameraFollow.Instance.HitShake(hitShakeDuration, hitShakeMagnitude);
        if (GameManager.instance) GameManager.instance.HitStop(playerHitStopDuration);
        if (sfxHit && SoundManager.instance) SoundManager.instance.PlaySFX(sfxHit, 0.9f);

        StartCoroutine(Co_OnHit());
    }

    IEnumerator Co_OnHit()
    {
        isInvincible = true;
        float timer = 0f;

        if (flashMaterial && spriteRenderer)
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

        if (spriteRenderer)
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

        if (dashDustPrefab != null)
        {
            CreateDust();
        }

        if (sfxDash != null && SoundManager.instance != null) SoundManager.instance.PlaySFX(sfxDash, 0.4f);
        if (CameraFollow.Instance != null) CameraFollow.Instance.HitShake(shakeDuration, shakeMagnitude);

        StartCoroutine(DashGhostRoutine());
        
        float scaledTimer = 0f;
        float realTimer = 0f;

        // ★ 제한시간을 1.0초로 넉넉하게 변경하여 정상적인 슬로우 모션을 방해하지 않게 함
        while (scaledTimer < dashDuration && realTimer < 1.0f)
        {
            // ★ 핵심 버그 수정: UI 진입(일시정지) 시 폭주하는 시간(스파이크)을 0.1초로 제한하여 대시가 증발하는 것을 방지
            float safeUnscaledDelta = Mathf.Min(Time.unscaledDeltaTime, 0.1f);

            // UI 모드가 아닐 때만 물리력 및 시간 증가 적용 (UI 모드 중 대시 헛돎 방지)
            if (InputStateManager.Instance.CurrentInputState != InputState.UI)
            {
                rigid.linearVelocity = dashDir * dashSpeed;
                scaledTimer += Time.deltaTime;
                realTimer += safeUnscaledDelta;
            }
            else
            {
                rigid.linearVelocity = Vector2.zero; // UI 열림 시 정지
            }

            yield return new WaitForFixedUpdate();
        }

        rigid.linearVelocity = Vector2.zero;
        _isDashing = false;
        isInvincible = false;
    }

    private IEnumerator DashGhostRoutine()
    {
        while (_isDashing)
        {
            // UI 정지 중에는 잔상 생성 방지
            if (InputStateManager.Instance.CurrentInputState != InputState.UI)
            {
                CreateGhost();
            }
            yield return new WaitForSeconds(ghostInterval);
        }
    }

    private void InitPools()
    {
        _ghostPoolContainer = new GameObject("Player_GhostPool").transform;
        if (poolParent != null) _ghostPoolContainer.SetParent(poolParent);

        for (int i = 0; i < ghostPoolSize; i++)
        {
            GameObject ghostObj = CreateNewGhostObject();
            ghostObj.SetActive(false);
            _ghostPool.Enqueue(ghostObj);
        }

        _dustPoolContainer = new GameObject("Player_DustPool").transform;
        if (poolParent != null) _dustPoolContainer.SetParent(poolParent);

        if (dashDustPrefab != null)
        {
            for (int i = 0; i < dustPoolSize; i++)
            {
                GameObject dustObj = Instantiate(dashDustPrefab, _dustPoolContainer);
                dustObj.SetActive(false);
                _dustPool.Enqueue(dustObj);
            }
        }
    }

    private GameObject CreateNewGhostObject()
    {
        GameObject ghostObj = new GameObject("DashGhost");
        ghostObj.transform.SetParent(_ghostPoolContainer);
        ghostObj.AddComponent<SpriteRenderer>();
        return ghostObj;
    }

    private void CreateGhost()
    {
        GameObject ghostObj = null;

        while (_ghostPool.Count > 0)
        {
            ghostObj = _ghostPool.Dequeue();
            if (ghostObj != null) break;
        }

        if (ghostObj == null) ghostObj = CreateNewGhostObject();

        ghostObj.transform.position = transform.position;
        ghostObj.transform.localScale = transform.localScale;

        SpriteRenderer sr = ghostObj.GetComponent<SpriteRenderer>();
        sr.sprite = spriteRenderer.sprite;
        sr.color = ghostColor;
        sr.flipX = spriteRenderer.flipX;
        sr.sortingLayerID = spriteRenderer.sortingLayerID;
        sr.sortingOrder = spriteRenderer.sortingOrder - 1;

        ghostObj.SetActive(true);
        StartCoroutine(FadeOutAndReturnGhost(ghostObj, sr));
    }

    private IEnumerator FadeOutAndReturnGhost(GameObject obj, SpriteRenderer sr)
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

        obj.SetActive(false);
        _ghostPool.Enqueue(obj);
    }

    private void CreateDust()
    {
        GameObject dustObj = null;

        while (_dustPool.Count > 0)
        {
            dustObj = _dustPool.Dequeue();
            if (dustObj != null) break;
        }

        if (dustObj == null) dustObj = Instantiate(dashDustPrefab, _dustPoolContainer);

        dustObj.transform.position = transform.position;
        dustObj.transform.rotation = Quaternion.identity;

        dustObj.SetActive(true);
        StartCoroutine(DeactivateAndReturnDust(dustObj, 1.0f));
    }

    private IEnumerator DeactivateAndReturnDust(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
        _dustPool.Enqueue(obj);
    }

    public void OnDie()
    {
        isAttacking = false;
        _isDashing = false;
        isInvincible = true;
        rigid.linearVelocity = Vector2.zero;
        rigid.simulated = false;
        canControl = false;

        StopAllCoroutines();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            spriteRenderer.material = _originalMaterial;
        }

        if (_ghostPoolContainer != null)
        {
            foreach (Transform child in _ghostPoolContainer)
            {
                if (child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                    _ghostPool.Enqueue(child.gameObject);
                }
            }
        }
        if (_dustPoolContainer != null)
        {
            foreach (Transform child in _dustPoolContainer)
            {
                if (child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                    _dustPool.Enqueue(child.gameObject);
                }
            }
        }

        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.ChangeInputState(InputState.UI);
        }

        if (anim != null)
        {
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;
            anim.SetTrigger("Die");
        }

        if (SoundManager.instance != null)
        {
            SoundManager.instance.StopBGM(1.5f);
        }

        StartCoroutine(Co_ForceCleanVisuals());

        if (CinematicManager.Instance != null)
        {
            CinematicManager.Instance.PlayGameOverCinematic(this.transform);
        }

        Debug.Log("Player: Dead");
    }

    private IEnumerator Co_ForceCleanVisuals()
    {
        yield return new WaitForEndOfFrame();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            spriteRenderer.material = _originalMaterial;
        }

        yield return new WaitForSecondsRealtime(1.5f);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            spriteRenderer.material = _originalMaterial;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, contactCheckRadius);
    }
}