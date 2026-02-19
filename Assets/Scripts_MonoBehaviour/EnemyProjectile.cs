using UnityEngine;
using System.Collections;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 10f;

    [Header("Movement")]
    [SerializeField] private float speed = 8f;

    [Header("Lifetime")]
    [SerializeField] private float lifeTime = 5f;

    [Header("Spawn Scale Effect")]
    [SerializeField] private float spawnScaleMultiplier = 1.15f;
    [SerializeField] private float scaleRecoverTime = 0.06f;

    [Header("Hit Effect")]
    [SerializeField] private GameObject hitEffectPrefab;

    private Rigidbody2D rigid;
    private Vector2 moveDir;

    private Vector3 baseScale;
    private Coroutine scaleCoroutine;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void Initialize(Vector2 direction, float overrideSpeed)
    {
        moveDir = direction.normalized;
        speed = overrideSpeed;

        RotateToDirection();

        // 현재 스케일을 기준으로 저장 (0.5든 1이든 대응)
        baseScale = transform.localScale;

        // 이전 코루틴 정리 (풀링 대비)
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        transform.localScale = baseScale * spawnScaleMultiplier;
        scaleCoroutine = StartCoroutine(Co_RecoverScale());

        rigid.linearVelocity = moveDir * speed;
    }

    void SpawnHitEffect(Vector3 hitPosition)
    {
        if (hitEffectPrefab == null) return;

        GameObject effect = Instantiate(
            hitEffectPrefab,
            hitPosition,
            Quaternion.identity);

        // 진행 방향 반대로 향하게
        effect.transform.right = -moveDir;
    }

    IEnumerator Co_RecoverScale()
    {
        float halfTime = scaleRecoverTime * 0.5f;
        float t = 0f;

        Vector3 enlarged = baseScale * spawnScaleMultiplier;
        Vector3 undershoot = baseScale * 0.95f;

        // 1단계: 확대 → 살짝 작게
        while (t < halfTime)
        {
            t += Time.deltaTime;
            float lerp = t / halfTime;
            transform.localScale = Vector3.Lerp(enlarged, undershoot, lerp);
            yield return null;
        }

        // 2단계: 살짝 작게 → 원래 크기
        t = 0f;
        while (t < halfTime)
        {
            t += Time.deltaTime;
            float lerp = t / halfTime;
            transform.localScale = Vector3.Lerp(undershoot, baseScale, lerp);
            yield return null;
        }

        transform.localScale = baseScale;
    }

    void RotateToDirection()
    {
        float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            if (player != null)
                player.OnDamage(damage);

            SpawnHitEffect(transform.position);
            Destroy(gameObject);
            return;
        }

        if (collision.CompareTag("Wall"))
        {
            SpawnHitEffect(transform.position);
            Destroy(gameObject);
        }
    }
}
