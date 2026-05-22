using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sword_Effect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float damageMultiplier = 1.0f;
    [SerializeField] private int ammoGain = 10;

    [Header("Multi Hit Settings")]
    [SerializeField] private int hitCount;
    [SerializeField] private float hitInterval;

    [Header("Hit VFX")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject criticalHitEffectPrefab;
    [SerializeField] private float baseRotationOffset = 0f;
    [SerializeField] private float randomRotationOffset = 12f;
    [SerializeField] private float hitEffectLifetime = 1.0f;

    private SpriteRenderer spriteRenderer;
    private CapsuleCollider2D capsule;

    private float cachedMeleeAttackPower = 0f;
    private float cachedMeleeVariance = 0f;
    private float cachedCriticalChance = 0f;
    private float cachedCriticalDamageMultiplier = 1.5f;
    private float cachedDiceDamageMultiplier = 1f;
    private float cachedStrongAttackMultiplier = 1f;

    private static Queue<GameObject> _hitEffectPool = new Queue<GameObject>();
    private static Queue<GameObject> _critHitEffectPool = new Queue<GameObject>();
    private static Transform _effectPoolContainer; 

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        capsule = GetComponent<CapsuleCollider2D>();
    }

    public void SetupAttackData(float strongAttackMultiplier)
    {
        if (GameManager.instance == null || GameManager.instance.stats == null)
            return;

        PlayerStats stats = GameManager.instance.stats;

        cachedMeleeAttackPower = stats.meleeAttackPower;
        cachedMeleeVariance = stats.meleeDamageVariance;
        cachedCriticalChance = stats.criticalChance;
        cachedCriticalDamageMultiplier = stats.GetFinalCriticalDamageMultiplier();
        cachedDiceDamageMultiplier = stats.diceDamageMultiplier;
        cachedStrongAttackMultiplier = strongAttackMultiplier;
    }

    private void OnEnable()
    {
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        StopAllCoroutines();
        StartCoroutine(FadeOutRoutine());
        ProcessHit();
    }

    private void ProcessHit()
    {
        if (capsule == null) return;

        Physics2D.SyncTransforms();

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask("Enemy"));
        filter.useLayerMask = true;
        filter.useTriggers = true;

        Collider2D[] hits = new Collider2D[10];
        int overlapCount = capsule.Overlap(filter, hits);

        for (int i = 0; i < overlapCount; i++)
        {
            if (hits[i] == null) continue;

            Enemy enemy = hits[i].GetComponent<Enemy>();
            if (enemy != null && enemy.gameObject != null)
            {
                StartCoroutine(ProcessMultiHit(enemy));
            }
        }
    }

    private IEnumerator ProcessMultiHit(Enemy enemy)
    {
        if (GameManager.instance == null || GameManager.instance.stats == null) yield break;
        PlayerStats stats = GameManager.instance.stats;

        float baseDamage =
            cachedMeleeAttackPower *
            damageMultiplier *
            cachedDiceDamageMultiplier *
            cachedStrongAttackMultiplier;

        for (int i = 0; i < hitCount; i++)
        {
            // ★ 수정: 다단 히트 도중에 적이 죽었거나 파괴되었는지 이중 체크
            if (enemy == null || enemy.gameObject == null || !enemy.gameObject.activeInHierarchy || enemy.IsDead)
                yield break;

            float randomMultiplier = Random.Range(1.1f - cachedMeleeVariance, 1.1f);
            float finalDamage = baseDamage * randomMultiplier;

            bool isCritical = Random.value < cachedCriticalChance;

            if (isCritical)
            {
                finalDamage *= cachedCriticalDamageMultiplier;
            }

            int damageInt = Mathf.RoundToInt(finalDamage);
            if (damageInt < 1) damageInt = 1;

            enemy.TakeDamage(damageInt, isCritical);

            if (GameManager.instance != null)
            {
                GameManager.instance.totalDamageDealt += damageInt;
            }

            // 대미지를 주고 나서 적이 파괴되었는지 한 번 더 체크 (오류 방지)
            if (enemy == null || enemy.gameObject == null || !enemy.gameObject.activeInHierarchy)
                yield break;

            SpawnHitEffect(enemy, isCritical);

            if (stats.currentAmmo < stats.maxAmmo)
            {
                stats.currentAmmo = Mathf.Min(stats.currentAmmo + ammoGain, stats.maxAmmo);
            }

            if (i < hitCount - 1)
            {
                yield return new WaitForSeconds(hitInterval);
            }
        }
    }

    private void SpawnHitEffect(Enemy enemy, bool isCritical)
    {
        if (enemy == null || !enemy.gameObject.activeSelf) return;
        if (GameManager.instance == null || GameManager.instance.player == null) return;

        GameObject prefab = isCritical ? criticalHitEffectPrefab : hitEffectPrefab;
        Queue<GameObject> targetPool = isCritical ? _critHitEffectPool : _hitEffectPool;

        if (prefab == null) return;

        // ★ 수정: 풀 초기화 시 이전 씬의 쓰레기 참조 제거 로직 추가
        if (_effectPoolContainer == null)
        {
            _effectPoolContainer = new GameObject("SwordHitEffect_Pool").transform;
            _hitEffectPool.Clear();
            _critHitEffectPool.Clear();
        }

        Transform playerTransform = GameManager.instance.player.transform;
        Vector3 playerPosition = playerTransform.position;
        Vector3 hitPosition = enemy.transform.position;

        Vector2 attackDirection = ((Vector2)enemy.transform.position - (Vector2)playerPosition).normalized;
        if (attackDirection.sqrMagnitude <= 0.0001f)
        {
            attackDirection = Vector2.right;
        }

        Vector2 perpendicularDirection = new Vector2(-attackDirection.y, attackDirection.x);
        float angle = Mathf.Atan2(perpendicularDirection.y, perpendicularDirection.x) * Mathf.Rad2Deg;

        angle += baseRotationOffset;
        angle += Random.Range(-randomRotationOffset, randomRotationOffset);

        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        
        GameObject vfx = null;
        
        // 파괴된 객체 건너뛰기
        while (targetPool.Count > 0)
        {
            vfx = targetPool.Dequeue();
            if (vfx != null) break;
        }

        if (vfx != null)
        {
            vfx.transform.position = hitPosition;
            vfx.transform.rotation = rotation;
            vfx.SetActive(true);
        }
        else
        {
            vfx = Instantiate(prefab, hitPosition, rotation, _effectPoolContainer);
        }

        SwordEffectReturner returner = vfx.GetComponent<SwordEffectReturner>();
        if (returner == null) returner = vfx.AddComponent<SwordEffectReturner>();

        returner.StartDelayReturn(hitEffectLifetime, () =>
        {
            if (vfx != null)
            {
                vfx.SetActive(false);
                targetPool.Enqueue(vfx);
            }
        });
    }

    private IEnumerator FadeOutRoutine()
    {
        yield return new WaitForSeconds(duration * 0.5f);

        float fadeTime = duration * 0.5f;
        float elapsed = 0f;
        Color startColor = spriteRenderer.color;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / fadeTime;
            float alpha = Mathf.Pow(1f - t, 3f);

            startColor.a = alpha;
            spriteRenderer.color = startColor;

            yield return null;
        }

        gameObject.SetActive(false);
    }
}

public class SwordEffectReturner : MonoBehaviour
{
    public void StartDelayReturn(float delay, System.Action onComplete)
    {
        StartCoroutine(Co_Delay(delay, onComplete));
    }

    private System.Collections.IEnumerator Co_Delay(float d, System.Action act)
    {
        yield return new WaitForSeconds(d);
        act?.Invoke();
    }
}