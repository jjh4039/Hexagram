using UnityEngine;

public class PlayerStats : MonoBehaviour
{

    [Header("Survival Stats")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Resource Stats")]
    public int maxAmmo = 500;
    public int currentAmmo;
    public float ammoRechargeRate = 100f;

    [Header("Dice Charge Stats")]
    public float maxDiceCharge = 300f;
    public float currentDiceCharge = 0f;
    public float dicePassiveChargeRate = 5f;
    public float diceHitChargeAmount = 2f;
    public float finalDicePower = 1f;

    [Header("Attack Power Stats")]
    public float meleeAttackPower = 10f;
    public float rangeAttackPower = 8f;
    public float finalAttackPower = 1f;

    [Header("Critical Stats")]
    [Range(0f, 1f)] public float criticalChance = 0.2f;
    public float criticalDamageMultiplier = 1.5f;

    [Header("Movement Stats")]
    public float moveSpeed = 5f;

    [Header("Attack Speed Stats")]
    public float attackSpeed = 1.0f;

    [Header("Dash Stats")]
    public int maxDashStacks = 3;
    public float currentDashStacks = 3f;
    public float dashRechargeRate = 1f;

    [Header("Damage Variance")]
    [Range(0f, 0.5f)] public float meleeDamageVariance = 0.2f;
    [Range(0f, 0.5f)] public float rangedDamageVariance = 0.1f;

    [Header("Dice Runtime Multipliers")]
    public float diceDamageMultiplier = 1.0f;
    public float diceMoveSpeedMultiplier = 1.0f;
    public float diceAttackSpeedMultiplier = 1.0f;
    public float diceChargeSpeedMultiplier = 1.0f;
    public float diceCritDamageBonus = 0f;
    public float diceRangedDamageMultiplier = 1.0f;
    public int diceStrongAttackStacks = 0;

    private float ammoRechargeTimer = 0f;

    private void Start()
    {
        currentHealth = maxHealth;
        currentAmmo = maxAmmo;
        currentDiceCharge = 0f;
        currentDashStacks = maxDashStacks;
    }
    
    // 아티팩트(영구 스탯) 획득 시 스탯에 반영하는 함수
    public void ApplyArtifactStat(ArtifactData data)
    {
        if (data.type != ArtifactType.Stat) return;             // 영구 스탯 타입만 처리

        // 1. 첫 번째 효과 적용
        ProcessSingleStat(data.effectType, data.value, data.isPercent, data.artifactName);

        // 2. 두 번째 효과가 있다면 적용
        if (data.effectType2 != ArtifactEffectType.None)
        {
            ProcessSingleStat(data.effectType2, data.value2, data.isPercent2, data.artifactName);
        }
    }
    
    private void ProcessSingleStat(ArtifactEffectType type, float value, bool isPercent, string name)
    {
        float multiplier = 1f + value;                          // 복리(%) 연산용 값
        float flatAmount = value;                               // 고정(합) 연산용 값

        switch (type)
        {
            case ArtifactEffectType.MaxHp:
                int hpBonus = Mathf.RoundToInt(flatAmount);
                maxHealth += hpBonus;
                currentHealth += hpBonus;                       // 늘어난 최대치만큼 현재 체력도 회복
                break;

            case ArtifactEffectType.AttackPower:
                if (isPercent)
                {
                    meleeAttackPower *= multiplier;
                    rangeAttackPower *= multiplier;
                }
                else
                {
                    meleeAttackPower += flatAmount;
                    rangeAttackPower += flatAmount;
                }
                break;

            case ArtifactEffectType.MoveSpeed:
                moveSpeed = isPercent ? moveSpeed * multiplier : moveSpeed + flatAmount;
                break;

            case ArtifactEffectType.AttackSpeed:
                attackSpeed = isPercent ? attackSpeed * multiplier : attackSpeed + flatAmount;
                break;

            case ArtifactEffectType.ChargeSpeed:
                ammoRechargeRate = isPercent ? ammoRechargeRate * multiplier : ammoRechargeRate + flatAmount;
                break;

            case ArtifactEffectType.DiceSpeed:
                dicePassiveChargeRate = isPercent ? dicePassiveChargeRate * multiplier : dicePassiveChargeRate + flatAmount;
                break;

            case ArtifactEffectType.CritChance:
                criticalChance += flatAmount;                   // 크리티컬 확률은 기본적으로 합연산
                break;

            case ArtifactEffectType.CritDamage:
                criticalDamageMultiplier += flatAmount;         // 크리티컬 배율은 기본적으로 합연산
                break;

            case ArtifactEffectType.ScrapGain:
                if (GameManager.instance)
                {
                    GameManager.instance.scrapPercentage += flatAmount;
                }
                break;
        }

        Debug.Log($"아티팩트 스탯 적용 완료: {name} / {type}");
    }

    private void Update()
    {
        UpdateDiceCharge();
        UpdateAmmoRecharge();
    }

    private void UpdateDiceCharge()
    {
        if (currentDiceCharge >= maxDiceCharge) return;

        currentDiceCharge += dicePassiveChargeRate * diceChargeSpeedMultiplier * Time.deltaTime;
        currentDiceCharge = Mathf.Clamp(currentDiceCharge, 0f, maxDiceCharge);
    }

    private void UpdateAmmoRecharge()
    {
        if (currentAmmo >= maxAmmo) return;

        ammoRechargeTimer += ammoRechargeRate * Time.deltaTime;

        if (ammoRechargeTimer >= 1f)
        {
            int amountToRecover = Mathf.FloorToInt(ammoRechargeTimer);
            ammoRechargeTimer -= amountToRecover;
            currentAmmo = Mathf.Min(currentAmmo + amountToRecover, maxAmmo);
        }
    }

    public void ResetDiceRuntimeStats()
    {
        diceDamageMultiplier = 1.0f;
        diceMoveSpeedMultiplier = 1.0f;
        diceAttackSpeedMultiplier = 1.0f;
        diceChargeSpeedMultiplier = 1.0f;
        diceCritDamageBonus = 0f;
        diceRangedDamageMultiplier = 1.0f;
        diceStrongAttackStacks = 0;
    }

    public float GetFinalCriticalDamageMultiplier()
    {
        return criticalDamageMultiplier + diceCritDamageBonus;
    }

    public void AddDiceCharge(float amount)
    {
        currentDiceCharge += amount;
        currentDiceCharge = Mathf.Clamp(currentDiceCharge, 0f, maxDiceCharge);
    }

    public void AddDiceChargeFromHit()
    {
        AddDiceCharge(diceHitChargeAmount * diceChargeSpeedMultiplier);
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"Player health reduced: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("GAME OVER");

        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            GameManager.instance.player.OnDie();
        }
    }
}