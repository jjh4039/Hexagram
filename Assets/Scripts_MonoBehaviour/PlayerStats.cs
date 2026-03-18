using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("--- 생존 스탯 (Health) ---")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("--- 액션 자원 (Resources) ---")]
    public int maxAmmo = 500;
    public int currentAmmo;

    [Header("--- 주사위 자원 (Dice Charge) ---")]
    public float maxDiceCharge = 300f;
    public float currentDiceCharge = 0f;

    [Header("--- 기본 전투력 (Base ATK) ---")]
    public float meleeAttackPower = 10f; // 근거리 공격력 (칼)
    public float rangeAttackPower = 7f;  // 원거리 공격력 (총)

    [Header("--- 대시 스택 (Dash Stacks) ---")]
    public int maxDashStacks = 3;       // 최대 3회 충전
    public float currentDashStacks = 3f; // 현재 보유 스택
    public float dashRechargeRate = 1f;  // 1초에 1스택 충전

    [Header("--- 전투력 편차 (Precision) ---")]
    [Range(0f, 0.5f)] public float meleeDamageVariance = 0.4f;
    [Range(0f, 0.5f)] public float rangedDamageVariance = 0.5f;

    [Header("--- 버프 증폭률 (Buff Multipliers) ---")]
    public float damageMultiplier = 1.0f;
    public float moveSpeedMultiplier = 1.0f;
    public float attackSpeedMultiplier = 1.0f;
    public float chargeSpeedMultiplier = 1.0f;
    public int remainingStrongAttacks = 0;

    float testTimer = 0f;
    float testTimer2 = 0f;

    private void Start()
    {
        currentHealth = maxHealth;
        currentAmmo = maxAmmo;
        currentDiceCharge = 0f;
    }

    private void Update()
    {
        testTimer += Time.deltaTime;
        testTimer2 += Time.deltaTime;

        if (testTimer >= 2f)
        {
            testTimer = 0f;
            currentHealth = Mathf.Min(currentHealth + 1, maxHealth);
        }

        if (testTimer2 >= 0.01f)
        {
            testTimer2 = 0f;
            currentAmmo = Mathf.Min(currentAmmo + 1, maxAmmo);
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"플레이어 체력 감소: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("!!! GAME OVER !!!");
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            GameManager.instance.player.OnDie();
        }
    }
}