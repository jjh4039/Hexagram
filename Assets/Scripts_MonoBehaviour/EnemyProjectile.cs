using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    private Coroutine lifeTimerCoroutine;

    // ★ 투사체 풀링용 큐 및 컨테이너
    private static Queue<EnemyProjectile> pool = new Queue<EnemyProjectile>();
    private static Transform poolContainer;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
    }
    
    public static EnemyProjectile Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (poolContainer == null)
            poolContainer = new GameObject("EnemyProjectile_Pool").transform;

        EnemyProjectile ep;
        if (pool.Count > 0)
        {
            ep = pool.Dequeue();
            ep.transform.position = position;
            ep.transform.rotation = rotation;
            ep.gameObject.SetActive(true);
        }
        else
        {
            GameObject obj = Instantiate(prefab, position, rotation, poolContainer);
            ep = obj.GetComponent<EnemyProjectile>();
        }
        return ep;
    }

    private void OnEnable()
    {
        if (baseScale == Vector3.zero) 
            baseScale = transform.localScale;

        if (lifeTimerCoroutine != null) 
            StopCoroutine(lifeTimerCoroutine);
        
        lifeTimerCoroutine = StartCoroutine(Co_LifeTimer());
    }

    private IEnumerator Co_LifeTimer()
    {
        yield return new WaitForSeconds(lifeTime);
        ReturnToPool();
    }

    public void Initialize(Vector2 direction, float overrideSpeed, float projDamage)
    {
        moveDir = direction.normalized;
        speed = overrideSpeed;
        damage = projDamage;

        RotateToDirection();

        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);

        transform.localScale = baseScale * spawnScaleMultiplier;
        scaleCoroutine = StartCoroutine(Co_RecoverScale());

        rigid.linearVelocity = moveDir * speed;
    }

    void SpawnHitEffect(Vector3 hitPosition)
    {
        if (hitEffectPrefab == null) return;
        // ★ 타격 이펙트도 풀링으로 스폰
        EnemyProjectileFlash.Spawn(hitEffectPrefab, hitPosition, Quaternion.Euler(0, 0, transform.eulerAngles.z + 180f));
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
            ReturnToPool();
            return;
        }

        if (collision.CompareTag("Wall"))
        {
            SpawnHitEffect(transform.position);
            ReturnToPool();
        }
    }

    // 파괴 대신 풀로 반환
    private void ReturnToPool()
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        if (lifeTimerCoroutine != null) StopCoroutine(lifeTimerCoroutine);
        
        rigid.linearVelocity = Vector2.zero;
        gameObject.SetActive(false);
        pool.Enqueue(this);
    }
}