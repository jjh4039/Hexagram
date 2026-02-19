using UnityEngine;

public class EnemyProjectileFlash : MonoBehaviour
{
    [SerializeField] private float duration = 0.08f;
    [SerializeField] private float scaleMultiplier = 1.4f;

    private float timer;
    private Vector3 startScale;
    private SpriteRenderer spriteRenderer;
    private Color startColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startScale = transform.localScale;

        if (spriteRenderer != null)
            startColor = spriteRenderer.color;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = timer / duration;

        transform.localScale = Vector3.Lerp(
            startScale * scaleMultiplier,
            startScale * 0.8f,
            t);

        // 점점 투명해짐
        if (spriteRenderer != null)
        {
            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            spriteRenderer.color = c;
        }

        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }
}
