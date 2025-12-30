using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 50f; // 저격총이니까 아주 빠르게!
    public float lifeTime = 2f; // 2초 뒤에 자동 삭제 (메모리 관리)

    [SerializeField] private float damageMultiplier = 1.0f;

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
        if (collision.CompareTag("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();

            if (enemy != null)
            {
                CalculateAndDealDamage(enemy);
            }

            // 적 맞추면 총알 삭제 (관통 필요하면 이 부분 로직 수정)
            Destroy(gameObject);
        }
    }

    private void CalculateAndDealDamage(Enemy enemy)
    {
        // 1. 플레이어 스탯 가져오기
        // (GameManager에 stats가 연결되어 있어야 함)
        PlayerStats stats = GameManager.instance.stats;

        // 2. 기본 원거리 공격력 * 총알 계수
        float baseDamage = stats.rangeAttackPower * damageMultiplier;

        // 3. 원거리 숙련도(오차) 적용
        float variance = stats.rangedDamageVariance;
        float maxRandom = 1.1f; // 최대 1.1배
        float minRandom = maxRandom - variance; // 오차만큼 깎임

        float randomMultiplier = Random.Range(minRandom, maxRandom);

        // 4. 최종 데미지 계산 및 반올림
        int finalDamage = Mathf.RoundToInt(baseDamage * randomMultiplier);

        // 최소 1 데미지 보장
        if (finalDamage < 1) finalDamage = 1;

        // 5. 적에게 전달
        enemy.TakeDamage(finalDamage);
    }
}
