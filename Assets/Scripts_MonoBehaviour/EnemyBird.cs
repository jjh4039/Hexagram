using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBird : Enemy
{
    [Header("Elite Settings")]
    [SerializeField] private bool isElite = false; 

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
    [SerializeField] private float projectileDamage = 15f; 

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

    private List<GameObject> maxRectInstances = new List<GameObject>(); // 로컬 풀
    private List<GameObject> currentRectInstances = new List<GameObject>(); // 로컬 풀
    private Coroutine attackCoroutine;
    private Coroutine stunCoroutine;

    private Transform telegraphContainer; // 독립된 조준선 전용 월드 컨테이너

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

        telegraphContainer = new GameObject($"{gameObject.name}_Telegraphs").transform; // 스케일 반전(Flip) 버그 방지용

        if (GameManager.instance != null && GameManager.instance.stats != null)
        {
            projectileDamage *= GameManager.instance.stats.enemyStatMultiplier;
        }
        
        StartCoroutine(Co_BirdAI());
    }

    private void Update()
    {
        if (isDead || target == null) return;
        LookAtTarget();
    }

    IEnumerator Co_BirdAI()
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

        if (Anim != null)
            Anim.SetBool("isMoving", true);

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

        if (Anim != null)
            Anim.SetBool("isMoving", false);
    }

    IEnumerator Co_AttackSequence()
    {
        isAttacking = true;

        if (Anim != null)
            Anim.SetBool("isMoving", false);

        Vector2 currentDir = (target.position - headPoint.position).normalized;
        int projCount = isElite ? 8 : 4;

        if (maxRangeRectPrefab != null && currentRectPrefab != null)
        {
            for (int i = 0; i < projCount; i++)
            {
                if (i >= maxRectInstances.Count)
                {
                    maxRectInstances.Add(Instantiate(maxRangeRectPrefab, telegraphContainer));
                    currentRectInstances.Add(Instantiate(currentRectPrefab, telegraphContainer));
                }
                maxRectInstances[i].SetActive(true);
                currentRectInstances[i].SetActive(true);
            }
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

            Vector2[] dirs = GetDirections(currentDir);

            for (int i = 0; i < dirs.Length; i++)
            {
                UpdateRectangle(maxRectInstances[i], dirs[i], rectLength);
                UpdateRectangle(currentRectInstances[i], dirs[i], rectLength * (timer / chargeTime));
            }

            yield return null;
        }

        yield return new WaitForSeconds(attackDelay);

        if (!isStunned && !isDead)
        {
            if (Anim != null)
                Anim.SetTrigger("Attack");

            FireProjectiles(currentDir);
        }

        ClearRectangles();

        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
        attackCoroutine = null;
    }

    private Vector2[] GetDirections(Vector2 mainDir)
    {
        int count = isElite ? 8 : 4;
        Vector2[] dirs = new Vector2[count];

        dirs[0] = mainDir;
        dirs[1] = -mainDir;
        dirs[2] = new Vector2(-mainDir.y, mainDir.x);
        dirs[3] = new Vector2(mainDir.y, -mainDir.x);

        if (isElite)
        {
            float rad = 45f * Mathf.Deg2Rad;
            float s = Mathf.Sin(rad);
            float c = Mathf.Cos(rad);

            for (int i = 0; i < 4; i++)
            {
                dirs[i + 4] = new Vector2(dirs[i].x * c - dirs[i].y * s, dirs[i].x * s + dirs[i].y * c);
            }
        }

        return dirs;
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

    void FireProjectiles(Vector2 mainDir)
    {
        if (projectilePrefab == null) return;

        Vector2[] dirs = GetDirections(mainDir);

        foreach (Vector2 dir in dirs)
        {
            if (projectileFlashPrefab != null)
            {
                Vector3 flashPos = headPoint.position + (Vector3)(dir.normalized * flashDistance);
                EnemyProjectileFlash.Spawn(projectileFlashPrefab, flashPos, Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg));
            }

            EnemyProjectile projectile = EnemyProjectile.Spawn(projectilePrefab, headPoint.position, Quaternion.identity);
            if (projectile != null)
                projectile.Initialize(dir, projectileSpeed, projectileDamage); 
        }

        if (rigid != null)
            rigid.AddForce(-mainDir.normalized * fireRecoilForce, ForceMode2D.Impulse);

        if (sfxFire != null && SoundManager.instance != null)
            SoundManager.instance.PlaySFX(sfxFire, 0.35f, 0.05f);
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

        if (Anim != null)
            Anim.Play("Enemy_Hit", -1, 0f);

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
        if (Anim != null)
            Anim.SetBool("isMoving", false);

        yield return new WaitForSeconds(stunTime);

        isStunned = false;
        stunCoroutine = null;
    }

    void ClearRectangles()
    {
        foreach (var rect in maxRectInstances)
            if (rect != null) rect.SetActive(false);

        foreach (var rect in currentRectInstances)
            if (rect != null) rect.SetActive(false);
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

    private void OnDestroy()
    {
        if (telegraphContainer != null) Destroy(telegraphContainer.gameObject); // 컨테이너 파괴 시 자식(조준선) 일괄 삭제
    }
}