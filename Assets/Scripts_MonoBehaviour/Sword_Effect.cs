using System.Collections;
using UnityEngine;

public class Sword_Effect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float duration = 0.5f; // 이펙트 지속 시간
    [SerializeField] private float damageMultiplier = 1.0f; // 스킬 자체 배율
    [SerializeField] private int ammoGain = 12; // 탄약 수급량

    [Header("Multi Hit Settings")]
    [SerializeField] private int hitCount = 1;
    [SerializeField] private float hitInterval = 0.05f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        // 색상 초기화 (투명해진 걸 다시 원상복구)
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        // 기존 Invoke 대신 페이드 아웃 코루틴 준비
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
                // 코루틴으로 다단 히트 처리
                StartCoroutine(ProcessMultiHit(enemy));
            }
        }
    }

    private IEnumerator ProcessMultiHit(Enemy enemy)
    {
        // ★ [핵심] 주사위 버프 가져오기
        Player player = GameManager.instance.player;
        PlayerStats stats = GameManager.instance.stats;

        // 1. 기본 데미지 (스탯 * 스킬배율 * 주사위 빨강 버프)
        float currentDmg = stats.meleeAttackPower * damageMultiplier * player.damageMultiplier;

        // 2. 주사위 주황 버프 (강한 공격) 체크
        // 다단히트 전체에 적용할지, 첫 타만 적용할지 결정해야 함 (여기선 전체 적용)
        if (player.remainingStrongAttacks > 0)
        {
            currentDmg *= 2.0f; // 2배 뻥튀기
            player.remainingStrongAttacks--; // 횟수 차감
            Debug.Log("강화된 검격 적중!");
        }

        float variance = stats.meleeDamageVariance;

        for (int i = 0; i < hitCount; i++)
        {
            if (enemy == null || !enemy.gameObject.activeSelf) yield break;

            // 랜덤 오차 적용
            float randomMult = Random.Range(1.1f - variance, 1.1f);
            int finalDamage = Mathf.RoundToInt(currentDmg * randomMult);
            if (finalDamage < 1) finalDamage = 1;

            // 데미지 전달
            enemy.TakeDamage(finalDamage);

            // 탄약 충전 (최대치 초과 방지)
            if (stats.currentAmmo < stats.maxAmmo)
            {
                stats.currentAmmo = Mathf.Min(stats.currentAmmo + ammoGain, stats.maxAmmo);
            }

            if (i < hitCount - 1) yield return new WaitForSeconds(hitInterval);
        }
    }

    // ★ [추가] 서서히 사라지는 페이드 아웃
    private IEnumerator FadeOutRoutine()
    {
        // 지속 시간의 70%는 선명하게 유지
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