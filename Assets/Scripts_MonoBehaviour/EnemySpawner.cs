using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("--- Settings ---")]
    public GameObject enemyPrefab;
    public GameObject warningPrefab;
    public float spawnDelay = 1f;

    [Header("--- Effects ---")]
    public GameObject spawnEffectPrefab;

    [Header("--- Wave Info ---")]
    public int waveNumber = 1;

    // ★ 느낌표(Warning) 및 스폰 이펙트용 스태틱 풀
    private static Queue<GameObject> warningPool = new Queue<GameObject>();
    private static Queue<GameObject> spawnEffectPool = new Queue<GameObject>();
    private static Transform poolContainer;

    public void StartSpawning(Action<Enemy> onSpawnFinished)
    {
        StartCoroutine(SpawnSequence(onSpawnFinished));
    }

    // ★ 스태틱 풀에서 꺼내오는 헬퍼 함수
    private GameObject GetFromPool(GameObject prefab, Queue<GameObject> pool, Vector3 pos, Transform parentObj = null)
    {
        if (prefab == null) return null;

        if (poolContainer == null)
            poolContainer = new GameObject("SpawnerEffect_Pool").transform;

        GameObject obj;
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
            obj.transform.position = pos;
            obj.transform.rotation = Quaternion.identity;
            obj.transform.SetParent(parentObj != null ? parentObj : poolContainer);
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(prefab, pos, Quaternion.identity, parentObj != null ? parentObj : poolContainer);
        }
        return obj;
    }

    // ★ 스태틱 풀로 반환하는 헬퍼 함수
    private void ReturnToPool(GameObject obj, Queue<GameObject> pool)
    {
        if (obj == null) return; // 몬스터와 함께 파괴되었다면 무시
        
        obj.SetActive(false);
        if (poolContainer != null) obj.transform.SetParent(poolContainer);
        pool.Enqueue(obj);
    }

    private IEnumerator SpawnSequence(Action<Enemy> onSpawnFinished)
    {
        // 1. 느낌표 풀에서 가져오기
        GameObject warningEffect = GetFromPool(warningPrefab, warningPool, transform.position);

        // 2. 대기 시간
        yield return new WaitForSeconds(spawnDelay);

        // 3. 느낌표 풀로 반환 (Destroy 대체)
        ReturnToPool(warningEffect, warningPool);

        // 4. 진짜 몬스터 생성 (이건 기존대로 Instantiate)
        if (enemyPrefab != null)
        {
            GameObject spawnedObj = Instantiate(enemyPrefab, transform.position, Quaternion.identity, this.transform);

            // 5. 스폰 이펙트 생성 및 반환 예약
            if (spawnEffectPrefab != null)
            {
                GameObject spawnEffect = GetFromPool(spawnEffectPrefab, spawnEffectPool, transform.position, spawnedObj.transform);
                StartCoroutine(Co_DelayReturn(spawnEffect, spawnEffectPool, 1.5f)); // 파티클 재생시간(1.5초) 후 반환
            }

            Enemy enemyScript = spawnedObj.GetComponent<Enemy>();
            onSpawnFinished?.Invoke(enemyScript);
        }
    }

    private IEnumerator Co_DelayReturn(GameObject obj, Queue<GameObject> pool, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(obj, pool);
    }
}