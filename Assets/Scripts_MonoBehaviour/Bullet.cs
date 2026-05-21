using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 50f;
    public float lifeTime = 2f;

    [Header("VFX")]
    [SerializeField] private float damageMultiplier = 1.0f;

    private Rigidbody2D rigid;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    private Color myColor = Color.white;
    private Material myMaterial;

    private bool hasHit = false;

    private float cachedRangeAttackPower = 0f;
    private float cachedRangedVariance = 0f;
    private float cachedCriticalChance = 0f;
    private float cachedCriticalDamageMultiplier = 1.5f;
    private float cachedDiceDamageMultiplier = 1f;
    private float cachedDiceRangedDamageMultiplier = 1f;
    private float cachedStrongAttackMultiplier = 1f;

    private Coroutine lifeTimerCoroutine;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    public void SetupVisuals(Color color, Material material)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = color;

        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particles)
        {
            var main = ps.main;
            main.startColor = color;
        }

        myColor = color;
        myMaterial = material;
    }

    public void SetupCombatData(
        float rangeAttackPower,
        float rangedVariance,
        float criticalChance,
        float criticalDamageMultiplier,
        float diceDamageMultiplier,
        float diceRangedDamageMultiplier,
        float strongAttackMultiplier
    )
    {
        cachedRangeAttackPower = rangeAttackPower;
        cachedRangedVariance = rangedVariance;
        cachedCriticalChance = criticalChance;
        cachedCriticalDamageMultiplier = criticalDamageMultiplier;
        cachedDiceDamageMultiplier = diceDamageMultiplier;
        cachedDiceRangedDamageMultiplier = diceRangedDamageMultiplier;
        cachedStrongAttackMultiplier = strongAttackMultiplier;
    }

    private void OnEnable()
    {
        hasHit = false;

        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (col != null) col.enabled = true;
        
        if (rigid != null)
        {
            rigid.bodyType = RigidbodyType2D.Dynamic;
            rigid.linearVelocity = transform.right * speed;
        }

        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particles)
        {
            ps.Play();
        }

        TrailRenderer trail = GetComponentInChildren<TrailRenderer>();
        if (trail != null)
        {
            trail.Clear(); 
            trail.emitting = true;
        }

        lifeTimerCoroutine = StartCoroutine(Co_LifeTimer());
    }

    private System.Collections.IEnumerator Co_LifeTimer()
    {
        yield return new WaitForSeconds(lifeTime);
        if (!hasHit) HideAndDelayReturn();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;

        if (collision.CompareTag("Enemy"))
        {
            hasHit = true;
            if (lifeTimerCoroutine != null) StopCoroutine(lifeTimerCoroutine);

            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                CalculateAndDealDamage(enemy);
                SpawnHitEffect(transform.position);
            }

            HideAndDelayReturn();
        }
        else if (collision.CompareTag("Wall"))
        {
            hasHit = true;
            if (lifeTimerCoroutine != null) StopCoroutine(lifeTimerCoroutine);

            SpawnHitEffect(transform.position);
            HideAndDelayReturn();
        }
    }

    private void HideAndDelayReturn()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        if (col != null)
            col.enabled = false;

        if (rigid != null)
        {
            rigid.linearVelocity = Vector2.zero;
            rigid.bodyType = RigidbodyType2D.Kinematic;
        }

        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particles)
        {
            ps.Stop();
        }

        TrailRenderer trail = GetComponentInChildren<TrailRenderer>();
        if (trail != null)
        {
            trail.emitting = false;
        }

        // ★ 수정: Invoke를 지우고 타임스케일에 영향받지 않는 코루틴으로 대체
        StartCoroutine(Co_DelayReturnToPool());
    }

    private System.Collections.IEnumerator Co_DelayReturnToPool()
    {
        float timer = 0f;
        while(timer < 0.5f)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        
        Gun.ReturnBullet(this.gameObject);
    }

    private void SpawnHitEffect(Vector3 position)
    {
        Gun.SpawnHitEffect(position, transform.rotation, myMaterial, myColor);
    }

    private void CalculateAndDealDamage(Enemy enemy)
    {
        float baseDamage =
            cachedRangeAttackPower *
            damageMultiplier *
            cachedDiceDamageMultiplier *
            cachedDiceRangedDamageMultiplier *
            cachedStrongAttackMultiplier;

        float randomMultiplier = Random.Range(1.1f - cachedRangedVariance, 1.1f);
        float finalDamage = baseDamage * randomMultiplier;

        bool isCritical = Random.value < cachedCriticalChance;

        if (isCritical)
        {
            finalDamage *= cachedCriticalDamageMultiplier;
        }

        int damageInt = Mathf.RoundToInt(finalDamage);
        if (damageInt < 1) damageInt = 1;

        enemy.TakeDamage(damageInt, isCritical);

        if (GameManager.instance != null)
        {
            GameManager.instance.totalDamageDealt += damageInt;
        }
    }
}