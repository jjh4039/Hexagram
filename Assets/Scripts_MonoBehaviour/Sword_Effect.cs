using System.Collections;
using UnityEngine;

public class Sword_Effect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float damageMultiplier = 1.0f;
    [SerializeField] private int ammoGain = 12;

    [Header("Multi Hit Settings")]
    [SerializeField] private int hitCount = 1;
    [SerializeField] private float hitInterval = 0.05f;

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
        int hitCount = capsule.Overlap(filter, hits);

        for (int i = 0; i < hitCount; i++)
        {
            if (hits[i] != null)
            {
                Enemy enemy = hits[i].GetComponent<Enemy>();
                if (enemy != null)
                {
                    StartCoroutine(ProcessMultiHit(enemy));
                }
            }
        }
    }

    private IEnumerator ProcessMultiHit(Enemy enemy)
    {
        PlayerStats stats = GameManager.instance.stats;

        // ★ [버그 수정 완료] stats.damageMultiplier 로 수정
        float currentDmg = stats.meleeAttackPower * damageMultiplier * stats.damageMultiplier;

        // ★ [버그 수정 완료] stats.remainingStrongAttacks 로 수정
        if (stats.remainingStrongAttacks > 0)
        {
            currentDmg *= 2.0f;
            stats.remainingStrongAttacks--;
        }

        float variance = stats.meleeDamageVariance;

        for (int i = 0; i < hitCount; i++)
        {
            if (enemy == null || !enemy.gameObject.activeSelf)
                yield break;

            float randomMult = Random.Range(1.1f - variance, 1.1f);
            int finalDamage = Mathf.RoundToInt(currentDmg * randomMult);
            if (finalDamage < 1) finalDamage = 1;

            enemy.TakeDamage(finalDamage);

            if (stats.currentAmmo < stats.maxAmmo)
                stats.currentAmmo = Mathf.Min(stats.currentAmmo + ammoGain, stats.maxAmmo);

            if (i < hitCount - 1)
                yield return new WaitForSeconds(hitInterval);
        }
    }

    private IEnumerator FadeOutRoutine()
    {
        yield return new WaitForSeconds(duration * 0.7f);

        float fadeTime = duration * 0.3f;
        float elapsed = 0f;
        Color startColor = spriteRenderer.color;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            startColor.a = alpha;
            spriteRenderer.color = startColor;
            yield return null;
        }

        gameObject.SetActive(false);
    }
}