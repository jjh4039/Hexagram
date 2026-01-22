using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("--- Settings ---")]
    public GameObject enemyPrefab; // 여기서 소환될 몬스터 프리펩

    // 몬스터를 소환하고, 소환된 녀석을 반환하는 함수
    public GameObject SpawnEnemy()
    {
        if (enemyPrefab != null)
        {
            // 내 위치(transform.position)에 몬스터 생성
            return Instantiate(enemyPrefab, transform.position, Quaternion.identity);
        }
        return null;
    }
}