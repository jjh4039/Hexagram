using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class StageController : MonoBehaviour
{
    [Header("--- Stage Type ---")]
    public bool isSafeStage = false;

    [Header("--- Settings ---")]
    public Transform spawnPoint;
    public GameObject barrierEla;
    public Statue statue;

    [Header("--- UI References ---")]
    public TextMeshProUGUI enemyCountText;

    [Header("--- Runtime Info ---")]
    private List<Enemy> activeEnemies = new List<Enemy>();
    private bool isCleared = false;

    // ★ [추가] 초기화가 되었는지 확인하는 깃발
    private bool isInitialized = false;

    // ★ [추가] 시작할 때 아무도 나를 안 건드렸으면 스스로 초기화!
    private void Start()
    {
        if (!isInitialized)
        {
            InitStage();
        }
    }

    public void InitStage()
    {
        // 이미 초기화했다면 중복 실행 방지
        if (isInitialized) return;
        isInitialized = true;

        if (enemyCountText == null)
        {
            GameObject uiObj = GameObject.Find("EnemyCount_Text");
            if (uiObj != null) enemyCountText = uiObj.GetComponent<TextMeshProUGUI>();
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && spawnPoint != null)
        {
            // 플레이어가 이미 배치된 상태라면 굳이 이동 안 시켜도 되지만,
            // 확실하게 하려면 이동시킴 (시작 방 위치로)
            player.transform.position = spawnPoint.position;
        }

        SpawnAllEnemies();

        // 안전지대 로직
        if (isSafeStage)
        {
            Debug.Log("안전지대(시작됨): 석상 활성화 (문은 안 염)");

            if (barrierEla != null) barrierEla.SetActive(true);
            if (statue != null) statue.ActivateStatue();
            if (enemyCountText != null) enemyCountText.text = "";

            isCleared = true; // 클리어 처리 -> Update 문 실행 안 됨
        }
        else
        {
            Debug.Log("전투지대(시작됨): 전투 시작");

            if (barrierEla != null) barrierEla.SetActive(true);
            isCleared = false;
        }

        if (!isSafeStage) UpdateEnemyCountUI();
    }

    private void SpawnAllEnemies()
    {
        EnemySpawner[] spawners = GetComponentsInChildren<EnemySpawner>();
        activeEnemies.Clear();

        foreach (var spawner in spawners)
        {
            GameObject enemyObj = spawner.SpawnEnemy();
            if (enemyObj != null)
            {
                Enemy enemyScript = enemyObj.GetComponent<Enemy>();
                if (enemyScript != null)
                {
                    activeEnemies.Add(enemyScript);
                }
            }
        }
    }

    private void Update()
    {
        // 안전지대면 isCleared가 true라서 여기 아예 통과 못 함 (텍스트 뜰 일 없음)
        if (isCleared) return;

        CheckBattleStatus();
    }

    private void CheckBattleStatus()
    {
        int beforeCount = activeEnemies.Count;
        activeEnemies.RemoveAll(x => x == null || x.IsDead);
        int afterCount = activeEnemies.Count;

        if (beforeCount != afterCount)
        {
            UpdateEnemyCountUI();
        }

        if (activeEnemies.Count == 0)
        {
            StageClear();
        }
    }

    private void UpdateEnemyCountUI()
    {
        if (enemyCountText != null)
        {
            if (activeEnemies.Count <= 0)
            {
                enemyCountText.text = "";
            }
            else
            {
                enemyCountText.text = $"남은 적 : <color=red>{activeEnemies.Count}</color>";
            }
        }
    }

    private void StageClear()
    {
        isCleared = true;

        if (barrierEla != null) barrierEla.SetActive(false);
        if (statue != null) statue.ActivateStatue();

        if (enemyCountText != null) enemyCountText.text = "";

        if (StageMessageUI.instance != null)
        {
            StageMessageUI.instance.ShowClearMessage();
        }
    }
}