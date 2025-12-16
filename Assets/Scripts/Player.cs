using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 5f; // 이동속도

    [Header("Components")]
    private Rigidbody2D rigid;

    [SerializeField] private Vector2 moveInput;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();

        rigid.gravityScale = 0; // 중력 제거 (탑뷰 최적화)
        rigid.interpolation = RigidbodyInterpolation2D.Interpolate; // 프레임 끊김 방지
    }   

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    private void FixedUpdate()
    {
        Move();
      
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
           // rb.linearVelocity = Vector2.zero;

        }
    }
}
