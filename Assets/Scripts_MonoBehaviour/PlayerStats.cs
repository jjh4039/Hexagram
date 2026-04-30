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
    
        // 신규 추가: 버프로 인한 최종 대미지 증가 배율 (기본 1)
        public float buffFinalDamageMultiplier = 1.0f; 

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

    // ★ [신규 추가됨] 스테이지 클리어 보상(모듈) 획득 시 스탯에 반영하는 함수
    public void ApplyModuleReward(ModuleData data)
    {
        if (data == null) return;

        // 기존 아티팩트 계산 로직을 100% 재활용하여 영구 스탯 상승 처리
        ProcessSingleStat(data.effectType, data.valueAmount, data.isPercent, data.titleText);
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

        // --- 스탯 산출용 Get 함수들 ---
    
        public float GetFinalMeleeDamage()
        {
            // 근거리 공격력 * (버프 데미지 증가량 합산) * (최종 공격력 계수 * 버프 최종 데미지 증가량)
            return meleeAttackPower * diceDamageMultiplier * (finalAttackPower * buffFinalDamageMultiplier);
        }

        public float GetFinalRangedDamage()
        {
            // 원거리 공격력 * (버프 데미지 증가량 합산) * (원거리 전용 버프 합산) * (최종 공격력 계수 * 버프 최종 데미지 증가량)
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
        
            buffFinalDamageMultiplier = 1.0f;                       // 신규 런타임 변수 초기화 추가
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
        
            BuffManager buffManager = GetComponent<BuffManager>();
            if (buffManager != null) buffManager.RemoveGlassCannonBuff();

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