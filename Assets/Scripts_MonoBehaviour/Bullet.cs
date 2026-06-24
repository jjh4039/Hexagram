using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Settings")] public float speed = 50f;
    public float lifeTime = 2f;

    [Header("VFX")] [SerializeField] private float damageMultiplier = 1.0f;

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
    private float cachedFinalDamageMultiplier = 1f;

    private int currentPenetration = 0; // 남은 관통 횟수

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
        float strongAttackMultiplier,
        int penetration,
        float finalDamageMultiplier
    )
    {
        cachedRangeAttackPower = rangeAttackPower;
        cachedRangedVariance = rangedVariance;
        cachedCriticalChance = criticalChance;
        cachedCriticalDamageMultiplier = criticalDamageMultiplier;
        cachedDiceDamageMultiplier = diceDamageMultiplier;
        cachedDiceRangedDamageMultiplier = diceRangedDamageMultiplier;
        cachedStrongAttackMultiplier = strongAttackMultiplier;

        currentPenetration = penetration;
        cachedFinalDamageMultiplier = finalDamageMultiplier;
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
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                CalculateAndDealDamage(enemy);
                SpawnHitEffect(transform.position);
            }

            if (currentPenetration > 0)
            {
                currentPenetration--;
            }
            else
            {
                hasHit = true;
                if (lifeTimerCoroutine != null) StopCoroutine(lifeTimerCoroutine);
                HideAndDelayReturn();
            }
        }
        else if (collision.CompareTag("Wall") && !collision.isTrigger) // 트리거가 아닌 실제 물리 벽인지 확인
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

        StartCoroutine(Co_DelayReturnToPool());
    }

    private System.Collections.IEnumerator Co_DelayReturnToPool()
    {
        float timer = 0f;
        while (timer < 0.5f)
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
            cachedStrongAttackMultiplier *
            cachedFinalDamageMultiplier;

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
            if (GameManager.instance.stats != null)
            {
                GameManager.instance.stats.AddDiceChargeFromHit();
            }
        }
    }
}