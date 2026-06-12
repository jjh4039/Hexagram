using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StageDebuffType
{
    None,
        DiceEffectHalf,
    TakeMoreDamage,
    CannotHeal
}

[System.Serializable]
public class ActiveBuff
{
    public DiceData buffData;
    public ArtifactData artifactData;

    public StageDebuffType debuffType;
    public float debuffValue;
    public Sprite debuffIcon;

    public float maxTime;
    public float remainingTime;
    public int stackCount;
    public int remainingCount;

    public int remainingStages;
    public bool isStageDuration;
    public bool isDebuff;

    public bool instantApplied;
    public bool isInfinite;
}

public class BuffManager : MonoBehaviour
{
    [Header("Dependencies")] [SerializeField]
    private Player player;

    [SerializeField] private PlayerStats stats;
    [SerializeField] private WeaponManager weaponManager;

    [Header("Active Buffs")] public List<ActiveBuff> activeBuffs = new List<ActiveBuff>();

    [Header("Visual Feedback")] [SerializeField]
    private GameObject floatingIconPrefab;

    [SerializeField] private Transform iconSpawnPoint;
    [SerializeField] private float iconSpawnInterval = 0.3f;

    [Header("Debuff Settings")] public Sprite unifiedDebuffIcon;

    public Action OnBuffUpdated;

    private Queue<Sprite> _iconQueue = new Queue<Sprite>();
    private bool _isSpawningIcon = false;

    private void Awake()
    {
        if (player == null) player = GetComponent<Player>();
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (weaponManager == null) weaponManager = GetComponentInChildren<WeaponManager>();
    }

    private void Update()
    {
        if (InputStateManager.Instance && InputStateManager.Instance.CurrentInputState == InputState.UI) return;
        UpdateBuffTimers();
    }

    public void ApplyStageDebuff(StageDebuffType type, float value, int stageDuration, Sprite icon = null)
    {
        if (stageDuration <= 0)
        {
            Debug.Log($"[디버프 무시] {type}의 지속 스테이지가 0이므로 생성하지 않습니다.");
            return;
        }

        if (type == StageDebuffType.TakeMoreDamage && value <= 0f)
        {
            Debug.Log($"[디버프 무시] {type}의 수치가 0%이므로 생성하지 않습니다.");
            return;
        }

        ActiveBuff existingDebuff = activeBuffs.Find(b => b.isStageDuration && b.isDebuff && b.debuffType == type);

        if (existingDebuff != null)
        {
            existingDebuff.remainingStages += stageDuration;
            existingDebuff.debuffValue = Mathf.Max(existingDebuff.debuffValue, value);

            Debug.Log($"디버프 중첩됨: {type}, 총 {existingDebuff.remainingStages}스테이지 지속으로 증가");
        }
        else
        {
            ActiveBuff newDebuff = new ActiveBuff
            {
                debuffType = type,
                debuffValue = value,
                debuffIcon = unifiedDebuffIcon != null ? unifiedDebuffIcon : icon,
                remainingStages = stageDuration,
                isStageDuration = true,
                isDebuff = true,
                stackCount = 1
            };

            activeBuffs.Add(newDebuff);
            Debug.Log($"디버프 적용됨: {type}, {stageDuration}스테이지 지속");
        }

        RecalculateStats();
        OnBuffUpdated?.Invoke();
    }

    public void OnStageCleared()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i].isStageDuration)
            {
                activeBuffs[i].remainingStages--;

                if (activeBuffs[i].remainingStages <= 0)
                {
                    RemoveBuffAt(i);
                }
            }
        }

        OnBuffUpdated?.Invoke();
    }

    public void ApplyArtifactBuff(ArtifactData data)
    {
        if (data == null || stats == null) return;

        if (floatingIconPrefab != null && iconSpawnPoint != null && data.icon != null)
        {
            _iconQueue.Enqueue(data.icon);
            if (!_isSpawningIcon) StartCoroutine(Co_SpawnIconRoutine());
        }

        float duration = data.buffDuration;
        bool infinite = data.isInfiniteBuff;

        ActiveBuff existingBuff = activeBuffs.Find(b => b.artifactData != null && b.artifactData == data);

        if (existingBuff != null)
        {
            if (infinite) return;

            existingBuff.remainingTime = duration;
            existingBuff.maxTime = duration;
            existingBuff.stackCount++;
        }
        else
        {
            float initTime = infinite ? 1f : duration;

            ActiveBuff newBuff = new ActiveBuff
            {
                artifactData = data,
                maxTime = initTime,
                remainingTime = initTime,
                stackCount = 1,
                isInfinite = infinite
            };
            activeBuffs.Add(newBuff);
        }

        RecalculateStats();
        OnBuffUpdated?.Invoke();
    }

    private IEnumerator Co_SpawnIconRoutine()
    {
        _isSpawningIcon = true;
        while (_iconQueue.Count > 0)
        {
            Sprite icon = _iconQueue.Dequeue();
            GameObject iconObj = Instantiate(floatingIconPrefab, iconSpawnPoint.position, Quaternion.identity);
            FloatingIcon floatingIcon = iconObj.GetComponent<FloatingIcon>();
            if (floatingIcon != null) floatingIcon.Setup(icon);
            yield return new WaitForSecondsRealtime(iconSpawnInterval);
        }

        _isSpawningIcon = false;
    }

    private void UpdateBuffTimers()
    {
        bool buffRemoved = false;
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            ActiveBuff buff = activeBuffs[i];

            if (!buff.isInfinite && !buff.isStageDuration && buff.remainingTime > 0f)
                buff.remainingTime -= Time.deltaTime;

            bool expiredByTime = !buff.isInfinite && !buff.isStageDuration && buff.remainingTime <= 0f;
            bool expiredByCount = buff.buffData != null &&
                                  buff.buffData.effectType == DiceEffectType.StrongAttackBuff &&
                                  buff.remainingCount <= 0;

            if (expiredByTime || expiredByCount)
            {
                RemoveBuffAt(i);
                buffRemoved = true;
            }
        }

        if (activeBuffs.Count > 0 || buffRemoved) OnBuffUpdated?.Invoke();
    }

    public bool HasAndConsumeFirstHitImmunity()
    {
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i].artifactData != null)
            {
                if (activeBuffs[i].artifactData.effectType == ArtifactEffectType.DefenseFirstHit ||
                    activeBuffs[i].artifactData.effectType2 == ArtifactEffectType.DefenseFirstHit)
                {
                    RemoveBuffAt(i);
                    OnBuffUpdated?.Invoke();
                    return true;
                }
            }
        }

        return false;
    }

    public void RemoveGlassCannonBuff()
    {
        bool removed = false;
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i].artifactData != null)
            {
                if (activeBuffs[i].artifactData.effectType == ArtifactEffectType.DamageGlassCannon ||
                    activeBuffs[i].artifactData.effectType2 == ArtifactEffectType.DamageGlassCannon)
                {
                    RemoveBuffAt(i);
                    removed = true;
                }
            }
        }

        if (removed) OnBuffUpdated?.Invoke();
    }

    public void ApplyDiceBuff(DiceData data)
    {
        if (data == null || stats == null) return;
        float safeDuration = Mathf.Max(data.duration, 0.01f);

        if (data.effectType == DiceEffectType.Heal)
        {
            ActiveBuff healBuff = new ActiveBuff
            {
                buffData = data, maxTime = safeDuration, remainingTime = safeDuration, stackCount = 1,
                instantApplied = false
            };
            activeBuffs.Add(healBuff);
            RecalculateStats();
            OnBuffUpdated?.Invoke();
            return;
        }

        ActiveBuff existingBuff = activeBuffs.Find(b => b.buffData != null && b.buffData.effectType == data.effectType);
        if (existingBuff != null)
        {
            int nextStackCount = existingBuff.stackCount + 1;
            existingBuff.remainingTime = ((existingBuff.remainingTime * existingBuff.stackCount) + safeDuration) /
                                         nextStackCount;
            existingBuff.maxTime = existingBuff.remainingTime;
            existingBuff.stackCount = nextStackCount;
            if (data.effectType == DiceEffectType.StrongAttackBuff)
                existingBuff.remainingCount += Mathf.Max(1, Mathf.RoundToInt(data.effectValue));
        }
        else
        {
            ActiveBuff newBuff = new ActiveBuff
            {
                buffData = data,
                maxTime = safeDuration,
                remainingTime = safeDuration,
                stackCount = 1,
                remainingCount = data.effectType == DiceEffectType.StrongAttackBuff
                    ? Mathf.Max(1, Mathf.RoundToInt(data.effectValue))
                    : 0
            };
            activeBuffs.Add(newBuff);
        }

        RecalculateStats();
        OnBuffUpdated?.Invoke();
    }

    private void RemoveBuffAt(int index)
    {
        if (index < 0 || index >= activeBuffs.Count) return;
        activeBuffs.RemoveAt(index);
        RecalculateStats();
    }

    public void RecalculateStats()
    {
        if (stats == null) return;
        stats.ResetDiceRuntimeStats();

        foreach (var buff in activeBuffs)
        {
            if (buff.isStageDuration && buff.isDebuff)
            {
                switch (buff.debuffType)
                {
                    case StageDebuffType.DiceEffectHalf:
                        stats.isDiceEffectHalved = true;
                        break;
                    case StageDebuffType.TakeMoreDamage:
                        stats.takeMoreDamageMultiplier += (buff.debuffValue / 100f);
                        break;
                    case StageDebuffType.CannotHeal:
                        stats.cannotHeal = true;
                        break;
                }
            }
        }

        float rangedDiceTotal = 0f;
        foreach (var buff in activeBuffs)
        {
            if (buff == null) continue;

            if (buff.buffData != null)
            {
                float finalEffectValue = buff.buffData.effectValue * buff.stackCount;

                if (stats.isDiceEffectHalved)
                    finalEffectValue *= 0.5f;

                switch (buff.buffData.effectType)
                {
                    case DiceEffectType.AttackBuff: stats.diceDamageMultiplier += finalEffectValue / 100f; break;
                    case DiceEffectType.CritDamageBuff: 
                        stats.diceCritDamageBonus += finalEffectValue / 100f; 
                        
                        // 치명타 확률 추가
                        float finalSecondaryValue = buff.buffData.secondaryValue * buff.stackCount;
                        if (stats.isDiceEffectHalved) finalSecondaryValue *= 0.5f;
                        stats.diceCritChanceBonus += finalSecondaryValue / 100f;
                        break;
                    case DiceEffectType.SpeedBuff:
                        stats.diceMoveSpeedMultiplier += finalEffectValue / 100f;
                        stats.diceAttackSpeedMultiplier += finalEffectValue / 100f;
                        break;
                    case DiceEffectType.RangedMegaBuff: rangedDiceTotal += finalEffectValue; break;
                    case DiceEffectType.StrongAttackBuff: stats.diceStrongAttackStacks += buff.remainingCount; break;
                    
                    case DiceEffectType.Heal:
                        if (!buff.instantApplied)
                        {
                            float healAmount = stats.maxHealth * (finalEffectValue / 100f);
                            int finalHealInt = Mathf.Max(1, Mathf.RoundToInt(healAmount)); 

                            stats.Heal(finalHealInt);

                            buff.instantApplied = true;
                        }
                        break;
                }
            }

            if (buff.artifactData != null)
            {
                float value1 = buff.artifactData.isPercent ? buff.artifactData.value : (buff.artifactData.value / 100f);
                ApplyArtifactStatAdditive(buff.artifactData.effectType, value1 * buff.stackCount);
                if (buff.artifactData.effectType2 != ArtifactEffectType.None)
                {
                    float value2 = buff.artifactData.isPercent2
                        ? buff.artifactData.value2
                        : (buff.artifactData.value2 / 100f);
                    ApplyArtifactStatAdditive(buff.artifactData.effectType2, value2 * buff.stackCount);
                }
            }
        }

        stats.diceRangedDamageMultiplier = rangedDiceTotal > 0f ? rangedDiceTotal : 1f;
    }

    private void ApplyArtifactStatAdditive(ArtifactEffectType type, float finalValue)
    {
        switch (type)
        {
            case ArtifactEffectType.AttackPower:
            case ArtifactEffectType.DamageGlassCannon:
                stats.diceDamageMultiplier += finalValue; break;
            case ArtifactEffectType.MoveSpeed: stats.diceMoveSpeedMultiplier += finalValue; break;
            case ArtifactEffectType.AttackSpeed: stats.diceAttackSpeedMultiplier += finalValue; break;
            case ArtifactEffectType.ChargeSpeed: stats.diceChargeSpeedMultiplier += finalValue; break;
            case ArtifactEffectType.CritDamage: stats.diceCritDamageBonus += finalValue; break;
            case ArtifactEffectType.FinalDamage: stats.buffFinalDamageMultiplier += finalValue; break;
        }
    }

    public bool TryConsumeStrongAttack(out float strongAttackMultiplier)
    {
        strongAttackMultiplier = 1f;
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            ActiveBuff buff = activeBuffs[i];
            if (buff?.buffData == null || buff.buffData.effectType != DiceEffectType.StrongAttackBuff ||
                buff.remainingCount <= 0) continue;
            buff.remainingCount--;
            strongAttackMultiplier = buff.buffData.secondaryValue > 1f
                ? buff.buffData.secondaryValue
                : (player != null ? player.defaultStrongAttackMultiplier : 2f);
            RecalculateStats();
            if (buff.remainingCount <= 0) RemoveBuffAt(i);
            OnBuffUpdated?.Invoke();
            return true;
        }

        return false;
    }
}