using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float currentHealth;
    [SerializeField] protected float contactDamage = 10f; // 몸통 박치기 데미지

    [Header("Effect")]
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private GameObject shadowObject;

    [Header("HpBar")]
    [SerializeField] protected Transform hpBarRoot;
    [SerializeField] private Transform hpBarFill;
    [SerializeField] private GameObject hpBarObject;
    private float initialScaleX;
    private SpriteRenderer[] hpBarSprites;

    [Header("Hit Flash")]
    [SerializeField] private Material flashMaterial;
    protected Material originalMaterial;
    private SpriteRenderer sr;
    private Coroutine flashRoutine;

    [SerializeField] private float hitStopDuration = 0.035f;

    [Header("Sound")]
    [SerializeField] private AudioClip sfxHit;

    [Header("Drop Settings")]
    [SerializeField] private GameObject scrapPrefab;
    [SerializeField][Range(0, 100)] private int dropChance = 100;

    protected Animator anim;
    private Collider2D col;
    protected bool isDead = false;

    public bool IsDead => isDead;
    public float ContactDamage => contactDamage; // 플레이어가 접근할 수 있도록 프로퍼티 추가

    protected virtual void Awake()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;

        if (scrapPrefab == null && GameManager.instance != null)
            scrapPrefab = GameManager.instance.commonScrapPrefab;

        if (hpBarFill != null)
            initialScaleX = hpBarFill.localScale.x;

        if (hpBarObject != null)
        {
            hpBarSprites = hpBarObject.GetComponentsInChildren<SpriteRenderer>();
            hpBarObject.SetActive(false);
        }

        if (sr != null)
            originalMaterial = sr.material;
    }

    public virtual void TakeDamage(float damage, bool isCritical = false)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (GameManager.instance != null)
            GameManager.instance.HitStop(hitStopDuration);

        if (CameraFollow.Instance != null)
            CameraFollow.Instance.HitShake(0.03f, 0.02f);

        if (hpBarObject != null && !hpBarObject.activeSelf)
            hpBarObject.SetActive(true);

        if (hpBarFill != null)
        {
            float ratio = currentHealth / maxHealth;
            if (ratio < 0) ratio = 0;
            hpBarFill.localScale = new Vector3(initialScaleX * ratio, hpBarFill.localScale.y, hpBarFill.localScale.z);
        }

        if (sfxHit != null)
            SoundManager.instance.PlaySFX(sfxHit, 0.3f, 0.1f);

        if (damageTextPrefab != null)
        {
            Vector3 randomOffset = new Vector3(Random.Range(-0.2f, 0.2f), 0.7f, 0);
            DamageText hud = DamageText.Spawn(damageTextPrefab, transform.position + randomOffset);
            hud.Setup(damage, isCritical);
        }

        if (gameObject.activeInHierarchy && sr != null && flashMaterial != null)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        if (currentHealth <= 0) Die();
        else OnHit();
    }

    private IEnumerator FlashRoutine()
    {
        sr.material = flashMaterial;
        yield return new WaitForSeconds(0.1f);
        sr.material = originalMaterial;
        flashRoutine = null;
    }

    protected virtual void OnHit()
    {
        if (anim != null)
            anim.SetTrigger("Hit");
    }

    protected virtual void Die()
    {
        isDead = true;

        if (col != null) col.enabled = false;
        if (shadowObject != null) shadowObject.SetActive(false);

        if (anim != null)
            anim.Play("Enemy_Die", 0, 0f);

        if (scrapPrefab != null && Random.Range(0, 100) < dropChance)
        {
            Transform scrapParent = null;
            if (GameManager.instance != null && GameManager.instance.currentStageObj != null)
            {
                scrapParent = GameManager.instance.currentStageObj.transform;
            }

            Instantiate(scrapPrefab, transform.position, Quaternion.identity, scrapParent);
        }

        if (hpBarObject != null && gameObject.activeInHierarchy)
            StartCoroutine(FadeOutHpBar());

        Destroy(gameObject, 1.0f);
    }

    private IEnumerator FadeOutHpBar()
    {
        float duration = 0.5f;
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

    private void LateUpdate()
    {
        UpdateHpBarFlip();
    }

    private void UpdateHpBarFlip()
    {
        if (hpBarRoot == null) return;

        float parentScaleX = transform.localScale.x;

        if (parentScaleX < 0)
            hpBarRoot.localScale = new Vector3(-1, 1, 1);
        else
            hpBarRoot.localScale = new Vector3(1, 1, 1);
    }
}