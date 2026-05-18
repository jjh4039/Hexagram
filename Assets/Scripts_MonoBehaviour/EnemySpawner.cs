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

        // 4. 진짜 몬스터 생성
        if (enemyPrefab != null)
        {
            // ★ [수정됨] 4번째 매개변수로 transform을 넣어 스포너의 자식으로 생성합니다.
            GameObject spawnedObj = Instantiate(enemyPrefab, transform.position, Quaternion.identity, this.transform);

            // 몬스터 생성 직후, 파티클을 생성하며 몬스터(spawnedObj.transform)를 부모로 지정합니다.
            if (spawnEffectPrefab != null)
            {
                Instantiate(spawnEffectPrefab, transform.position, Quaternion.identity, spawnedObj.transform);
            }

            Enemy enemyScript = spawnedObj.GetComponent<Enemy>();
            onSpawnFinished?.Invoke(enemyScript);
        }
    }
}