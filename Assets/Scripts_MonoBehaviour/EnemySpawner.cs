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

    private static Queue<GameObject> warningPool = new Queue<GameObject>();
    private static Queue<GameObject> spawnEffectPool = new Queue<GameObject>();
    private static Transform poolContainer;

    public void StartSpawning(Action<Enemy> onSpawnFinished)
    {
        StartCoroutine(SpawnSequence(onSpawnFinished));
    }

    private GameObject GetFromPool(GameObject prefab, Queue<GameObject> pool, Vector3 pos, Transform parentObj = null)
    {
        if (prefab == null) return null;

        if (poolContainer == null)
        {
            poolContainer = new GameObject("SpawnerEffect_Pool").transform;
            warningPool.Clear(); 
            spawnEffectPool.Clear();
        }

        GameObject obj = null;

        while (pool.Count > 0)
        {
            obj = pool.Dequeue();
            if (obj) break; 
        }

        if (obj)
        {
            obj.transform.position = pos;
            obj.transform.rotation = Quaternion.identity;
            obj.transform.SetParent(parentObj ? parentObj : poolContainer);
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(prefab, pos, Quaternion.identity, parentObj ? parentObj : poolContainer);
        }
        return obj;
    }

    private void ReturnToPool(GameObject obj, Queue<GameObject> pool)
    {
        if (!obj) return; 
        
        obj.SetActive(false);
        if (poolContainer) obj.transform.SetParent(poolContainer);
        pool.Enqueue(obj);
    }

    private IEnumerator SpawnSequence(Action<Enemy> onSpawnFinished)
    {
        GameObject warningEffect = GetFromPool(warningPrefab, warningPool, transform.position);

        yield return new WaitForSeconds(spawnDelay);

        ReturnToPool(warningEffect, warningPool);

        if (enemyPrefab)
        {
            GameObject spawnedObj = Instantiate(enemyPrefab, transform.position, Quaternion.identity, this.transform);

            if (spawnEffectPrefab)
            {
                GameObject spawnEffect = GetFromPool(spawnEffectPrefab, spawnEffectPool, transform.position);
                StartCoroutine(Co_DelayReturn(spawnEffect, spawnEffectPool, 1.5f)); 
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