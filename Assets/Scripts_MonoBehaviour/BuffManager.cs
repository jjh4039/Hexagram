using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ActiveBuff
{
    public DiceData buffData;   // 버프 원본 데이터
    public float maxTime;       // UI 게이지용 전체 시간
    public float remainingTime; // 현재 남은 시간
    public int stackCount;      // 중첩 횟수
    public int remainingCount;  // 횟수제 버프의 남은 횟수
    public bool instantApplied; // 즉발 효과 적용 여부
}

public class BuffManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Player player;
    [SerializeField] private PlayerStats stats;
    [SerializeField] private WeaponManager weaponManager;

    [Header("Active Buffs")]
    public List<ActiveBuff> activeBuffs = new List<ActiveBuff>(); // 활성화된 버프 리스트

    public Action OnBuffUpdated;                                  // UI 갱신용 이벤트

    private Color _currentDiceColor = Color.white;                // 현재 적용된 주사위 색상

    private void Awake()
    {
        if (player == null) player = GetComponent<Player>();
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (weaponManager == null) weaponManager = GetComponentInChildren<WeaponManager>();
    }

    private void Update()
    {
        // UI 조작 중이 아닐 때만 버프 타이머를 돌립니다 (StateManager 활용)
        if (InputStateManager.Instance != null && InputStateManager.Instance.CurrentInputState == InputState.UI) return;
        
        UpdateBuffTimers();
    }

    private void UpdateBuffTimers()
    {
        bool buffRemoved = false;

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            ActiveBuff buff = activeBuffs[i];

            if (buff.remainingTime > 0f) buff.remainingTime -= Time.deltaTime;

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

        if (activeBuffs.Count > 0 || buffRemoved) OnBuffUpdated?.Invoke();
    }

    public void ApplyDiceBuff(DiceData data)
    {
        if (data == null || stats == null) return;

        float safeDuration = Mathf.Max(data.duration, 0.01f);

        if (data.effectType == DiceEffectType.Heal)
        {
            ActiveBuff healBuff = new ActiveBuff { buffData = data, maxTime = safeDuration, remainingTime = safeDuration, stackCount = 1, instantApplied = false };
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
            existingBuff.maxTime = existingBuff.remainingTime;
            existingBuff.stackCount = nextStackCount;

            if (data.effectType == DiceEffectType.StrongAttackBuff)
                existingBuff.remainingCount += Mathf.Max(1, Mathf.RoundToInt(data.effectValue));
        }
        else
        {
            ActiveBuff newBuff = new ActiveBuff { buffData = data, maxTime = safeDuration, remainingTime = safeDuration, stackCount = 1,
                remainingCount = data.effectType == DiceEffectType.StrongAttackBuff ? Mathf.Max(1, Mathf.RoundToInt(data.effectValue)) : 0 };
            activeBuffs.Add(newBuff);
        }

        RecalculateStats();
        UpdateDiceBuffVisuals();
        OnBuffUpdated?.Invoke();
    }

    private void RemoveBuffAt(int index)
    {
        if (index < 0 || index >= activeBuffs.Count) return;
        activeBuffs.RemoveAt(index);

        RecalculateStats();
        UpdateDiceBuffVisuals();
    }

    public void RecalculateStats()
    {
        if (stats == null) return;

        stats.ResetDiceRuntimeStats();
        float rangedDiceTotal = 0f;

        foreach (var buff in activeBuffs)
        {
            if (buff == null || buff.buffData == null) continue;
            float finalEffectValue = buff.buffData.effectValue * buff.stackCount;

            switch (buff.buffData.effectType)
            {
                case DiceEffectType.AttackBuff: stats.diceDamageMultiplier += finalEffectValue / 100f; break;
                case DiceEffectType.CritDamageBuff: stats.diceCritDamageBonus += finalEffectValue / 100f; break;
                case DiceEffectType.SpeedBuff: 
                    stats.diceMoveSpeedMultiplier += finalEffectValue / 100f; 
                    stats.diceAttackSpeedMultiplier += finalEffectValue / 100f; break;
                case DiceEffectType.RangedMegaBuff: rangedDiceTotal += finalEffectValue; break;
                case DiceEffectType.StrongAttackBuff: stats.diceStrongAttackStacks += buff.remainingCount; break;
                case DiceEffectType.Heal:
                    if (!buff.instantApplied)
                    {
                        stats.currentHealth = Mathf.Min(stats.currentHealth + Mathf.RoundToInt(buff.buffData.effectValue), stats.maxHealth);
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
            if (activeBuffs[i]?.buffData == null || activeBuffs[i].buffData.effectType == DiceEffectType.Heal) continue;
            latestVisualBuff = activeBuffs[i].buffData;
            break;
        }

        if (latestVisualBuff != null)
        {
            _currentDiceColor = latestVisualBuff.particleColor;
            if (weaponManager != null) weaponManager.UpdateWeaponVisuals(_currentDiceColor, latestVisualBuff.muzzleFlashMaterial);
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
            if (buff?.buffData == null || buff.buffData.effectType != DiceEffectType.StrongAttackBuff || buff.remainingCount <= 0) continue;

            buff.remainingCount--;
            strongAttackMultiplier = buff.buffData.secondaryValue > 1f ? buff.buffData.secondaryValue : (player != null ? player.defaultStrongAttackMultiplier : 2f);

            RecalculateStats();
            if (buff.remainingCount <= 0) RemoveBuffAt(i);

            OnBuffUpdated?.Invoke();
            return true;
        }
        return false;
    }

    public Color GetCurrentDiceColor() => _currentDiceColor;
}