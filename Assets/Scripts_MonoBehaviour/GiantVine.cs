using UnityEngine;
using System.Collections;

public class GiantVine : MonoBehaviour
{
    [Header("Vine Settings")]
    [SerializeField] private float damage = 30f;
    [SerializeField] private float pierceSpeed = 0.05f;
    [SerializeField] private float duration = 0.4f;

    private Collider2D col;
    private SpriteRenderer sr;
    private float fixedWidth; // 처음에 설정된 가로 폭 저장용

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            sr.color = new Color(1, 1, 1, 0);
            // 인스펙터에 설정된 초기 Width를 고정값으로 기억합니다.
            fixedWidth = sr.size.x;
            sr.size = new Vector2(fixedWidth, 0f);
        }
        if (col != null) col.enabled = false;
    }

    public void Fire(float dmg, float targetLength)
    {
        this.damage = dmg;
        // 스케일이 2배라면, 실제 그려져야 할 Sprite의 Size는 절반이어야 딱 맞습니다.
        float adjustedLength = targetLength / transform.localScale.y;
        StartCoroutine(Co_Strike(adjustedLength));
    }

    private IEnumerator Co_Strike(float targetLength)
    {
        if (sr) sr.color = Color.white;
        if (col) col.enabled = true;

        float timer = 0f;
        while (timer < pierceSpeed)
        {
            timer += Time.deltaTime;
            float currentLength = Mathf.Lerp(0f, targetLength, timer / pierceSpeed);

            // 가로 폭(fixedWidth)은 유지하고 세로 길이만 늘림
            if (sr != null) sr.size = new Vector2(fixedWidth, currentLength);

            if (col is BoxCollider2D box)
            {
                box.size = new Vector2(fixedWidth, currentLength);
                box.offset = new Vector2(0, currentLength / 2f);
            }
            yield return null;
        }

        if (sr != null) sr.size = new Vector2(fixedWidth, targetLength);
        if (col is BoxCollider2D finalBox)
        {
            finalBox.size = new Vector2(fixedWidth, targetLength);
            finalBox.offset = new Vector2(0, targetLength / 2f);
        }

        yield return new WaitForSeconds(0.1f);
        if (col != null) col.enabled = false;

        timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / duration);
            if (sr != null) sr.color = new Color(1, 1, 1, alpha);
            yield return null;
        }
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            if (player != null) player.OnDamage(damage);
        }
    }
}