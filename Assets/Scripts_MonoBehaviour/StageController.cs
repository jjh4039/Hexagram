using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro;
using System.Collections;

public class StageController : MonoBehaviour
{
    [Header("Stage Type")]
    public bool isSafeStage = false;                      // 안전 방 여부

    [Header("Start Room Settings (Intro)")]
    public bool isStartingRoom = false;                   // ★ 시작 방 여부 (체크 시 인트로 실행)
    public string startTitleText = "시스템 가동";          // 시작 방 진입 시 띄울 제목
    public string startDescText = "모듈 테스트를 시작합니다."; // 시작 방 진입 시 띄울 설명
    public Color startTitleColor = Color.cyan;            // 제목 색상

    [Header("Settings")]
    public Transform spawnPoint;                          // 플레이어 시작 위치
    public GameObject barrierEla;                         // 전투 진입 차단벽
    public Statue statue;                                 // 출구 석상
    public Collider2D stageBounds;                        // 카메라 제한용 콜라이더

    [Header("Reward Settings")]
    public GameObject rewardPrefab;                       // 맵에 생성될 보상 프리팹
    public Transform rewardSpawnPoint;                    // 보상이 생성될 위치
    public int moduleRewardCount = 1;                     // 클리어 시 제공할 모듈 강화 횟수

    public Transform CurrentRewardTransform { get; private set; } // 생성된 보상의 좌표
    public IRewardItem CurrentRewardItem { get; private set; }    // 생성된 보상의 인터페이스

    private List<Enemy> activeEnemies = new List<Enemy>(); // 현재 활성화된 적 목록
    private bool isCleared = false;                        // 스테이지 클리어 여부
    private bool isInitialized = false;                    // 스테이지 초기화 여부

    private Dictionary<int, List<EnemySpawner>> waveSpawners = new Dictionary<int, List<EnemySpawner>>();
    private int currentWave = 1;                           // 현재 진행 중인 웨이브 번호
    private int pendingSpawns = 0;                         // 대기 중인 적 스폰 수
    private int totalRemainingEnemies = 0;                 // 남은 전체 적 수

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

        if (CameraFollow.instance != null && stageBounds != null)
        {
            CameraFollow.instance.SetCameraBounds(stageBounds.bounds);
        }

        InitializeWaves();

        if (isSafeStage)
        {
            if (barrierEla != null) barrierEla.SetActive(true);
            
            SpawnReward();
            
            if (statue != null) statue.ActivateStatue(CurrentRewardItem); 
            
            if (StageMessageUI.instance != null) StageMessageUI.instance.HideEnemyCountUI();
            isCleared = true;

            Transform statueTarget = GetStatueArrowTarget();
            if (GuideArrow.Instance != null && player != null)
                GuideArrow.Instance.ActivateArrow(player.transform, null, null, statueTarget);

            if (InputStateManager.Instance != null) InputStateManager.Instance.ChangeGamePhase(GamePhase.SafeZone);
        }
        else
        {
            if (barrierEla != null) barrierEla.SetActive(true);
            isCleared = false;

            if (GuideArrow.Instance != null) GuideArrow.Instance.HideArrow();

            if (InputStateManager.Instance != null) InputStateManager.Instance.ChangeGamePhase(GamePhase.InCombat);
        }

        if (!isSafeStage) UpdateEnemyCountUI();
        if (ArtifactManager.instance != null) ArtifactManager.instance.OnStageEnterTrigger();

        // ★ 조작 제어 없이 순수하게 텍스트 메시지만 출력
        if (isStartingRoom && StageMessageUI.instance != null)
        {
            StageMessageUI.instance.ShowCustomEntryMessage(startTitleText, startDescText, startTitleColor);
        }
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

    private void SpawnReward()
    {
        if (rewardPrefab != null && rewardSpawnPoint != null)
        {
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

        if (statue) statue.ActivateStatue(CurrentRewardItem);

        if (StageMessageUI.instance)
        {
            StageMessageUI.instance.HideEnemyCountUI();
            StageMessageUI.instance.ShowClearMessage();
            StageMessageUI.instance.QueueModuleReward(moduleRewardCount);
        }

        if (!isSafeStage)
        {
            if (GameManager.instance != null && GameManager.instance.player != null && GameManager.instance.player.buffManager != null)
            {
                GameManager.instance.player.buffManager.OnStageCleared();
                Debug.Log("전투 스테이지 클리어: 디버프 지속 횟수 차감");
            }
        }

        Transform statueTarget = GetStatueArrowTarget();
        GameObject player = GameManager.instance.player.gameObject;

        if (GuideArrow.Instance != null && player != null)
            GuideArrow.Instance.ActivateArrow(player.transform, CurrentRewardTransform, CurrentRewardItem, statueTarget);

        if (InputStateManager.Instance) InputStateManager.Instance.ChangeGamePhase(GamePhase.SafeZone);
    }

    private Transform GetStatueArrowTarget()
    {
        if (statue == null) return null;
        return statue.arrowTargetPos != null ? statue.arrowTargetPos : statue.transform;
    }
}