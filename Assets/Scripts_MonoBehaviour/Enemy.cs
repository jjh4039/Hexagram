using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] protected float maxHealth = 4000f;
    [SerializeField] protected float currentHealth;
    [SerializeField] protected float contactDamage = 10f; 

    [Header("Effect")]
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private GameObject shadowObject;

    [Header("HpBar")]
    [SerializeField] protected Transform hpBarRoot;
    [SerializeField] private Transform hpBarFill;
    [SerializeField] private GameObject hpBarObject;
    private float _initialScaleX;
    private SpriteRenderer[] _hpBarSprites;

    [Header("Hit Flash")]
    [SerializeField] private Material flashMaterial;
    protected Material OriginalMaterial;
    private SpriteRenderer _sr;
    private Coroutine _flashRoutine;

    [SerializeField] private float hitStopDuration = 0.035f;

    [Header("Sound")]
    [SerializeField] private AudioClip sfxHit;

    [Header("Drop Settings")]
    [SerializeField] private GameObject scrapPrefab;
    [SerializeField][Range(0, 100)] private int dropChance = 100;

    protected Animator Anim;
    private Collider2D _col;
    protected bool isDead = false;

    public bool IsDead => isDead;
    public float ContactDamage => contactDamage; 

    protected virtual void Awake()
    {
        Anim = GetComponent<Animator>();
        _col = GetComponent<Collider2D>();
        _sr = GetComponent<SpriteRenderer>();
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;

        if (scrapPrefab == null && GameManager.instance != null)
            scrapPrefab = GameManager.instance.commonScrapPrefab;

        if (hpBarFill != null)
            _initialScaleX = hpBarFill.localScale.x;

        if (hpBarObject != null)
        {
            _hpBarSprites = hpBarObject.GetComponentsInChildren<SpriteRenderer>();
            hpBarObject.SetActive(false);
        }

        if (_sr != null)
            OriginalMaterial = _sr.material;
    }

    public virtual void TakeDamage(float damage, bool isCritical = false)
    {
        if (isDead) return;

        currentHealth -= damage;

        // ★ 수정: 타격감이 너무 과해지는(화면이 덜덜 떨리는) 현상 방지
        // 치명타가 터졌을 때만 히트스톱과 화면 흔들림을 주어 타격감을 극대화합니다.
        if (isCritical)
        {
            if (GameManager.instance != null)
                GameManager.instance.HitStop(hitStopDuration);

            if (CameraFollow.Instance != null)
                CameraFollow.Instance.HitShake(0.04f, 0.025f);
        }

        if (hpBarObject != null && !hpBarObject.activeSelf)
            hpBarObject.SetActive(true);

        if (hpBarFill != null)
        {
            float ratio = currentHealth / maxHealth;
            if (ratio < 0) ratio = 0;
            hpBarFill.localScale = new Vector3(_initialScaleX * ratio, hpBarFill.localScale.y, hpBarFill.localScale.z);
        }

        if (sfxHit != null)
            SoundManager.instance.PlaySFX(sfxHit, 0.3f, 0.1f);

        if (damageTextPrefab != null)
        {
            Vector3 randomOffset = new Vector3(Random.Range(-0.2f, 0.2f), 0.7f, 0);
            DamageText hud = DamageText.Spawn(damageTextPrefab, transform.position + randomOffset);
            hud.Setup(damage, isCritical);
        }

        if (gameObject.activeInHierarchy && _sr != null && flashMaterial != null)
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        if (currentHealth <= 0) Die();
        else OnHit();
    }

    private IEnumerator FlashRoutine()
    {
        _sr.material = flashMaterial;
        // 타임스케일에 영향을 받지 않는 플래시 효과
        yield return new WaitForSecondsRealtime(0.08f); 
        
        if (_sr != null) _sr.material = OriginalMaterial;
        _flashRoutine = null;
    }

    protected virtual void OnHit()
    {
        if (Anim != null)
            Anim.SetTrigger("Hit");
    }

    protected virtual void Die()
    {
        isDead = true;

        if (_col != null) _col.enabled = false;
        if (shadowObject != null) shadowObject.SetActive(false);

        if (Anim != null)
            Anim.Play("Enemy_Die", 0, 0f);

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

        if (_hpBarSprites == null || _hpBarSprites.Length == 0)
            _hpBarSprites = hpBarObject.GetComponentsInChildren<SpriteRenderer>();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);

            foreach (SpriteRenderer s in _hpBarSprites)
            {
                // ★ 수정: 파괴 과정 중 NullReference 에러를 막기 위한 이중 방어
                if (s != null && s.gameObject != null) 
                {
                    Color c = s.color;
                    s.color = new Color(c.r, c.g, c.b, alpha);
                }
            }

            yield return null;
        }

        if (hpBarObject != null) hpBarObject.SetActive(false);
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