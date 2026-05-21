using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StageController : MonoBehaviour
{
    [Header("Stage Type")]
    public bool isSafeStage = false;                      
    public bool isBossStage = false;                      

    [Header("Start Room Settings (Intro)")]
    public bool isStartingRoom = false;                   
    public float startFadeDelay = 0.5f;                   
    public string startTitleText = "시스템 가동";          
    public string startDescText = "모듈 테스트를 시작합니다."; 
    public Color startTitleColor = Color.cyan;            

    [Header("Settings")]
    public Transform spawnPoint;                          
    public GameObject barrierEla;                         
    public Statue statue;                                 
    public Collider2D stageBounds;                        

    [Header("Reward Settings")]
    public GameObject rewardPrefab;                       
    public Transform rewardSpawnPoint;                    
    public int moduleRewardCount = 1;                     

    public Transform CurrentRewardTransform { get; private set; } 
    public IRewardItem CurrentRewardItem { get; private set; }    

    private List<Enemy> activeEnemies = new List<Enemy>(); 
    private bool isCleared = false;                        
    private bool isInitialized = false;                    
    private bool isBattleStarted = false;                  

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

        if (CameraFollow.Instance != null && stageBounds != null)
        {
            CameraFollow.Instance.SetCameraBounds(stageBounds.bounds);
        }

        // ★ 보스 스테이지 진입 시 기존 맵 브금을 페이드 아웃 시킵니다 (보스 조우 시 새 브금이 켜짐)
        if (isBossStage && SoundManager.instance != null)
        {
            SoundManager.instance.StopBGM(1.5f);
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
        if (ArtifactManager.Instance != null) ArtifactManager.Instance.OnStageEnterTrigger();

        StartCoroutine(Co_DelayedStart());
    }

    private IEnumerator Co_DelayedStart()
    {
        yield return new WaitForSeconds(startFadeDelay);

        if (isStartingRoom && StageMessageUI.instance != null)
        {
            StageMessageUI.instance.ShowCustomEntryMessage(startTitleText, startDescText, startTitleColor);
        }

        if (!isSafeStage)
        {
            isBattleStarted = true;
            StartWave(1);
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
        if (isCleared || !isBattleStarted) return;
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
            if (!isBossStage && StageMessageUI.instance != null)
                StageMessageUI.instance.UpdateEnemyCount(totalRemainingEnemies, true);
        }

        if (activeEnemies.Count == 0 && pendingSpawns == 0) StartWave(currentWave + 1);
    }

    private void UpdateEnemyCountUI()
    {
        if (isBossStage) return;

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

            if (moduleRewardCount > 0)
            {
                StageMessageUI.instance.ShowClearMessage();
                StageMessageUI.instance.QueueModuleReward(moduleRewardCount);
            }
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