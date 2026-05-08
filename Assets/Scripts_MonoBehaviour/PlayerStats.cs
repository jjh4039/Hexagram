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

    [Header("Visual Feedback")]
    public GameObject damageTextPrefab;                                 // 플레이어 피격 시 띄울 텍스트 프리팹

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
        float multiplier = 1f + value;                                  // 복리(%) 연산용 값
        float flatAmount = value;                                       // 고정(합) 연산용 값

        switch (type)
        {
            case ArtifactEffectType.MaxHp:
                if (isPercent)
                {
                    ApplyPercentMaxHealth(value);
                }
                else
                {
                    int hpBonus = Mathf.RoundToInt(flatAmount);
                    maxHealth += hpBonus;
                    currentHealth += hpBonus;                           // 늘어난 최대치만큼 현재 체력도 회복
                }
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
                attackSpeed += value;                                   // 기존 곱연산에서 합연산으로 수정
                break;

            case ArtifactEffectType.ChargeSpeed:
                ammoRechargeRate = isPercent ? ammoRechargeRate * multiplier : ammoRechargeRate + flatAmount;
                break;

            case ArtifactEffectType.DiceSpeed:
                dicePassiveChargeRate = isPercent ? dicePassiveChargeRate * multiplier : dicePassiveChargeRate + flatAmount;
                break;

            case ArtifactEffectType.CritChance:
                criticalChance += flatAmount;                           // 크리티컬 확률은 기본적으로 합연산
                break;

            case ArtifactEffectType.CritDamage:
                criticalDamageMultiplier += flatAmount;                 // 크리티컬 배율은 기본적으로 합연산
                break;

            case ArtifactEffectType.ScrapGain:
                if (GameManager.instance)
                {
                    GameManager.instance.scrapPercentage += flatAmount;
                }
                break;
        }
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

    private void ApplyPercentMaxHealth(float percentValue)
    {
        int hpBonus = Mathf.RoundToInt(maxHealth * percentValue);
        maxHealth += hpBonus;
        currentHealth += hpBonus;                                       // 퍼센트 증가량만큼 현재 체력도 회복
    }

    public void AddDiceChargeFromHit()
    {
        AddDiceCharge(diceHitChargeAmount * diceChargeSpeedMultiplier);
    }

    public void TakeDamage(int amount)
    {
        if (_buffManager != null)
        {
            if (_buffManager.HasAndConsumeFirstHitImmunity())
            {
                SpawnDamageText("GUARD!", Color.cyan, 3f);                  // 무효화 텍스트 사이즈 3 고정
                return;
            }

            _buffManager.RemoveGlassCannonBuff();
        }

        currentHealth -= amount;
        SpawnDamageText(amount.ToString(), Color.red, 3f);                  // 피격 데미지 텍스트 사이즈 3 고정

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void SpawnDamageText(string message, Color color, float size)
    {
        if (damageTextPrefab == null) return;

        GameObject textObj = Instantiate(damageTextPrefab, transform.position + (Vector3.up * 0.5f), Quaternion.identity);
        DamageText dmgText = textObj.GetComponent<DamageText>();

        if (dmgText != null)
        {
            dmgText.Setup(message, color, size);
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