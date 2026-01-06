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
    public bool isCharging = false; // ★ 핵심 변수 (무기들이 이걸 쳐다봄)

    [Header("Hit & Invincibility")]
    [SerializeField] private bool isInvincible = false;
    [SerializeField] private float invincibleTime = 1.0f;
    [SerializeField] private float blinkSpeed = 0.2f;
    [SerializeField] private float bodyContactDamage = 5f;

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

    private void HandleSpeedInterpolation()
    {
        float targetSpeed = isCharging ? minMoveSpeed : defaultMoveSpeed;
        currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, targetSpeed, Time.deltaTime * speedChangeRate);
    }

    // ★ 주사위(Dice)가 호출하는 함수
    public void SetChargingState(bool _isCharging)
    {
        isCharging = _isCharging;

        if (anim != null)
        {
            anim.SetBool("IsCharging", isCharging);
        }
    }

    public void SetDiceAnimation(int diceIndex)
    {
        if (anim != null)
        {
            // Blend Tree의 파라미터 값 변경 (0 ~ 5)
            anim.SetFloat("DiceType", (float)diceIndex);
        }
    }

    public void OnDamage(float damage)
    {
        if (isInvincible) return;
        if (stats != null) stats.TakeDamage((int)damage);
        StartCoroutine(Co_OnHit());
    }

    // ... (충돌 및 이동 로직은 기존 유지) ...
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
            rigid.linearVelocity = moveInput.normalized * currentMoveSpeed;
        else
            rigid.linearVelocity = Vector2.zero;
    }

    private void LookAtMouse()
    {
        Vector2 mouseScreenPos = inputActions.Player.Look.ReadValue<Vector2>();
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        spriteRenderer.flipX = mousePos.x < transform.position.x;
    }
}