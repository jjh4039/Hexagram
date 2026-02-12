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
    [SerializeField] private float rectWidth = 0.3f;
    [SerializeField] private float rectLength = 6f;

    [Header("Spawn Point")]
    [SerializeField] private Transform headPoint;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 8f;

    [Header("Homing")]
    [SerializeField] private float homingStrength = 5f;

    [Header("Hit Reaction")]
    [SerializeField] private float knockbackForce = 4f;
    [SerializeField] private float stunTime = 0.25f;

    private Transform target;
    private Rigidbody2D rigid;

    private bool isAttacking = false;
    private bool isStunned = false;

    private GameObject maxRectInstance;
    private GameObject currentRectInstance;
    private Coroutine attackCoroutine;

    protected override void Awake()
    {
        base.Awake(); // 부모에서 anim 세팅
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
                yield return null;
                continue;
            }

            if (isStunned)
            {
                yield return null;
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

            yield return null;
        }
    }

    void MoveToTarget()
    {
        if (isStunned) return;

        if (anim != null)
            anim.SetBool("isMoving", true);

        Vector2 dir = (target.position - transform.position).normalized;
        transform.Translate(dir * moveSpeed * Time.deltaTime);
    }

    IEnumerator Co_AttackSequence()
    {
        isAttacking = true;

        if (anim != null)
            anim.SetBool("isMoving", false);

        Vector2 currentDir = (target.position - headPoint.position).normalized;

        maxRectInstance = Instantiate(maxRangeRectPrefab);
        currentRectInstance = Instantiate(currentRectPrefab);
    
        maxRectInstance.SetActive(true);
        currentRectInstance.SetActive(true);

        float timer = 0f;

        while (timer < chargeTime)
        {
            if (isStunned)   // 피격 시 공격 취소
            {
                CancelAttack();
                yield break;
            }

            timer += Time.deltaTime;

            Vector2 targetDir = (target.position - headPoint.position).normalized;

            currentDir = Vector2.Lerp(
                currentDir,
                targetDir,
                Time.deltaTime * homingStrength);

            UpdateRectangle(maxRectInstance, currentDir, rectLength);
            UpdateRectangle(currentRectInstance, currentDir, rectLength * (timer / chargeTime));

            yield return null;
        }

        yield return new WaitForSeconds(attackDelay);

        if (!isStunned)
        {
            if (anim != null)
                anim.SetTrigger("Attack");

            FireProjectile(currentDir);
        }

        ClearRectangles();

        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
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

        GameObject proj = Instantiate(
            projectilePrefab,
            headPoint.position,
            Quaternion.identity);

        EnemyProjectile projectile = proj.GetComponent<EnemyProjectile>();
        if (projectile != null)
            projectile.Initialize(dir, projectileSpeed);
    }

    void LookAtTarget()
    {
        float dirX = target.position.x - transform.position.x;

        if (dirX > 0)
            transform.localScale = new Vector3(-spriteScale, spriteScale, 1);
        else
            transform.localScale = new Vector3(spriteScale, spriteScale, 1);
    }

    protected override void OnHit()
    {
        if (isDead) return;

        if (!isStunned)
            StartCoroutine(Co_Stun());

        if (anim != null)
            anim.Play("Enemy_Hit", -1, 0f);

        if (target != null)
        {
            Vector2 dir = (transform.position - target.position).normalized;
            rigid.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
        }
    }

    IEnumerator Co_Stun()
    {
        isStunned = true;

        if (anim != null)
            anim.SetBool("isMoving", false);

        CancelAttack();

        yield return new WaitForSeconds(stunTime);

        isStunned = false;
    }

    void CancelAttack()
    {
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);

        ClearRectangles();
        isAttacking = false;
    }

    void ClearRectangles()
    {
        if (maxRectInstance != null)
            Destroy(maxRectInstance);

        if (currentRectInstance != null)
            Destroy(currentRectInstance);
    }
}
