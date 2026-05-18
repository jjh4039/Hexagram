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

    // ★ 추가: 풀링을 위한 라이프타임 코루틴용 변수
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

    // ★ OnEnable: 풀에서 꺼내어 활성화될 때마다 초기화
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
            trail.Clear(); // 이전 궤적 초기화
            trail.emitting = true;
        }

        // 특정 시간이 지나도 안 부딪히면 스스로 풀로 돌아감
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

        // 트레일 등 잔여 이펙트가 꺼지길 기다린 후 풀로 반환
        Invoke(nameof(ReturnToPool), 0.5f);
    }

    private void ReturnToPool()
    {
        // Gun 스크립트에서 관리하는 Bullet Pool로 반환
        Gun.ReturnBullet(this.gameObject);
    }

    private void SpawnHitEffect(Vector3 position)
    {
        // Gun 스크립트에서 관리하는 이펙트 풀을 통해 생성
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