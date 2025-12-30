
using System.Collections;
using UnityEngine;

public class Sword_Effect : MonoBehaviour
{
    [SerializeField] private float duration; // 이펙트가 머무는 시간

    [SerializeField] private float damageMultiplier = 1.0f; // 데미지 배율
    [SerializeField] private int ammoGain = 12; // 원거리 수급량

    [Header("Multi Hit Settings")]
    [SerializeField] private int hitCount = 1; // 연속 공격 수
    [SerializeField] private float hitInterval = 0.05f; // 연속 공격 간격

    private void OnEnable()
    {
        // 켜지는 순간 카운트다운 시작
        CancelInvoke();
        Invoke("Disable", duration);
    }

    private void Disable()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 적 태그 확인
        if (other.CompareTag("Enemy"))
        {
            // 2. 부모 클래스인 'Enemy' 컴포넌트를 찾음 (Dummy도 Enemy의 자식이라 찾아짐)
            Enemy enemy = other.GetComponent<Enemy>();

            if (enemy != null)
            {
                // ▼▼▼ [수정] 그냥 때리는 게 아니라 코루틴 실행 ▼▼▼
                StartCoroutine(ProcessMultiHit(enemy));
            }
        }
    }

    private IEnumerator ProcessMultiHit(Enemy enemy)
    {
        PlayerStats stats = GameManager.instance.stats;

        // 2. 기본 공격력 계산 (플레이어 공격력 * 스킬 계수)
        float baseDamage = stats.meleeAttackPower * damageMultiplier;

        // 3. 오차 범위 가져오기 (예: 0.3 = 30%만큼 데미지가 깎일 수 있음)
        float variance = stats.meleeDamageVariance;

        for (int i = 0; i < hitCount; i++)
        {
            if (enemy == null) yield break;

            float maxRandom = 1.1f;
            float minRandom = maxRandom - variance;

            float randomMultiplier = Random.Range(minRandom, maxRandom);

            float finalDamage = baseDamage * randomMultiplier;

            // 데미지 반올림
            int damageInt = Mathf.RoundToInt(finalDamage);

            // (추천) 최소 데미지 보정: 반올림해서 0이 되더라도 최소 1은 뜨게!
            if (damageInt < 1) damageInt = 1;

            enemy.TakeDamage(damageInt); // 정수로 전달

            // 총알 충전
            stats.currentAmmo += ammoGain;

            // 최대치 넘지 않게 막기 (MaxAmmo는 PlayerStats에 있다고 가정)
            if (stats.currentAmmo > stats.maxAmmo)
            {
                stats.currentAmmo = stats.maxAmmo;
            }

            if (i < hitCount - 1)
                yield return new WaitForSeconds(hitInterval);
        }
    }
}
