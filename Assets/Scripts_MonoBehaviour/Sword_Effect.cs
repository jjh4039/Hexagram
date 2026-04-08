using System.Collections;
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

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        capsule = GetComponent<CapsuleCollider2D>();
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
            if (enemy != null)
            {
                StartCoroutine(ProcessMultiHit(enemy));
            }
        }
    }

    private IEnumerator ProcessMultiHit(Enemy enemy)
    {
        PlayerStats stats = GameManager.instance.stats;

        float baseDamage = stats.meleeAttackPower * damageMultiplier * stats.damageMultiplier;
        float variance = stats.meleeDamageVariance;

        for (int i = 0; i < hitCount; i++)
        {
            if (enemy == null || !enemy.gameObject.activeSelf)
                yield break;

            float randomMultiplier = Random.Range(1.1f - variance, 1.1f);
            float finalDamage = baseDamage * randomMultiplier;

            bool isCritical = Random.value < stats.criticalChance;

            if (stats.remainingStrongAttacks > 0)
            {
                isCritical = true;
                stats.remainingStrongAttacks--;
            }

            if (isCritical)
            {
                finalDamage *= stats.criticalDamageMultiplier;
            }

            int damageInt = Mathf.RoundToInt(finalDamage);
            if (damageInt < 1) damageInt = 1;

            enemy.TakeDamage(damageInt, isCritical);

            if (enemy == null || !enemy.gameObject.activeSelf)
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

        GameObject selectedEffectPrefab = isCritical && criticalHitEffectPrefab != null
            ? criticalHitEffectPrefab
            : hitEffectPrefab;

        if (selectedEffectPrefab == null) return;

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
        GameObject vfx = Instantiate(selectedEffectPrefab, hitPosition, rotation);

        Destroy(vfx, hitEffectLifetime);
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