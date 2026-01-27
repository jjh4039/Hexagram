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

    [Header("Game Feel")]
    [SerializeField] private float hitStopDuration = 0.07f; // ★ [추가] 멈출 시간 설정 (0.05 ~ 0.1 추천)

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
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
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                // ★ [추가] 적을 때리는 순간! 시간을 멈춰라!
                // 카메라 흔들림(Camera Shake)도 같이 넣으면 금상첨화지만, 일단 정지부터.
                if (GameManager.instance != null)
                {
                    GameManager.instance.HitStop(hitStopDuration);
                }

                StartCoroutine(ProcessMultiHit(enemy));
            }
        }
    }

    private IEnumerator ProcessMultiHit(Enemy enemy)
    {
        Player player = GameManager.instance.player;
        PlayerStats stats = GameManager.instance.stats;

        float currentDmg = stats.meleeAttackPower * damageMultiplier * player.damageMultiplier;

        if (player.remainingStrongAttacks > 0)
        {
            currentDmg *= 2.0f;
            player.remainingStrongAttacks--;
            Debug.Log("강화된 검격 적중!");
        }

        float variance = stats.meleeDamageVariance;

        for (int i = 0; i < hitCount; i++)
        {
            if (enemy == null || !enemy.gameObject.activeSelf) yield break;

            float randomMult = Random.Range(1.1f - variance, 1.1f);
            int finalDamage = Mathf.RoundToInt(currentDmg * randomMult);
            if (finalDamage < 1) finalDamage = 1;

            enemy.TakeDamage(finalDamage);

            if (stats.currentAmmo < stats.maxAmmo)
            {
                stats.currentAmmo = Mathf.Min(stats.currentAmmo + ammoGain, stats.maxAmmo);
            }

            if (i < hitCount - 1) yield return new WaitForSeconds(hitInterval);
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