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

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 적 감지
        if (collision.CompareTag("Enemy"))
        {
            // EnemyDummy가 아니라 부모인 Enemy를 찾음!
            Enemy enemy = collision.GetComponent<Enemy>();

            if (enemy != null)
            {
                enemy.TakeDamage((int)Random.Range(11,99f)); // 샌드백이든 보스든 다 똑같이 맞음, 테스트 데미지
            }

            // (관통 로직에 따라 Destroy 위치 결정)
            Destroy(gameObject);
        }
    }
}
