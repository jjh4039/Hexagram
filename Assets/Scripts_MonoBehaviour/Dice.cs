using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Dice : MonoBehaviour
{
    [Header("--- Data Settings ---")]
    [SerializeField] public DiceData[] diceList;
    [SerializeField] public DiceData defaultData;

    [Header("--- Charge Economy ---")]
    [SerializeField] private float passiveChargeRate = 5f; // 초당 자동 충전량
    [SerializeField] private float hitChargeAmount = 15f;  // 적 타격 시 충전량

    [Header("--- Skill Cooldowns (소프트 쿨타임) ---")]
    [SerializeField] private float singleRollCooldown = 0.3f;
    [SerializeField] private float allRollCooldown = 3.0f;

    private float lastSingleRollTime = -99f;
    private float lastAllRollTime = -99f;

    private void Update()
    {
        if (GameManager.instance == null || GameManager.instance.stats == null) return;
        PlayerStats stats = GameManager.instance.stats;

        // 1. 패시브 충전 (시간 경과)
        if (stats.currentDiceCharge < stats.maxDiceCharge)
        {
            // chargeSpeedMultiplier(충전 속도 버프) 적용!
            stats.currentDiceCharge += passiveChargeRate * stats.chargeSpeedMultiplier * Time.deltaTime;
            stats.currentDiceCharge = Mathf.Clamp(stats.currentDiceCharge, 0f, stats.maxDiceCharge);
        }

        // 2. 입력 감지 (Q = 단일, E = 전체)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.qKey.wasPressedThisFrame) TrySingleRoll(stats);
            if (Keyboard.current.eKey.wasPressedThisFrame) TryAllRoll(stats);
        }
    }

    // 무기 스크립트에서 적을 때렸을 때 호출할 함수
    public void AddChargeFromHit()
    {
        if (GameManager.instance == null || GameManager.instance.stats == null) return;
        PlayerStats stats = GameManager.instance.stats;

        stats.currentDiceCharge += hitChargeAmount * stats.chargeSpeedMultiplier;
        stats.currentDiceCharge = Mathf.Clamp(stats.currentDiceCharge, 0f, stats.maxDiceCharge);
    }

    private void TrySingleRoll(PlayerStats stats)
    {
        if (stats.currentDiceCharge >= 100f && Time.time >= lastSingleRollTime + singleRollCooldown)
        {
            stats.currentDiceCharge -= 100f;
            lastSingleRollTime = Time.time;

            DiceData resultData = diceList[Random.Range(0, diceList.Length)];
            Debug.Log($"[Q 사용] 뽑힌 주사위: {resultData.diceName}");

            // 실제 플레이어에게 버프 전달
            GameManager.instance.player.ApplyDiceBuff(resultData);
        }
    }

    private void TryAllRoll(PlayerStats stats)
    {
        if (stats.currentDiceCharge >= 300f && Time.time >= lastAllRollTime + allRollCooldown)
        {
            stats.currentDiceCharge -= 300f;
            lastAllRollTime = Time.time;

            List<DiceData> resultDatas = GetUniqueRolls(3);
            Debug.Log($"[E 사용] 뽑힌 주사위: {resultDatas[0].diceName}, {resultDatas[1].diceName}, {resultDatas[2].diceName}");

            // 3개의 버프를 동시에 전달
            foreach (var data in resultDatas)
            {
                GameManager.instance.player.ApplyDiceBuff(data);
            }
        }
    }

    private List<DiceData> GetUniqueRolls(int count)
    {
        List<DiceData> pool = new List<DiceData>(diceList);
        List<DiceData> results = new List<DiceData>();

        for (int i = 0; i < count; i++)
        {
            int randIndex = Random.Range(0, pool.Count);
            results.Add(pool[randIndex]);
            pool.RemoveAt(randIndex);
        }
        return results;
    }
}