using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] protected float maxHealth = 100f; // protected: 자식도 쓸 수 있게 해줌
    [SerializeField] protected float currentHealth;

    [Header("Effect")]
    [SerializeField] private GameObject damageTextPrefab;

    [Header("HpBar")]
    [SerializeField] private Transform hpBarFill;
    private float initialScaleX;

    protected virtual void Start()
    {
        currentHealth = maxHealth;

        if (hpBarFill != null)
        {
            initialScaleX = hpBarFill.localScale.x;
        }
    }

    // 데미지 받는 함수 (모든 몬스터 공통)
    public virtual void TakeDamage(float damage)
    {
        currentHealth -= damage;

        // ★ 체력바 계산
        if (hpBarFill != null)
        {
            float ratio = currentHealth / maxHealth;
            if (ratio < 0) ratio = 0;
            hpBarFill.localScale = new Vector3(initialScaleX * ratio, hpBarFill.localScale.y, hpBarFill.localScale.z);
        }

        // ★ 데미지 텍스트 띄우기
        if (damageTextPrefab != null)
        {
            // 1. 데미지 텍스트 생성 (위치는 내 머리 위쯤)
            Vector3 randomOffset = new Vector3(Random.Range(-0.1f, 0.1f),0.65f, 0);
            GameObject hud = Instantiate(damageTextPrefab, transform.position + randomOffset, Quaternion.identity);

            // 2. 데미지 수치 전달 (스크립트 가져와서)
            hud.GetComponent<DamageText>().Setup(damage);
        }

        // ★ 죽음 처리
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        // 죽었을 때 공통 처리 (아이템 드랍 등)
        Destroy(gameObject);
    }
}

