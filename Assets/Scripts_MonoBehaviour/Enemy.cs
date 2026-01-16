using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float currentHealth;

    [Header("Effect")]
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private GameObject shadowObject;

    [Header("HpBar")]
    [SerializeField] private Transform hpBarFill;
    [SerializeField] private GameObject hpBarObject;
    private float initialScaleX;
    private SpriteRenderer[] hpBarSprites; // 체력바 페이드아웃용

    [Header("Hit Flash")]
    [SerializeField] private Material flashMaterial; // ★ 하얀색 매테리얼 연결
    private Material originalMaterial;
    private SpriteRenderer sr;
    private Coroutine flashRoutine;

    [Header("Sound")]
    [SerializeField] private AudioClip sfxHit;

    protected Animator anim;
    protected Collider2D col;
    protected bool isDead = false;

    protected virtual void Awake()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>(); // 스프라이트 렌더러 가져오기
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;

        // 1. 체력바 초기화
        if (hpBarFill != null)
            initialScaleX = hpBarFill.localScale.x;

        // 2. 체력바 숨기기 및 스프라이트 캐싱
        if (hpBarObject != null)
        {
            hpBarSprites = hpBarObject.GetComponentsInChildren<SpriteRenderer>();
            hpBarObject.SetActive(false); // 시작할 땐 숨김
        }

        // 3. 원래 매테리얼 저장 (플래시 효과 복구용)
        if (sr != null)
            originalMaterial = sr.material;
    }

    public virtual void TakeDamage(float damage, bool isCritical = false)
    {
        if (isDead) return;

        currentHealth -= damage;

        // ★ 맞았을 때 체력바 켜기
        if (hpBarObject != null && !hpBarObject.activeSelf)
        {
            hpBarObject.SetActive(true);
        }

        // 체력바 갱신
        if (hpBarFill != null)
        {
            float ratio = currentHealth / maxHealth;
            if (ratio < 0) ratio = 0;
            hpBarFill.localScale = new Vector3(initialScaleX * ratio, hpBarFill.localScale.y, hpBarFill.localScale.z);
        }

        if (sfxHit != null)
            SoundManager.instance.PlaySFX(sfxHit, 0.3f, 0.1f);

        // 데미지 텍스트
        if (damageTextPrefab != null)
        {
            Vector3 randomOffset = new Vector3(Random.Range(-0.2f, 0.2f), 0.7f, 0);
            GameObject hud = Instantiate(damageTextPrefab, transform.position + randomOffset, Quaternion.identity);
            hud.GetComponent<DamageText>().Setup(damage, isCritical);
        }

        // ★ 하얀색 플래시 효과 실행
        if (gameObject.activeInHierarchy && sr != null && flashMaterial != null)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        if (currentHealth <= 0) Die();
        else OnHit();
    }

    // 0.1초 동안 하얀색 매테리얼 입히기
    private IEnumerator FlashRoutine()
    {
        sr.material = flashMaterial;
        yield return new WaitForSeconds(0.1f);
        sr.material = originalMaterial;
        flashRoutine = null;
    }

    protected virtual void OnHit()
    {
        // 자식에서 오버라이드해서 씀 (애니메이션 등)
        if (anim != null) anim.SetTrigger("Hit");
    }

    protected virtual void Die()
    {
        isDead = true;

        if (col != null) col.enabled = false;
        if (shadowObject != null) shadowObject.SetActive(false);
        if (anim != null) anim.SetTrigger("Die");

        // ★ 체력바 서서히 사라지게 하기
        if (hpBarObject != null && gameObject.activeInHierarchy)
        {
            StartCoroutine(FadeOutHpBar());
        }

        Destroy(gameObject, 0.9f);
    }

    private IEnumerator FadeOutHpBar()
    {
        float duration = 0.5f; // 사라지는 시간
        float elapsed = 0f;

        if (hpBarSprites == null || hpBarSprites.Length == 0)
            hpBarSprites = hpBarObject.GetComponentsInChildren<SpriteRenderer>();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);

            foreach (SpriteRenderer s in hpBarSprites)
            {
                if (s != null)
                {
                    Color c = s.color;
                    s.color = new Color(c.r, c.g, c.b, alpha);
                }
            }
            yield return null;
        }

        hpBarObject.SetActive(false);
    }
}