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

    [Header("--- Reward Settings (New) ---")]
    public GameObject rewardPrefab;       // 맵에 생성될 보상 프리팹 (인스펙터 할당)
    public Transform rewardSpawnPoint;    // 보상이 생성될 위치 (인스펙터 할당)

    // [미래 대비] 가이드 화살표 UI나 석상이 쉽게 접근할 수 있도록 프로퍼티로 열어둡니다.
    public Transform CurrentRewardTransform { get; private set; }
    public IRewardItem CurrentRewardItem { get; private set; }

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

        GameObject player = GameManager.instance.player.gameObject;
        if (player != null && spawnPoint != null) player.transform.position = spawnPoint.position;

        InitializeWaves();

        if (isSafeStage)
        {
            if (barrierEla != null) barrierEla.SetActive(true);
            
            SpawnReward();
            // ★ [수정됨] 생성된 보상 정보를 석상에 꽂아줍니다.
            if (statue != null) statue.ActivateStatue(CurrentRewardItem); 
            
            if (StageMessageUI.instance != null) StageMessageUI.instance.HideEnemyCountUI();
            isCleared = true;

            if (InputStateManager.Instance != null) InputStateManager.Instance.ChangeGamePhase(GamePhase.SafeZone);
        }
        else
        {
            if (barrierEla != null) barrierEla.SetActive(true);
            isCleared = false;

            if (InputStateManager.Instance != null) InputStateManager.Instance.ChangeGamePhase(GamePhase.InCombat);
        }

        if (!isSafeStage) UpdateEnemyCountUI();
        if (ArtifactManager.instance != null) ArtifactManager.instance.OnStageEnterTrigger();
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
            if (StageMessageUI.instance != null)
                StageMessageUI.instance.UpdateEnemyCount(totalRemainingEnemies, true);
        }

        if (activeEnemies.Count == 0 && pendingSpawns == 0) StartWave(currentWave + 1);
    }

    private void UpdateEnemyCountUI()
    {
        if (StageMessageUI.instance != null)
        {
            StageMessageUI.instance.UpdateEnemyCount(totalRemainingEnemies);
        }
    }

    // ★ [추가됨] 보상 생성 전담 함수
    private void SpawnReward()
    {
        if (rewardPrefab != null && rewardSpawnPoint != null)
        {
            // 4번째 인자로 rewardSpawnPoint를 넘겨주어 부모로 설정합니다.
            GameObject rewardObj = Instantiate(rewardPrefab, rewardSpawnPoint.position, Quaternion.identity, rewardSpawnPoint);
            
            CurrentRewardTransform = rewardObj.transform;
            CurrentRewardItem = rewardObj.GetComponent<IRewardItem>();
        }
    }

    private void StageClear()
    {
        isCleared = true;
        if (barrierEla) barrierEla.SetActive(false);
        
        SpawnReward();

        // ★ [수정됨] 생성된 보상 정보를 석상에 꽂아줍니다.
        if (statue) statue.ActivateStatue(CurrentRewardItem); 

        if (StageMessageUI.instance)
        {
            StageMessageUI.instance.HideEnemyCountUI();
            StageMessageUI.instance.ShowClearMessage();
        }

        if (InputStateManager.Instance) InputStateManager.Instance.ChangeGamePhase(GamePhase.SafeZone);
    }
}