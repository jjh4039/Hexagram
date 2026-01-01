using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] public Rigidbody2D rigid;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PlayerInput inputActions;
    [SerializeField] private PlayerStats stats;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Vector2 moveInput;
    [SerializeField] public bool isAttacking = false;

    [Header("Hit & Invincibility")]
    [SerializeField] private bool isInvincible = false;
    [SerializeField] private float invincibleTime = 1.0f;
    [SerializeField] private float blinkSpeed = 0.2f;
    [SerializeField] private float bodyContactDamage = 5f;

    private Color paleRed = new Color(1f, 0.3f, 0.3f, 1f);

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        inputActions = new PlayerInput();
        stats = GetComponent<PlayerStats>();

        rigid.gravityScale = 0;
        rigid.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void OnEnable() { inputActions.Enable(); }
    private void OnDisable() { inputActions.Disable(); }

    private void Update()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        LookAtMouse();
    }

    private void FixedUpdate()
    {
        if (!isAttacking) Move();
    }

    public void OnDamage(float damage)
    {
        if (isInvincible) return;

        if (stats != null) stats.TakeDamage((int)damage);
        StartCoroutine(Co_OnHit());
    }

    // ★ 1. 물리 충돌 (Is Trigger 꺼져있을 때)
    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckContactDamage(collision.gameObject);
    }

    // ★ 2. 트리거 충돌 (Is Trigger 켜져있을 때) - 이거 추가하면 100% 됩니다.
    private void OnTriggerStay2D(Collider2D collision)
    {
        CheckContactDamage(collision.gameObject);
    }

    // 충돌 로직 통합 함수
    private void CheckContactDamage(GameObject target)
    {
        if (isInvincible) return;

        if (target.CompareTag("Enemy"))
        {
            Debug.Log("몬스터와 접촉!");
            OnDamage(bodyContactDamage);
        }
    }

    // ★ 투명도 유지 + 연한 빨강 깜빡임
    IEnumerator Co_OnHit()
    {
        isInvincible = true;

        float timer = 0f;
        bool isRed = false;

        while (timer < invincibleTime)
        {
            // 투명도(Alpha)는 건드리지 않고 색상만 교체
            spriteRenderer.color = isRed ? Color.white : paleRed;
            isRed = !isRed;

            yield return new WaitForSeconds(blinkSpeed);
            timer += blinkSpeed;
        }

        // 끝난 후 원래대로 복귀
        spriteRenderer.color = Color.white;
        isInvincible = false;
    }

    private void Move()
    {
        if (moveInput.magnitude > 0)
        {
            Vector2 moveDirection = moveInput.normalized;
            rigid.linearVelocity = moveDirection * moveSpeed;
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

        if (mousePos.x < transform.position.x) spriteRenderer.flipX = true;
        else spriteRenderer.flipX = false;
    }
}