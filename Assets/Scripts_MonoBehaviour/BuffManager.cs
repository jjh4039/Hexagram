using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ActiveBuff
{
    public DiceData buffData;       // 주사위 버프 데이터
    public ArtifactData artifactData; // 아티팩트 버프 데이터 (Trigger형)
    public float maxTime;           // UI 게이지용 전체 시간
    public float remainingTime;     // 현재 남은 시간
    public int stackCount;          // 중첩 횟수
    public int remainingCount;      // 횟수제 버프의 남은 횟수
    public bool instantApplied;     // 즉발 효과 적용 여부
    public bool isInfinite;         // 무한 버프 여부 플래그
}

public class BuffManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Player player;
    [SerializeField] private PlayerStats stats;
    [SerializeField] private WeaponManager weaponManager;

    [Header("Active Buffs")]
    public List<ActiveBuff> activeBuffs = new List<ActiveBuff>();

    [Header("Visual Feedback")]
    [SerializeField] private GameObject floatingIconPrefab;     // FloatingIcon 스크립트가 붙은 프리팹
    [SerializeField] private Transform iconSpawnPoint;          // 플레이어 머리 위 빈 오브젝트의 위치
    [SerializeField] private float iconSpawnInterval = 0.3f;    // 다중 발동 시 아이콘이 뜨는 간격

    public Action OnBuffUpdated;

    private Color _currentDiceColor = Color.white;
    
    // 아이콘 순차 재생을 위한 대기열
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
        if (InputStateManager.Instance != null && InputStateManager.Instance.CurrentInputState == InputState.UI) return;
        
        UpdateBuffTimers();
    }

    public void ApplyArtifactBuff(ArtifactData data)
    {
        if (data == null || stats == null) return;

        // 아이콘 순차 재생 대기열 등록
        if (floatingIconPrefab != null && iconSpawnPoint != null && data.icon != null)
        {
            _iconQueue.Enqueue(data.icon);
            if (!_isSpawningIcon)
            {
                StartCoroutine(Co_SpawnIconRoutine());
            }
        }

        float duration = data.buffDuration;
        bool infinite = data.isInfiniteBuff;

        ActiveBuff existingBuff = activeBuffs.Find(b => b.artifactData != null && b.artifactData == data);

        if (existingBuff != null)
        {
            if (infinite) return; // 무한 버프는 이미 존재하면 중첩 시간 갱신을 생략

            existingBuff.remainingTime = duration;
            existingBuff.maxTime = duration;
            existingBuff.stackCount++;
        }
        else
        {
            ActiveBuff newBuff = new ActiveBuff 
            { 
                artifactData = data, 
                maxTime = duration, 
                remainingTime = duration, 
                stackCount = 1,
                isInfinite = infinite // 생성 시 무한 플래그 설정
            };
            activeBuffs.Add(newBuff);
        }

        RecalculateStats();
        UpdateDiceBuffVisuals();
        OnBuffUpdated?.Invoke();
    }

    // 아이콘을 순차적으로 띄워주는 코루틴 (타임스케일 무관하게 작동)
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

            // 무한 버프가 아닐 때만 시간 감소
            if (!buff.isInfinite && buff.remainingTime > 0f)
            {
                buff.remainingTime -= Time.deltaTime;
            }

            bool expiredByTime = !buff.isInfinite && buff.remainingTime <= 0f;
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

    // 피격 시 유리창 버프(DamageGlassCannon) 해제
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

        if (removed)
        {
            Debug.Log("피격 발생: 무손상 버프(유리창)가 해제되었습니다.");
            OnBuffUpdated?.Invoke(); // UI 즉시 갱신
        }
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
            if (buff == null) continue;

            if (buff.buffData != null)
            {
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

            if (buff.artifactData != null)
            {
                float value1 = buff.artifactData.isPercent ? buff.artifactData.value : (buff.artifactData.value / 100f);
                ApplyArtifactStatAdditive(buff.artifactData.effectType, value1 * buff.stackCount);

                if (buff.artifactData.effectType2 != ArtifactEffectType.None)
                {
                    float value2 = buff.artifactData.isPercent2 ? buff.artifactData.value2 : (buff.artifactData.value2 / 100f);
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