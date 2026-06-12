using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Survival Stats")]
    public int maxHealth = 30;
    public int currentHealth;

    [Header("Resource Stats")]
    public int maxAmmo = 500;
    public int currentAmmo = 0;
    public float ammoRechargeRate = 10f;

    [Header("Dice Charge Stats")]
    public float maxDiceCharge = 300f;
    public float currentDiceCharge = 100f;
    public float dicePassiveChargeRate = 5f;
    public float diceHitChargeAmount = 2f;
    public float finalDicePower = 1f;

    [Header("Attack Power Stats")]
    public float meleeAttackPower = 10f;
    public float rangeAttackPower = 14f;
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
    public float diceCritChanceBonus = 0f; 

    public float buffFinalDamageMultiplier = 1.0f;

    [Header("Event Debuff States")]
    public bool cannotHeal = false;
    public float takeMoreDamageMultiplier = 1.0f;
    public bool isDiceEffectHalved = false;

    [Header("Visual Feedback")]
    public GameObject damageTextPrefab;

    [Header("Meta Progression Settings")]
    public int metaHealthPerLevel = 1;            
    public float metaAttackPerLevel = 0.2f;         
    public float metaChargeSpeedPercent = 0.03f;   
    public int metaPierceStep = 3;                 
    public float metaDifficultyPercent = 0.05f;     

    [HideInInspector] public int bonusPenetration = 0;       
    [HideInInspector] public float enemyStatMultiplier = 1f; 
    [HideInInspector] public float metaAmmoGainMultiplier = 1f; 

    private float _ammoRechargeTimer = 0f;
    private BuffManager _buffManager;

    private void Start()
    {
        _buffManager = GetComponent<BuffManager>();

        ApplyMetaProgression();

        currentHealth = maxHealth;
        currentAmmo = maxAmmo;
        currentDashStacks = maxDashStacks;
    }

    private void ApplyMetaProgression()
    {
        if (DataManager.instance == null || DataManager.instance.data == null) return;
        GameData data = DataManager.instance.data;

        maxHealth += data.upgradeHealthLevel * metaHealthPerLevel;

        meleeAttackPower += data.upgradeAttackLevel * metaAttackPerLevel;
        rangeAttackPower += data.upgradeAttackLevel * metaAttackPerLevel;

        float chargeSpeedBonus = 1f + (data.upgradeBulletLevel * metaChargeSpeedPercent);
        ammoRechargeRate *= chargeSpeedBonus;
        metaAmmoGainMultiplier = chargeSpeedBonus; 
        
        bonusPenetration = data.upgradeBulletLevel / metaPierceStep;

        float difficultyBonus = data.difficultyLevel * metaDifficultyPercent;
        enemyStatMultiplier = 1f + difficultyBonus;
        
        if (GameManager.instance != null)
        {
            GameManager.instance.scrapPercentage += difficultyBonus;
        }
    }

    public int GetFinalMeleeAmmoGain(int baseGain)
    {
        return Mathf.RoundToInt(baseGain * metaAmmoGainMultiplier * diceChargeSpeedMultiplier);
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
                if (isPercent) ApplyPercentMaxHealth(value);
                else
                {
                    int hpBonus = Mathf.RoundToInt(flatAmount);
                    maxHealth += hpBonus;
                    if (!cannotHeal) currentHealth += hpBonus;
                }
                break;
            case ArtifactEffectType.AttackPower:
                if (isPercent) { meleeAttackPower *= multiplier; rangeAttackPower *= multiplier; }
                else { meleeAttackPower += flatAmount; rangeAttackPower += flatAmount; }
                break;
            case ArtifactEffectType.FinalDamage:
                if (isPercent) finalAttackPower *= multiplier;
                else finalAttackPower += flatAmount;
                break;
            case ArtifactEffectType.MoveSpeed:
                moveSpeed = isPercent ? moveSpeed * multiplier : moveSpeed + flatAmount;
                break;
            case ArtifactEffectType.AttackSpeed:
                attackSpeed += value;
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
                if (GameManager.instance) GameManager.instance.scrapPercentage += flatAmount;
                break;
        }
    }

    public void ApplyShopStat(ShopStatOptionHoverSystem.ShopStatType type, float percentValue)
    {
        float decimalValue = percentValue / 100f; 

        switch (type)
        {
            case ShopStatOptionHoverSystem.ShopStatType.AttackPower:
                meleeAttackPower *= (1f + decimalValue);
                rangeAttackPower *= (1f + decimalValue);
                break;
            case ShopStatOptionHoverSystem.ShopStatType.AttackSpeed:
                attackSpeed += decimalValue; 
                break;
            case ShopStatOptionHoverSystem.ShopStatType.MoveSpeed:
                moveSpeed *= (1f + decimalValue);
                break;
            case ShopStatOptionHoverSystem.ShopStatType.CritChance:
                criticalChance += decimalValue; 
                break;
            case ShopStatOptionHoverSystem.ShopStatType.CritDamage:
                criticalDamageMultiplier += decimalValue; 
                break;
        }
        SpawnDamageText("STAT UP!", Color.yellow, 4f); 
    }

    private void Update()
    {
        UpdateDiceCharge();
        UpdateAmmoRecharge();
    }

    private void UpdateDiceCharge()
    {
        if (currentDiceCharge >= maxDiceCharge) return;
        
        bool isTutorial = false;
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            isTutorial = GameManager.instance.player.isTutorial;
        }
        
        if (InputStateManager.Instance != null && InputStateManager.Instance.CurrentPhase != GamePhase.InCombat)
        {
            if (!isTutorial) return; 
        }

        currentDiceCharge += dicePassiveChargeRate * diceChargeSpeedMultiplier * Time.deltaTime;
        currentDiceCharge = Mathf.Clamp(currentDiceCharge, 0f, maxDiceCharge);
    }

    private void UpdateAmmoRecharge()
    {
        if (currentAmmo >= maxAmmo) return;

        _ammoRechargeTimer += ammoRechargeRate * Time.deltaTime;

        if (_ammoRechargeTimer >= 1f)
        {
            int amountToRecover = Mathf.FloorToInt(_ammoRechargeTimer);
            _ammoRechargeTimer -= amountToRecover;
            currentAmmo = Mathf.Min(currentAmmo + amountToRecover, maxAmmo);
        }
    }

    public float GetFinalMeleeDamage() => meleeAttackPower * diceDamageMultiplier * (finalAttackPower * buffFinalDamageMultiplier);
    public float GetFinalRangedDamage() => rangeAttackPower * diceDamageMultiplier * diceRangedDamageMultiplier * (finalAttackPower * buffFinalDamageMultiplier);
    public float GetFinalMoveSpeed() => moveSpeed * diceMoveSpeedMultiplier;
    
    public float GetFinalAttackSpeed() 
    {
        float speed = attackSpeed * diceAttackSpeedMultiplier;
        return Mathf.Clamp(speed, 0.1f, 2.0f);
    }
    
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
        diceCritChanceBonus = 0f; // ★ 임시 치명타 확률 초기화

        buffFinalDamageMultiplier = 1.0f;

        cannotHeal = false;
        takeMoreDamageMultiplier = 1.0f;
        isDiceEffectHalved = false;
    }

    public float GetFinalCriticalDamageMultiplier() => criticalDamageMultiplier + diceCritDamageBonus;
    
    // ★ 추가: 최종 치명타 확률 산출 (기본스탯 + 주사위스탯)
    public float GetFinalCriticalChance() => criticalChance + diceCritChanceBonus;

    public void AddDiceCharge(float amount)
    {
        currentDiceCharge += amount;
        currentDiceCharge = Mathf.Clamp(currentDiceCharge, 0f, maxDiceCharge);
    }

    public void ApplyPercentMaxHealth(float percentValue)
    {
        int hpBonus = Mathf.RoundToInt(maxHealth * percentValue);
        maxHealth += hpBonus;

        if (!cannotHeal) currentHealth += hpBonus;
    }

    public void AddDiceChargeFromHit() => AddDiceCharge(diceHitChargeAmount * diceChargeSpeedMultiplier);

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return; 

        if (_buffManager != null)
        {
            if (_buffManager.HasAndConsumeFirstHitImmunity())
            {
                SpawnDamageText("GUARD!", Color.mediumSeaGreen, 3f);
                return;
            }
            _buffManager.RemoveGlassCannonBuff();
        }

        int finalDamage = Mathf.RoundToInt(amount * takeMoreDamageMultiplier);

        currentHealth -= finalDamage;
        SpawnDamageText(finalDamage.ToString(), Color.red, 3f);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (cannotHeal) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        SpawnDamageText($"+{amount}", Color.green, 3f); 
    }
    
    public void SpawnDamageText(string message, Color color, float size)
    {
        if (!damageTextPrefab) return;
        DamageText dmgText = DamageText.Spawn(damageTextPrefab, transform.position + (Vector3.up * 0.5f));
        if (dmgText) dmgText.Setup(message, color, size);
    }

    private void Die()
    {
        if (GameManager.instance && GameManager.instance.player != null)
            GameManager.instance.player.OnDie();
    }
}