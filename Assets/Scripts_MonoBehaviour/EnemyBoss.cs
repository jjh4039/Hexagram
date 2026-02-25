using UnityEngine;
using System.Collections;

public class EnemyBoss : Enemy
{
    [Header("Boss Specific Stats")]
    [SerializeField] private string bossName = "숲의 관리자";

    protected override void Start()
    {
        base.Start(); // 체력 초기화 등 기존 로직 수행

        // 보스 등장 시 UI에 이름과 최대 체력 전달
        if (BossHealthUI.instance != null)
        {
            BossHealthUI.instance.SetupBoss(bossName, maxHealth);
        }
    }

    public override void TakeDamage(float damage, bool isCritical = false)
    {
        if (isDead) return;

        // 기존 데미지 로직 실행 (체력 감소, 히트 플래시 등)
        base.TakeDamage(damage, isCritical);

        // ★ 보스 전용: 데미지 입을 때마다 상단 UI 업데이트 호출
        if (BossHealthUI.instance != null)
        {
            BossHealthUI.instance.UpdateBossHealth(currentHealth);
        }

        // 보스전의 중량감을 위해 카메라 흔들림을 일반 적보다 강하게 추가
        if (CameraFollow.instance != null)
            CameraFollow.instance.HitShake(0.05f, 0.04f);
    }

    protected override void Die()
    {
        base.Die();
        // 보스 사망 시 UI 숨김 처리 등 추가 가능
        if (BossHealthUI.instance != null)
        {
            BossHealthUI.instance.HideUI();
        }
    }
}
