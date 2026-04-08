using System.Collections;
using UnityEngine;

public class EnemyBee : Enemy
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float spriteScale = 1.2f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 6f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float chargeTime = 1f;
    [SerializeField] private float attackDelay = 0.1f;

    [Header("Rectangle Indicator (World Space)")]
    [SerializeField] private GameObject maxRangeRectPrefab;
    [SerializeField] private GameObject currentRectPrefab;
    [SerializeField] private float rectWidth = 0.05f;
    [SerializeField] private float rectLength = 1.5f;

    [Header("Spawn Point")]
    [SerializeField] private Transform headPoint;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 10f;

    [Header("Homing")]
    [SerializeField] private float homingStrength = 5f;

    [Header("Hit Reaction")]
    [SerializeField] private float knockbackForce = 1.5f;
    [SerializeField] private float stunTime = 0.25f;

    [Header("Muzzle Flash")]
    [SerializeField] private GameObject projectileFlashPrefab;
    [SerializeField] private float flashDistance = 0.3f;
    [SerializeField] private float fireRecoilForce = 0.6f;

    [Header("Sound")]
    [SerializeField] private AudioClip sfxFire;

    private Transform target;
    private Rigidbody2D rigid;

    private bool isAttacking = false;
    private bool isStunned = false;

    private GameObject maxRectInstance;
    private GameObject currentRectInstance;
    private Coroutine attackCoroutine;
    private Coroutine stunCoroutine;

    protected override void Awake()
    {
        base.Awake();
        rigid = GetComponent<Rigidbody2D>();
    }

    protected override void Start()
    {
        base.Start();

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            target = playerObj.transform;

        StartCoroutine(Co_BeeAI());
    }

    private void Update()
    {
        if (isDead || target == null) return;
        LookAtTarget();
    }

    IEnumerator Co_BeeAI()
    {
        while (!isDead)
        {
            if (target == null)
            {
                if (rigid != null)
                    rigid.linearVelocity = Vector2.zero;

                yield return new WaitForFixedUpdate();
                continue;
            }

            if (isStunned)
            {
                yield return new WaitForFixedUpdate();
                continue;
            }

            float dist = Vector2.Distance(transform.position, target.position);

            if (!isAttacking)
            {
                if (dist <= attackRange)
                {
                    attackCoroutine = StartCoroutine(Co_AttackSequence());
                }
                else
                {
                    MoveToTarget();
                }
            }

            yield return new WaitForFixedUpdate();
        }
    }

    void MoveToTarget()
    {
        if (isStunned || rigid == null || target == null) return;

        if (anim != null)
            anim.SetBool("isMoving", true);

        Vector2 dir = (target.position - transform.position).normalized;
        rigid.linearVelocity = dir * moveSpeed;
    }

    void CancelAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        if (rigid != null)
            rigid.linearVelocity = Vector2.zero;

        ClearRectangles();
        isAttacking = false;

        if (anim != null)
            anim.SetBool("isMoving", false);
    }

    IEnumerator Co_AttackSequence()
    {
        isAttacking = true;

        if (anim != null)
            anim.SetBool("isMoving", false);

        Vector2 currentDir = (target.position - headPoint.position).normalized;

        if (maxRangeRectPrefab != null)
        {
            maxRectInstance = Instantiate(maxRangeRectPrefab);
            maxRectInstance.SetActive(true);
        }

        if (currentRectPrefab != null)
        {
            currentRectInstance = Instantiate(currentRectPrefab);
            currentRectInstance.SetActive(true);
        }

        float timer = 0f;

        while (timer < chargeTime)
        {
            if (isStunned || isDead || target == null)
            {
                CancelAttack();
                yield break;
            }

            timer += Time.deltaTime;

            Vector2 targetDir = (target.position - headPoint.position).normalized;
            currentDir = Vector2.Lerp(currentDir, targetDir, Time.deltaTime * homingStrength);

            UpdateRectangle(maxRectInstance, currentDir, rectLength);
            UpdateRectangle(currentRectInstance, currentDir, rectLength * (timer / chargeTime));

            yield return null;
        }

        yield return new WaitForSeconds(attackDelay);

        if (!isStunned && !isDead)
        {
            if (anim != null)
                anim.SetTrigger("Attack");

            FireProjectile(currentDir);
        }

        ClearRectangles();

        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
        attackCoroutine = null;
    }

    void UpdateRectangle(GameObject rect, Vector2 dir, float length)
    {
        if (rect == null) return;

        rect.transform.position = headPoint.position;
        rect.transform.right = dir;

        SpriteRenderer sr = rect.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.size = new Vector2(length, rectWidth);
    }

    void FireProjectile(Vector2 dir)
    {
        if (projectilePrefab == null) return;

        if (projectileFlashPrefab != null)
        {
            Vector3 flashPos = headPoint.position + (Vector3)(dir.normalized * flashDistance);

            GameObject flash = Instantiate(
                projectileFlashPrefab,
                flashPos,
                Quaternion.identity
            );

            flash.transform.right = dir;
        }

        if (rigid != null)
            rigid.AddForce(-dir.normalized * fireRecoilForce, ForceMode2D.Impulse);

        if (sfxFire != null && SoundManager.instance != null)
            SoundManager.instance.PlaySFX(sfxFire, 0.35f, 0.05f);

        GameObject proj = Instantiate(
            projectilePrefab,
            headPoint.position,
            Quaternion.identity
        );

        EnemyProjectile projectile = proj.GetComponent<EnemyProjectile>();
        if (projectile != null)
            projectile.Initialize(dir, projectileSpeed);
    }

    void LookAtTarget()
    {
        float dirX = target.position.x - transform.position.x;

        if (dirX > 0)
            transform.localScale = new Vector3(-spriteScale, spriteScale, 1f);
        else
            transform.localScale = new Vector3(spriteScale, spriteScale, 1f);
    }

    protected override void OnHit()
    {
        if (isDead) return;

        isStunned = true;

        CancelAttack();

        if (anim != null)
            anim.Play("Enemy_Hit", -1, 0f);

        if (rigid != null && target != null)
        {
            rigid.linearVelocity = Vector2.zero;
            Vector2 knockbackDir = (transform.position - target.position).normalized;
            rigid.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
        }

        if (stunCoroutine != null)
            StopCoroutine(stunCoroutine);

        stunCoroutine = StartCoroutine(Co_Stun());
    }

    IEnumerator Co_Stun()
    {
        if (anim != null)
            anim.SetBool("isMoving", false);

        yield return new WaitForSeconds(stunTime);

        isStunned = false;
        stunCoroutine = null;
    }

    void ClearRectangles()
    {
        if (maxRectInstance != null)
        {
            Destroy(maxRectInstance);
            maxRectInstance = null;
        }

        if (currentRectInstance != null)
        {
            Destroy(currentRectInstance);
            currentRectInstance = null;
        }
    }

    protected override void Die()
    {
        CancelAttack();

        base.Die();

        if (rigid != null)
        {
            rigid.linearVelocity = Vector2.zero;
            rigid.simulated = false;
        }
    }
}