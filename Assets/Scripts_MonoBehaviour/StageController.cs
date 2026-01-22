using UnityEngine;
using System.Collections.Generic;

public class StageController : MonoBehaviour
{
    [Header("--- Settings ---")]
    public Transform spawnPoint;    // 플레이어 시작 위치
    public GameObject barrierEla;   // 길을 막고 있는 방벽 (Ela)
    public Statue statue;

    [Header("--- Runtime Info ---")]
    // 현재 살아있는 몬스터들을 추적하는 리스트
    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool isCleared = false; // 이미 클리어했는지 체크

    public void InitStage()
    {
        // 1. 플레이어 이동
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && spawnPoint != null)
        {
            player.transform.position = spawnPoint.position;
        }

        // 2. 몬스터 소환 시작
        SpawnAllEnemies();

        // 3. 방벽 활성화 (못 나가게 막음)
        if (barrierEla != null) barrierEla.SetActive(true);
    }

    private void SpawnAllEnemies()
    {
        // 내 자식들 중에 'EnemySpawner'가 붙은 애들을 다 찾음
        EnemySpawner[] spawners = GetComponentsInChildren<EnemySpawner>();

        foreach (var spawner in spawners)
        {
            GameObject enemy = spawner.SpawnEnemy();
            if (enemy != null)
            {
                activeEnemies.Add(enemy); // 리스트에 명단 등록
            }
        }

        Debug.Log($"전투 시작! 몬스터 {activeEnemies.Count}마리 출현");
    }

    private void Update()
    {
        if (isCleared) return; // 이미 깼으면 검사 안 함

        CheckBattleStatus();
    }

    private void CheckBattleStatus()
    {
        // 리스트에서 죽어서 사라진(null이 된) 몬스터를 제거
        // (Tip: RemoveAll은 조건에 맞는 요소를 리스트에서 뺍니다)
        activeEnemies.RemoveAll(enemy => enemy == null);

        // 남은 몬스터가 0마리면 클리어!
        if (activeEnemies.Count == 0)
        {
            StageClear();
        }
    }

    private void StageClear()
    {
        isCleared = true;
        Debug.Log("스테이지 클리어! 방벽이 사라집니다.");

        if (barrierEla != null) barrierEla.SetActive(false);

        // ★ [추가] 석상 활성화!
        if (statue != null)
        {
            statue.ActivateStatue();
        }
    }
}