using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ActiveBuff
{
    public DiceData buffData;
    public float maxTime;       // UI 게이지용 (전체 시간)
    public float remainingTime; // 남은 시간
    public int stackCount;
    public int remainingCount;
    public bool instantApplied;
}

public class BuffManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Player player;
    [SerializeField] private PlayerStats stats;
    [SerializeField] private WeaponManager weaponManager;

    [Header("Active Buffs")]
    public List<ActiveBuff> activeBuffs = new List<ActiveBuff>();

    // UI 업데이트를 위한 이벤트
    public Action OnBuffUpdated;

    private Color _currentDiceColor = Color.white;

    private void Awake()
    {
        if (player == null) player = GetComponent<Player>();
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (weaponManager == null) weaponManager = GetComponentInChildren<WeaponManager>(); // 계층 구조에 맞게 수정 가능
    }

    private void Update()
    {
        if (player != null && !player.canControl) return;
        UpdateBuffTimers();
    }

    private void UpdateBuffTimers()
    {
        bool buffRemoved = false;

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            ActiveBuff buff = activeBuffs[i];

            if (buff.remainingTime > 0f)
            {
                buff.remainingTime -= Time.deltaTime;
            }

            bool expiredByTime = buff.remainingTime <= 0f;
            bool expiredByCount = buff.buffData != null &&
                                  buff.buffData.effectType == DiceEffectType.StrongAttackBuff &&
                                  buff.remainingCount <= 0;

            if (expiredByTime || expiredByCount)
            {
                RemoveBuffAt(i);
                buffRemoved = true;
            }
        }

        // 활성화된 버프가 있거나 버프가 방금 제거된 경우에만 UI 업데이트 호출
        if (activeBuffs.Count > 0 || buffRemoved)
        {
            OnBuffUpdated?.Invoke();
        }
    }

    public void ApplyDiceBuff(DiceData data)
    {
        if (data == null || stats == null) return;

        float safeDuration = Mathf.Max(data.duration, 0.01f);

        if (data.effectType == DiceEffectType.Heal)
        {
            ActiveBuff healBuff = new ActiveBuff
            {
                buffData = data,
                maxTime = safeDuration,
                remainingTime = safeDuration,
                stackCount = 1,
                remainingCount = 0,
                instantApplied = false
            };

            activeBuffs.Add(healBuff);
            RecalculateStats();
            UpdateDiceBuffVisuals();
            OnBuffUpdated?.Invoke();
            return;
        }

        ActiveBuff existingBuff = activeBuffs.Find(b => b.buffData != null && b.buffData.effectType == data.effectType);

        if (existingBuff != null)
        {
            int nextStackCount = existingBuff.stackCount + 1;
            existingBuff.remainingTime = ((existingBuff.remainingTime * existingBuff.stackCount) + safeDuration) / nextStackCount;
            existingBuff.maxTime = existingBuff.remainingTime; // 최대 시간 갱신
            existingBuff.stackCount = nextStackCount;

            if (data.effectType == DiceEffectType.StrongAttackBuff)
            {
                existingBuff.remainingCount += Mathf.Max(1, Mathf.RoundToInt(data.effectValue));
            }
            Debug.Log($"[Buff Stacked] {data.diceName} / Stack: {existingBuff.stackCount} / Time: {existingBuff.remainingTime:F2}");
        }
        else
        {
            ActiveBuff newBuff = new ActiveBuff
            {
                buffData = data,
                maxTime = safeDuration,
                remainingTime = safeDuration,
                stackCount = 1,
                remainingCount = data.effectType == DiceEffectType.StrongAttackBuff ? Mathf.Max(1, Mathf.RoundToInt(data.effectValue)) : 0,
                instantApplied = false
            };

            activeBuffs.Add(newBuff);
            Debug.Log($"[Buff Added] {data.diceName} / Time: {safeDuration:F2}");
        }

        RecalculateStats();
        UpdateDiceBuffVisuals();
        OnBuffUpdated?.Invoke();
    }

    private void RemoveBuffAt(int index)
    {
        if (index < 0 || index >= activeBuffs.Count) return;

        ActiveBuff removedBuff = activeBuffs[index];
        activeBuffs.RemoveAt(index);

        if (removedBuff != null && removedBuff.buffData != null)
        {
            Debug.Log($"[Buff Removed] {removedBuff.buffData.diceName}");
        }

        RecalculateStats();
        UpdateDiceBuffVisuals();
    }

    public void RecalculateStats()
    {
        if (stats == null) return;

        stats.ResetDiceRuntimeStats();
        float rangedDiceTotal = 0f;

        for (int i = 0; i < activeBuffs.Count; i++)
        {
            ActiveBuff buff = activeBuffs[i];
            if (buff == null || buff.buffData == null) continue;

            float finalEffectValue = buff.buffData.effectValue * buff.stackCount;

            switch (buff.buffData.effectType)
            {
                case DiceEffectType.AttackBuff:
                    stats.diceDamageMultiplier += finalEffectValue / 100f;
                    break;
                case DiceEffectType.CritDamageBuff:
                    stats.diceCritDamageBonus += finalEffectValue / 100f;
                    break;
                case DiceEffectType.SpeedBuff:
                    stats.diceMoveSpeedMultiplier += finalEffectValue / 100f;
                    stats.diceAttackSpeedMultiplier += finalEffectValue / 100f;
                    break;
                case DiceEffectType.RangedMegaBuff:
                    rangedDiceTotal += finalEffectValue;
                    break;
                case DiceEffectType.StrongAttackBuff:
                    stats.diceStrongAttackStacks += buff.remainingCount;
                    break;
                case DiceEffectType.Heal:
                    if (!buff.instantApplied)
                    {
                        int healAmount = Mathf.RoundToInt(buff.buffData.effectValue);
                        stats.currentHealth = Mathf.Min(stats.currentHealth + healAmount, stats.maxHealth);
                        buff.instantApplied = true;
                    }
                    break;
            }
        }
        stats.diceRangedDamageMultiplier = rangedDiceTotal > 0f ? rangedDiceTotal : 1f;
    }

    private void UpdateDiceBuffVisuals()
    {
        DiceData latestVisualBuff = null;

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i] == null || activeBuffs[i].buffData == null) continue;
            if (activeBuffs[i].buffData.effectType == DiceEffectType.Heal) continue;

            latestVisualBuff = activeBuffs[i].buffData;
            break;
        }

        if (latestVisualBuff != null)
        {
            _currentDiceColor = latestVisualBuff.particleColor;
            if (weaponManager != null) weaponManager.UpdateWeaponVisuals(latestVisualBuff.particleColor, latestVisualBuff.muzzleFlashMaterial);
        }
        else
        {
            _currentDiceColor = Color.white;
            if (weaponManager != null) weaponManager.UpdateWeaponVisuals(Color.white, null);
        }
    }

    public bool TryConsumeStrongAttack(out float strongAttackMultiplier)
    {
        strongAttackMultiplier = 1f;

        for (int i = 0; i < activeBuffs.Count; i++)
        {
            ActiveBuff buff = activeBuffs[i];
            if (buff == null || buff.buffData == null) continue;
            if (buff.buffData.effectType != DiceEffectType.StrongAttackBuff) continue;
            if (buff.remainingCount <= 0) continue;

            buff.remainingCount -= 1;

            // Player의 defaultStrongAttackMultiplier 값을 가져오거나 고정값 사용
            strongAttackMultiplier = buff.buffData.secondaryValue > 1f ? buff.buffData.secondaryValue : (player != null ? player.defaultStrongAttackMultiplier : 2f);

            RecalculateStats();

            if (buff.remainingCount <= 0)
            {
                RemoveBuffAt(i);
            }

            OnBuffUpdated?.Invoke();
            return true;
        }
        return false;
    }

    public Color GetCurrentDiceColor()
    {
        return _currentDiceColor;
    }
}