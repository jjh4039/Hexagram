using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro;

public class StageController : MonoBehaviour
{
    [Header("--- Stage Type ---")]
    public bool isSafeStage = false;

    [Header("--- Settings ---")]
    public Transform spawnPoint;
    public GameObject barrierEla;
    public Statue statue;

    [Header("--- Runtime Info ---")]
    private List<Enemy> activeEnemies = new List<Enemy>();
    private bool isCleared = false;
    private bool isInitialized = false;

    private Dictionary<int, List<EnemySpawner>> waveSpawners = new Dictionary<int, List<EnemySpawner>>();
    private int currentWave = 1;
    private int pendingSpawns = 0;
    private int totalRemainingEnemies = 0;

    private void Start()
    {
        if (!isInitialized) InitStage();
    }

    public void InitStage()
    {
        if (isInitialized) return;
        isInitialized = true;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && spawnPoint != null) player.transform.position = spawnPoint.position;

        InitializeWaves();

        if (isSafeStage)
        {
            if (barrierEla != null) barrierEla.SetActive(true);
            if (statue != null) statue.ActivateStatue();
            if (StageMessageUI.instance != null) StageMessageUI.instance.HideEnemyCountUI();
            isCleared = true;
        }
        else
        {
            if (barrierEla != null) barrierEla.SetActive(true);
            isCleared = false;
        }

        if (!isSafeStage) UpdateEnemyCountUI();
    }

    private void InitializeWaves()
    {
        EnemySpawner[] spawners = GetComponentsInChildren<EnemySpawner>();
        activeEnemies.Clear();
        waveSpawners.Clear();
        totalRemainingEnemies = spawners.Length;

        foreach (var spawner in spawners)
        {
            if (!waveSpawners.ContainsKey(spawner.waveNumber))
                waveSpawners[spawner.waveNumber] = new List<EnemySpawner>();
            waveSpawners[spawner.waveNumber].Add(spawner);
        }

        if (!isSafeStage && waveSpawners.Count > 0) StartWave(1);
    }

    private void StartWave(int waveIndex)
    {
        if (!waveSpawners.ContainsKey(waveIndex))
        {
            StageClear();
            return;
        }

        currentWave = waveIndex;
        List<EnemySpawner> spawnersToActivate = waveSpawners[waveIndex];
        pendingSpawns = spawnersToActivate.Count;

        // ★ [수정됨] 웨이브 시작 시에는 조용히 UI만 갱신 (인자 없음 = false)
        UpdateEnemyCountUI();

        foreach (var spawner in spawnersToActivate)
        {
            spawner.StartSpawning(OnEnemySpawned);
        }
    }

    private void OnEnemySpawned(Enemy newEnemy)
    {
        pendingSpawns--;
        if (newEnemy != null) activeEnemies.Add(newEnemy);

        // ★ [수정됨] 몬스터 등장 시에도 조용히 UI만 갱신
        UpdateEnemyCountUI();
    }

    private void Update()
    {
        if (isCleared) return;
        CheckBattleStatus();
    }

    private void CheckBattleStatus()
    {
        int beforeCount = activeEnemies.Count;
        activeEnemies.RemoveAll(x => x == null || x.IsDead);
        int afterCount = activeEnemies.Count;

        int deadCount = beforeCount - afterCount;
        if (deadCount > 0)
        {
            totalRemainingEnemies -= deadCount;
            // ★ [핵심] 몬스터가 죽었을 때만 playPunch = true 전달
            if (StageMessageUI.instance != null)
                StageMessageUI.instance.UpdateEnemyCount(totalRemainingEnemies, true);
        }

        if (activeEnemies.Count == 0 && pendingSpawns == 0) StartWave(currentWave + 1);
    }

    private void UpdateEnemyCountUI()
    {
        if (StageMessageUI.instance != null)
        {
            // ★ [수정됨] 기본 호출은 연출 없음 (playPunch = false)
            StageMessageUI.instance.UpdateEnemyCount(totalRemainingEnemies);
        }
    }

    private void StageClear()
    {
        isCleared = true;
        if (barrierEla != null) barrierEla.SetActive(false);
        if (statue != null) statue.ActivateStatue();
        if (StageMessageUI.instance != null)
        {
            StageMessageUI.instance.HideEnemyCountUI();
            StageMessageUI.instance.ShowClearMessage();
        }
    }
}