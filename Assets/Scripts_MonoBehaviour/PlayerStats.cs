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
    public float dicePassiveChargeRate = 25f;
    public float diceHitChargeAmount = 2f;
    public float finalDicePower = 0f;

    [Header("Attack Power Stats")]
    public float meleeAttackPower = 10f;
    public float rangeAttackPower = 8f;
    public float finalAttackPower = 0f;

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