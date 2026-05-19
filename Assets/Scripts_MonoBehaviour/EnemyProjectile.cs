using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Pool Settings")]
    [Tooltip("프리팹별로 다른 인덱스를 설정하세요 (예: 벌=0, 엘리트벌=1, 새=2)")]
    public int poolIndex = 0; // ★ 인스펙터에서 설정할 수 있는 풀 인덱스

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

    // ★ 인덱스별로 분류되는 딕셔너리 큐 (프리팹이 달라도 인덱스가 같으면 같은 풀, 인덱스가 다르면 다른 풀 사용)
    private static Dictionary<int, Queue<EnemyProjectile>> poolDict = new Dictionary<int, Queue<EnemyProjectile>>();
    private static Transform poolContainer;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
    }
    
    public static EnemyProjectile Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (poolContainer == null)
            poolContainer = new GameObject("EnemyProjectile_Pool").transform;

        // ★ 스폰할 프리팹이 가지고 있는 poolIndex를 읽어옵니다.
        EnemyProjectile prefabScript = prefab.GetComponent<EnemyProjectile>();
        int targetIndex = prefabScript != null ? prefabScript.poolIndex : 0;

        // 해당 인덱스의 풀이 없으면 새로 생성
        if (!poolDict.ContainsKey(targetIndex))
        {
            poolDict[targetIndex] = new Queue<EnemyProjectile>();
        }

        EnemyProjectile ep;
        if (poolDict[targetIndex].Count > 0)
        {
            ep = poolDict[targetIndex].Dequeue();
            ep.transform.position = position;
            ep.transform.rotation = rotation;
            ep.gameObject.SetActive(true);
        }
        else
        {
            GameObject obj = Instantiate(prefab, position, rotation, poolContainer);
            ep = obj.GetComponent<EnemyProjectile>();
            ep.poolIndex = targetIndex; // 생성된 객체에 확실히 인덱스 각인
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
        // 타격 이펙트도 풀링으로 스폰
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

    // 파괴 대신 본인의 인덱스 풀로 반환
    private void ReturnToPool()
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        if (lifeTimerCoroutine != null) StopCoroutine(lifeTimerCoroutine);
        
        rigid.linearVelocity = Vector2.zero;
        gameObject.SetActive(false);

        // 안전망
        if (!poolDict.ContainsKey(poolIndex))
        {
            poolDict[poolIndex] = new Queue<EnemyProjectile>();
        }
        poolDict[poolIndex].Enqueue(this);
    }
}