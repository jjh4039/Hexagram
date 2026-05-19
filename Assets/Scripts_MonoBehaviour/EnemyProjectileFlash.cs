using UnityEngine;
using System.Collections.Generic;

public class EnemyProjectileFlash : MonoBehaviour
{
    [SerializeField] private float duration = 0.08f;
    [SerializeField] private float scaleMultiplier = 1.4f;

    private float timer;
    private Vector3 baseScale;
    private SpriteRenderer spriteRenderer;
    private Color startColor;
    
    private static Queue<EnemyProjectileFlash> pool = new Queue<EnemyProjectileFlash>();
    private static Transform poolContainer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;

        if (spriteRenderer != null)
            startColor = spriteRenderer.color;
    }
    
    public static EnemyProjectileFlash Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (poolContainer == null)
            poolContainer = new GameObject("EnemyProjectileFlash_Pool").transform;

        EnemyProjectileFlash epf;
        if (pool.Count > 0)
        {
            epf = pool.Dequeue();
            epf.transform.position = position;
            epf.transform.rotation = rotation;
            epf.gameObject.SetActive(true);
        }
        else
        {
            GameObject obj = Instantiate(prefab, position, rotation, poolContainer);
            epf = obj.GetComponent<EnemyProjectileFlash>();
        }
        return epf;
    }

    private void OnEnable()
    {
        timer = 0f;
        transform.localScale = baseScale;
        if (spriteRenderer != null) spriteRenderer.color = startColor;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = timer / duration;

        transform.localScale = Vector3.Lerp(
            baseScale * scaleMultiplier,
            baseScale * 0.8f,
            t);

        if (spriteRenderer != null)
        {
            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            spriteRenderer.color = c;
        }

        if (timer >= duration)
        {
            // ★ Destroy 대신 비활성화 후 큐에 반환
            gameObject.SetActive(false);
            pool.Enqueue(this);
        }
    }
}