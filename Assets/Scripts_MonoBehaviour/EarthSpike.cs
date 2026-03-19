using UnityEngine;
using System.Collections;

public class EarthSpike : MonoBehaviour
{
    [Header("Spike Settings")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float colliderEnableDelay = 0.15f;
    [SerializeField] private float colliderActiveTime = 0.2f;
    [SerializeField] private float destroyTime = 1.0f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject debrisPrefab; // ★ 흙먼지 파티클 연결할 곳

    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    public void Initialize(float dmg)
    {
        this.damage = dmg;
        StartCoroutine(Co_SpikeRoutine());
    }

    private IEnumerator Co_SpikeRoutine()
    { 
        yield return new WaitForSeconds(colliderEnableDelay);
        if (debrisPrefab != null)
        {
            GameObject vfx = Instantiate(debrisPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 1f); // 파티클은 1초 뒤 자동 삭제
        }

        if (col != null) col.enabled = true;

        yield return new WaitForSeconds(colliderActiveTime);
        if (col != null) col.enabled = false;

        Destroy(gameObject, destroyTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            if (player != null)
            {
                player.OnDamage(damage);
            }
        }
    }
}