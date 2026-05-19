using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBee : Enemy
{
    [Header("Elite Settings")]
    [SerializeField] private bool isElite = false; 
    [SerializeField] private int eliteProjCount = 5; 
    [SerializeField] private float eliteSpreadAngle = 15f; 

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

    private List<GameObject> maxRectInstances = new List<GameObject>(); 
    private List<GameObject> currentRectInstances = new List<GameObject>();
    private Coroutine attackCoroutine;
    private Coroutine stunCoroutine;

    // ★ 조준선들을 모아둘 전용 최상단 폴더
    private static Transform telegraphPoolContainer;

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
        int projCount = isElite ? eliteProjCount : 1;

        // ★ DamageText 방식처럼 최상단 폴더가 없으면 생성
        if (telegraphPoolContainer == null)
        {
            telegraphPoolContainer = new GameObject("EnemyBee_Telegraph_Pool").transform;
        }

        if (maxRangeRectPrefab != null && currentRectPrefab != null)
        {
            for (int i = 0; i < projCount; i++)
            {
                if (i >= maxRectInstances.Count)
                {
                    // ★ 생성 시 telegraphPoolContainer에 넣습니다.
                    maxRectInstances.Add(Instantiate(maxRangeRectPrefab, telegraphPoolContainer));
                    currentRectInstances.Add(Instantiate(currentRectPrefab, telegraphPoolContainer));
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

            Vector2[] dirs = GetSpreadDirections(currentDir);

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
            if (anim != null)
                anim.SetTrigger("Attack");

            FireProjectiles(currentDir);
        }

        ClearRectangles();

        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
        attackCoroutine = null;
    }

    private Vector2[] GetSpreadDirections(Vector2 mainDir)
    {
        int count = isElite ? eliteProjCount : 1;
        Vector2[] dirs = new Vector2[count];

        if (count == 1)
        {
            dirs[0] = mainDir;
            return dirs;
        }

        float startAngle = -(count - 1) * eliteSpreadAngle / 2f;
        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + i * eliteSpreadAngle;
            float rad = angle * Mathf.Deg2Rad;
            float s = Mathf.Sin(rad);
            float c = Mathf.Cos(rad);
            dirs[i] = new Vector2(mainDir.x * c - mainDir.y * s, mainDir.x * s + mainDir.y * c);
        }

        return dirs;
    }

    void UpdateRectangle(GameObject rect, Vector2 dir, float length)
    {
        if (rect == null) return;

        // ★ 이제 월드에 존재하는 독립 오브젝트이므로, 위치와 방향만 세팅하면 됩니다.
        rect.transform.position = headPoint.position;
        rect.transform.right = dir;

        SpriteRenderer sr = rect.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.size = new Vector2(length, rectWidth);
    }

    void FireProjectiles(Vector2 mainDir)
    {
        if (projectilePrefab == null) return;

        Vector2[] dirs = GetSpreadDirections(mainDir);

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

    // ★ EnemyBee 오브젝트가 삭제될 때 자신이 생성한 조준선들도 깔끔하게 제거
    private void OnDestroy()
    {
        foreach (var rect in maxRectInstances)
            if (rect != null) Destroy(rect);

        foreach (var rect in currentRectInstances)
            if (rect != null) Destroy(rect);
    }
}