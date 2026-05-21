using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Pool Settings")]
    [Tooltip("프리팹별로 다른 인덱스를 설정하세요 (예: 벌=0, 엘리트벌=1, 새=2)")]
    public int poolIndex = 0; // 인스펙터 풀 인덱스

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

    private static Dictionary<int, Queue<EnemyProjectile>> poolDict = new Dictionary<int, Queue<EnemyProjectile>>();
    private static Transform poolContainer;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
    }
    
    public static EnemyProjectile Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!poolContainer)
        {
            poolContainer = new GameObject("EnemyProjectile_Pool").transform;
            poolDict.Clear(); // 씬 전환 시 파괴된 오브젝트 찌꺼기 제거
        }

        EnemyProjectile prefabScript = prefab.GetComponent<EnemyProjectile>();
        int targetIndex = prefabScript ? prefabScript.poolIndex : 0;

        if (!poolDict.ContainsKey(targetIndex))
        {
            poolDict[targetIndex] = new Queue<EnemyProjectile>();
        }

        EnemyProjectile ep = null;
        
        while (poolDict[targetIndex].Count > 0)
        {
            ep = poolDict[targetIndex].Dequeue();
            if (ep != null) break; // 파괴된 오브젝트 건너뛰기
        }

        if (ep)
        {
            ep.transform.position = position;
            ep.transform.rotation = rotation;
            ep.gameObject.SetActive(true);
        }
        else
        {
            GameObject obj = Instantiate(prefab, position, rotation, poolContainer);
            ep = obj.GetComponent<EnemyProjectile>();
            ep.poolIndex = targetIndex; 
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

    private void ReturnToPool()
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        if (lifeTimerCoroutine != null) StopCoroutine(lifeTimerCoroutine);
        
        rigid.linearVelocity = Vector2.zero;
        gameObject.SetActive(false);

        if (!poolDict.ContainsKey(poolIndex))
        {
            poolDict[poolIndex] = new Queue<EnemyProjectile>();
        }
        poolDict[poolIndex].Enqueue(this);
    }
}