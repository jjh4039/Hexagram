using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] public Rigidbody2D rigid;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PlayerInput inputActions;

    [Header("Var")]
    [SerializeField] private float moveSpeed = 5f; // 이동속도
    [SerializeField] private Vector2 moveInput;
    [SerializeField] public bool isAttacking = false;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>(); // 추가
        inputActions = new PlayerInput();

        rigid.gravityScale = 0; // 중력 제거
        rigid.interpolation = RigidbodyInterpolation2D.Interpolate; // 프레임 끊김 방지
    }

    private void OnEnable()
    {
        inputActions.Enable(); // 입력 활성화
    }

    private void OnDisable()
    {
        inputActions.Disable(); // 입력 비활성화
    }

    private void Update()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>(); // 기초 이동 관련
        LookAtMouse();
    }

    private void FixedUpdate()
    {
        if (!isAttacking) Move(); // 기초 이동
    }

    private void Move() // 기초 이동
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

    private void LookAtMouse() // 마우스 쳐다보기
    {
        Vector2 mouseScreenPos = inputActions.Player.Look.ReadValue<Vector2>();
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        if (mousePos.x < transform.position.x)
        {
            spriteRenderer.flipX = true;
        }
        else
        {
            spriteRenderer.flipX = false;
        }
    }
}
