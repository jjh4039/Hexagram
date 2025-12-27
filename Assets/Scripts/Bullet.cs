using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 50f; // 저격총이니까 아주 빠르게!
    public float lifeTime = 2f; // 2초 뒤에 자동 삭제 (메모리 관리)
    public int damage = 10;

    private Rigidbody2D rigid;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // 생성되자마자 자신의 오른쪽(Red Axis) 방향으로 날아감
        rigid.linearVelocity = transform.right * speed;

        // n초 뒤에 스스로 파괴
        Destroy(gameObject, lifeTime);
    }

    // 어딘가에 닿았을 때 (나중에 몬스터 피격 로직 추가할 곳)
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Wall")) // 벽에 닿으면
        {
            Destroy(gameObject); // 즉시 삭제
        }
    }
}
