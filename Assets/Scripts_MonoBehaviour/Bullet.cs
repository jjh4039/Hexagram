using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 50f;
    public float lifeTime = 2f;

    [Header("VFX")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float damageMultiplier = 1.0f;

    private Rigidbody2D rigid;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    private Color myColor = Color.white;
    private Material myMaterial;

    private bool hasHit = false;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    public void SetupVisuals(Color color, Material material)
    {
        if (spriteRenderer != null) spriteRenderer.color = color;

        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particles)
        {
            var main = ps.main;
            main.startColor = color;
        }

        myColor = color;
        myMaterial = material;
    }

    private void Start()
    {
        rigid.linearVelocity = transform.right * speed;
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;

        if (collision.CompareTag("Enemy"))
        {
            hasHit = true;
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                CalculateAndDealDamage(enemy);
                SpawnHitEffect(transform.position);
            }
            HideAndDelayDestroy();
        }
        else if (collision.CompareTag("Wall"))
        {
            hasHit = true;
            SpawnHitEffect(transform.position);
            HideAndDelayDestroy();
        }
    }

    private void HideAndDelayDestroy()
    {
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (col != null) col.enabled = false;

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

        Destroy(gameObject, 0.5f);
    }

    private void SpawnHitEffect(Vector3 position)
    {
        if (hitEffectPrefab == null) return;

        Quaternion reverseRotation = transform.rotation * Quaternion.Euler(0, 0, 180f);
        GameObject vfx = Instantiate(hitEffectPrefab, position, reverseRotation);

        ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
        ParticleSystemRenderer psr = vfx.GetComponent<ParticleSystemRenderer>();

        if (psr != null && myMaterial != null)
        {
            psr.material = myMaterial;
        }

        if (ps != null)
        {
            var main = ps.main;
            main.startColor = myColor;
            ps.Play();
        }

        Destroy(vfx, 1.0f);
    }

    private void CalculateAndDealDamage(Enemy enemy)
    {
        PlayerStats stats = GameManager.instance.stats;

        float baseDamage = stats.rangeAttackPower * damageMultiplier * stats.damageMultiplier;

        float variance = stats.rangedDamageVariance;
        float randomMultiplier = Random.Range(1.1f - variance, 1.1f);
        float finalDamage = baseDamage * randomMultiplier;

        bool isCritical = Random.value < stats.criticalChance;

        if (stats.remainingStrongAttacks > 0)
        {
            isCritical = true;
            stats.remainingStrongAttacks--;
        }

        if (isCritical)
        {
            finalDamage *= stats.criticalDamageMultiplier;
        }

        int damageInt = Mathf.RoundToInt(finalDamage);
        if (damageInt < 1) damageInt = 1;

        enemy.TakeDamage(damageInt, isCritical);
    }
}