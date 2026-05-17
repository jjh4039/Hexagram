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

    // 초기화 시 데미지도 받아와 덮어씌움
    public void Initialize(Vector2 direction, float overrideSpeed, float projDamage)
    {
        moveDir = direction.normalized;
        speed = overrideSpeed;
        damage = projDamage; // 에너미 스크립트에서 받아온 투사체 데미지 적용

        RotateToDirection();

        baseScale = transform.localScale;

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

        effect.transform.right = -moveDir;
    }

    IEnumerator Co_RecoverScale()
    {
        float halfTime = scaleRecoverTime * 0.5f;
        float t = 0f;

        Vector3 enlarged = baseScale * spawnScaleMultiplier;
        Vector3 undershoot = baseScale * 0.95f;

        while (t < halfTime)
        {
            t += Time.deltaTime;
            float lerp = t / halfTime;
            transform.localScale = Vector3.Lerp(enlarged, undershoot, lerp);
            yield return null;
        }

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