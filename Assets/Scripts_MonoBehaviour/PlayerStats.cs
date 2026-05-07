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

    public float buffFinalDamageMultiplier = 1.0f; 

    private float ammoRechargeTimer = 0f;
    private BuffManager _buffManager;

    private void Start()
    {
        _buffManager = GetComponent<BuffManager>();
        currentHealth = maxHealth;
        currentAmmo = maxAmmo;
        currentDiceCharge = 0f;
        currentDashStacks = maxDashStacks;
    }

    public void ApplyArtifactStat(ArtifactData data)
    {
        if (data.type != ArtifactType.Stat) return;             

        ProcessSingleStat(data.effectType, data.value, data.isPercent, data.artifactName);

        if (data.effectType2 != ArtifactEffectType.None)
        {
            ProcessSingleStat(data.effectType2, data.value2, data.isPercent2, data.artifactName);
        }
    }

    public void ApplyModuleReward(ModuleData data)
    {
        if (data == null) return;
        ProcessSingleStat(data.effectType, data.valueAmount, data.isPercent, data.titleText);
    }

    private void ProcessSingleStat(ArtifactEffectType type, float value, bool isPercent, string name)
    {
        float multiplier = 1f + value;                          
        float flatAmount = value;                               

        switch (type)
        {
            case ArtifactEffectType.MaxHp:
                int hpBonus = Mathf.RoundToInt(flatAmount);
                maxHealth += hpBonus;
                currentHealth += hpBonus;                       
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
                criticalChance += flatAmount;                   
                break;

            case ArtifactEffectType.CritDamage:
                criticalDamageMultiplier += flatAmount;         
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

    public float GetFinalMeleeDamage()
    {
        return meleeAttackPower * diceDamageMultiplier * (finalAttackPower * buffFinalDamageMultiplier);
    }

    public float GetFinalRangedDamage()
    {
        return rangeAttackPower * diceDamageMultiplier * diceRangedDamageMultiplier * (finalAttackPower * buffFinalDamageMultiplier);
    }

    public float GetFinalMoveSpeed() => moveSpeed * diceMoveSpeedMultiplier;
    public float GetFinalAttackSpeed() => attackSpeed * diceAttackSpeedMultiplier;
    public float GetFinalChargeSpeed() => ammoRechargeRate * diceChargeSpeedMultiplier;
    public float GetFinalDiceChargeRate() => dicePassiveChargeRate * diceChargeSpeedMultiplier;


    public void ResetDiceRuntimeStats()
    {
        diceDamageMultiplier = 1.0f;
        diceMoveSpeedMultiplier = 1.0f;
        diceAttackSpeedMultiplier = 1.0f;
        diceChargeSpeedMultiplier = 1.0f;
        diceCritDamageBonus = 0f;
        diceRangedDamageMultiplier = 1.0f;
        diceStrongAttackStacks = 0;
    
        buffFinalDamageMultiplier = 1.0f;                       
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

    // ★ [핵심 변경] 데미지를 받기 전에 무효화 버프를 확인합니다.
    public void TakeDamage(int amount)
    {
        if (_buffManager != null)
        {
            // 1. 방어막(첫 피해 무효화)가 있는지 확인하고 소모합니다.
            if (_buffManager.HasAndConsumeFirstHitImmunity())
            {
                Debug.Log("첫 번째 피해 무효화 발동! (유리조각 버프 유지)");
                return; // 여기서 실행을 즉시 멈춰 체력을 깎지 않고 유리대포 해제도 건너뜁니다!
            }

            // 2. 방어막이 없었다면 정상적으로 유리대포(Glass Cannon)가 깨집니다.
            _buffManager.RemoveGlassCannonBuff();
        }

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
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            GameManager.instance.player.OnDie();
        }
    }
}