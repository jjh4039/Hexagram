using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 50f;
    public float lifeTime = 2f;

    [Header("VFX")]
    [SerializeField] private GameObject hitEffectPrefab; // ★ 여기에 HitEffect_VFX 프리팹 연결!

    [SerializeField] private float damageMultiplier = 1.0f;

    private Rigidbody2D rigid;

    // ★ 전달받은 색상과 재질을 저장할 변수
    private Color myColor = Color.white;
    private Material myMaterial;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
    }

    // ★ [추가] 총(Gun)에서 색상을 받아오는 함수
    public void SetupVisuals(Color color, Material material)
    {
        myColor = color;
        myMaterial = material;
    }

    void Start()
    {
        rigid.linearVelocity = transform.right * speed;
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

                // ★ [핵심] 적을 맞춘 위치에 이펙트 생성!
                SpawnHitEffect(transform.position); // 총알 위치에서 터짐
            }

            Destroy(gameObject);
        }
    }

    private void SpawnHitEffect(Vector3 position)
    {
        if (hitEffectPrefab == null) return;

        // ★ [핵심] 총알이 날아오던 방향의 '반대 방향'을 바라보는 회전값 계산
        // (총알은 오른쪽을 보고 날아가므로, 반대인 왼쪽을 보게 함 -> 180도 회전)
        Quaternion reverseRotation = transform.rotation * Quaternion.Euler(0, 0, 180f);

        // 1. 이펙트 생성 (위치는 그대로, 회전은 반대로)
        GameObject vfx = Instantiate(hitEffectPrefab, position, reverseRotation);

        // 2. 색상 & 재질 적용
        ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
        ParticleSystemRenderer psr = vfx.GetComponent<ParticleSystemRenderer>();

        if (psr != null && myMaterial != null)
        {
            psr.material = myMaterial;
        }

        if (ps != null)
        {
            var main = ps.main;
            main.startColor = myColor;
            ps.Play();
        }

        Destroy(vfx, 1.0f);
    }

    private void CalculateAndDealDamage(Enemy enemy)
    {
        PlayerStats stats = GameManager.instance.stats;

        // 1. 기본 데미지
        float baseDamage = stats.rangeAttackPower * damageMultiplier;

        // 2. 랜덤 오차 (기존 로직)
        float variance = stats.rangedDamageVariance;
        float randomMultiplier = Random.Range(1.1f - variance, 1.1f);

        float finalDamage = baseDamage * randomMultiplier;

        // ★ [추가] 치명타 로직 (예: 20% 확률)
        // 나중에 PlayerStats에 critChance 변수를 만들어서 가져오면 더 좋습니다.
        bool isCritical = Random.value < 0.2f; // 20% 확률

        // ★ 주사위 버프(주황색)가 켜져있으면 무조건 치명타! (연동)
        if (GameManager.instance.player.remainingStrongAttacks > 0)
        {
            isCritical = true;
        }

        if (isCritical)
        {
            finalDamage *= 1.5f; // 치명타면 데미지 1.5배
        }

        // 반올림 및 최소 데미지 보정
        int damageInt = Mathf.RoundToInt(finalDamage);
        if (damageInt < 1) damageInt = 1;

        // ★ [수정] Enemy에게 치명타 여부(isCritical)를 같이 전달
        enemy.TakeDamage(damageInt, isCritical);
    }
}