using System.Collections;
using System;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("--- Settings ---")]
    public GameObject enemyPrefab;
    public GameObject warningPrefab;
    public float spawnDelay = 1f;

    // ★ [추가 1] 몬스터가 등장할 때 터질 파티클(이펙트) 프리팹
    [Header("--- Effects ---")]
    public GameObject spawnEffectPrefab;

    [Header("--- Wave Info ---")]
    public int waveNumber = 1;

    public void StartSpawning(Action<Enemy> onSpawnFinished)
    {
        StartCoroutine(SpawnSequence(onSpawnFinished));
    }

    private IEnumerator SpawnSequence(Action<Enemy> onSpawnFinished)
    {
        // 1. 느낌표 생성
        GameObject warningEffect = null;
        if (warningPrefab != null)
        {
            warningEffect = Instantiate(warningPrefab, transform.position, Quaternion.identity);
        }

        // 2. 대기 시간
        yield return new WaitForSeconds(spawnDelay);

        // 3. 느낌표 파괴
        if (warningEffect != null) Destroy(warningEffect);

        // ★ [추가 2] 몬스터 생성과 동시에 이펙트 펑! 생성
        if (spawnEffectPrefab != null)
        {
            Instantiate(spawnEffectPrefab, transform.position, Quaternion.identity);
        }

        // 4. 진짜 몬스터 생성
        if (enemyPrefab != null)
        {
            GameObject spawnedObj = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
            Enemy enemyScript = spawnedObj.GetComponent<Enemy>();

            onSpawnFinished?.Invoke(enemyScript);
        }
    }
}