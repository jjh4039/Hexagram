using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 50f;
    public float lifeTime = 2f;

    [Header("VFX")]
    [SerializeField] private GameObject hitEffectPrefab; // 타격 이펙트 (폭발 등)
    [SerializeField] private float damageMultiplier = 1.0f;

    private Rigidbody2D rigid;
    private SpriteRenderer spriteRenderer; // ★ [추가] 숨기기 위해 필요
    private Collider2D col;                // ★ [추가] 충돌 끄기 위해 필요

    private Color myColor = Color.white;
    private Material myMaterial;

    // 이미 맞았는지 확인하는 변수 (중복 충돌 방지)
    private bool hasHit = false;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>(); // ★ 컴포넌트 가져오기
        col = GetComponent<Collider2D>();                // ★ 컴포넌트 가져오기
    }

    public void SetupVisuals(Color color, Material material)
    {
        myColor = color;
        myMaterial = material;

        // 총알 자체 색상도 변경 (선택사항)
        if (spriteRenderer != null) spriteRenderer.color = color;
    }

    void Start()
    {
        rigid.linearVelocity = transform.right * speed;
        Destroy(gameObject, lifeTime); // 아무것도 안 맞았을 때 자연 소멸
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 이미 어딘가에 맞아서 사라지는 중이라면 무시
        if (hasHit) return;

        // 1. 적(Enemy)과 충돌했을 때
        if (collision.CompareTag("Enemy"))
        {
            hasHit = true; // 맞았음 표시

            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                CalculateAndDealDamage(enemy);
                SpawnHitEffect(transform.position); // 이펙트 생성
            }

            // ★ 바로 Destroy 하지 않고, 숨기기 함수 호출
            HideAndDelayDestroy();
        }
        // 2. 벽(Wall)과 충돌했을 때 (★ 추가됨)
        else if (collision.CompareTag("Wall"))
        {
            hasHit = true;

            // 벽 파편 이펙트도 여기서 생성 가능 (지금은 적이랑 같은 거 씀)
            SpawnHitEffect(transform.position);

            // ★ 벽에 박혔으니 숨기기 처리
            HideAndDelayDestroy();
        }
    }

    // ★ [핵심 기능] 총알을 투명하게 만들고 나중에 삭제하는 함수
    private void HideAndDelayDestroy()
    {
        // 1. 눈에서 치우기 (이미지 끄기)
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        // 2. 물리 판정 치우기 (더 이상 충돌 안 함)
        if (col != null) col.enabled = false;

        // 3. 움직임 멈추기 (관성으로 굴러가는 것 방지)
        if (rigid != null)
        {
            rigid.linearVelocity = Vector2.zero;
            rigid.bodyType = RigidbodyType2D.Kinematic; // 물리 연산 완전 중지
        }

        // 4. 자식 오브젝트(파티클/Trail)들은 살아있으므로 서서히 사라질 시간을 줌
        // 0.5초 뒤에 진짜로 오브젝트 삭제
        Destroy(gameObject, 0.5f);
    }

    private void SpawnHitEffect(Vector3 position)
    {
        if (hitEffectPrefab == null) return;

        // 반대 방향을 보게 회전 (이펙트가 튀는 방향)
        Quaternion reverseRotation = transform.rotation * Quaternion.Euler(0, 0, 180f);

        GameObject vfx = Instantiate(hitEffectPrefab, position, reverseRotation);

        // 색상 & 재질 적용 로직 (기존과 동일)
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

        Destroy(vfx, 1.0f); // 이펙트 프리팹 삭제
    }

    private void CalculateAndDealDamage(Enemy enemy)
    {
        // (기존 데미지 로직 유지)
        PlayerStats stats = GameManager.instance.stats;
        float baseDamage = stats.rangeAttackPower * damageMultiplier;

        float variance = stats.rangedDamageVariance;
        float randomMultiplier = Random.Range(1.1f - variance, 1.1f);
        float finalDamage = baseDamage * randomMultiplier;

        bool isCritical = Random.value < 0.2f;

        if (GameManager.instance.player.remainingStrongAttacks > 0)
        {
            isCritical = true;
        }

        if (isCritical)
        {
            finalDamage *= 1.5f;
        }

        int damageInt = Mathf.RoundToInt(finalDamage);
        if (damageInt < 1) damageInt = 1;

        enemy.TakeDamage(damageInt, isCritical);
    }
}