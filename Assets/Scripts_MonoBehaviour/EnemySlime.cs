using System.Collections;
using UnityEngine;

public class EnemySlime : Enemy
{
    [Header("AI Movement")]
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float idleTime = 1.0f;
    [SerializeField] private float moveTime = 2.0f;
    [SerializeField] private float spriteScale = 1.4f;

    [Header("AI Attack Condition")]
    [SerializeField] private float attackRange = 2f;    // AI가 멈추는 거리
    [SerializeField] private float attackCooldown = 2.0f;

    [Header("Attack Damage & Range")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackRadius = 1.15f;   // 공격 판정 반지름
    [SerializeField] private LayerMask targetLayer; // ★ 필수 추가: 플레이어 레이어 선택용

    [Header("Attack FX")] // ★ 헤더 추가
    [SerializeField] private GameObject slamEffectPrefab;

    [Header("Attack Effect")]
    [SerializeField] private GameObject rangeBackground;
    [SerializeField] private GameObject attackIndicator;
    [SerializeField] private float indicatorScale = 0.45f;
    [SerializeField] private float chargeTime = 1.0f;
    [SerializeField] private float attackDelay = 0.1f;

    [Header("Hit & Knockback")]
    [SerializeField] private float knockbackForce = 5.0f;
    [SerializeField] private float stunTime = 0.5f;

    [Header("UI Fix")]
    [SerializeField] private Transform hpBarRoot;

    private Transform target;
    private bool isAttacking = false;
    private bool isStunned = false;
    private Rigidbody2D rigid;

    protected override void Awake()
    {
        base.Awake();
        rigid = GetComponent<Rigidbody2D>();
    }

    protected override void Start()
    {
        base.Start();

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) target = playerObj.transform;

        if (rangeBackground != null) rangeBackground.SetActive(false);
        if (attackIndicator != null) attackIndicator.SetActive(false);

        StartCoroutine(Co_SlimeAI());
    }

    private void Update()
    {
        if (isDead)
        {
            if (rigid != null) rigid.linearVelocity = Vector2.zero;
            return;
        }

        if (target == null) return;
        LookAtTarget();
    }

    private void LookAtTarget()
    {
        // 기준점: 장판이 있으면 장판 위치, 없으면 몸통 위치
        Vector3 centerPos = transform.position;
        if (attackIndicator != null) centerPos = attackIndicator.transform.position;

        // 장판보다 플레이어가 오른쪽에 있나? 왼쪽에 있나?
        float dirX = target.position.x - centerPos.x;

        if (dirX > 0)
            transform.localScale = new Vector3(-spriteScale, spriteScale, 1);
        else
            transform.localScale = new Vector3(spriteScale, spriteScale, 1);

        // 체력바 반전 보정
        if (hpBarRoot != null)
        {
            if (transform.localScale.x < 0)
                hpBarRoot.localScale = new Vector3(-1, 1, 1);
            else
                hpBarRoot.localScale = new Vector3(1, 1, 1);
        }
    }

    protected override void OnHit()
    {
        if (isDead) return;

        if (anim != null) anim.Play("Enemy_Hit", -1, 0f);

        if (rigid != null && target != null)
        {
            Vector2 knockbackDir = (transform.position - target.position).normalized;
            rigid.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
        }

        if (!isStunned)
        {
            StartCoroutine(Co_HitRecovery());
        }
    }

    IEnumerator Co_HitRecovery()
    {
        isStunned = true;
        yield return new WaitForSeconds(stunTime);
        isStunned = false;

        if (isDead) yield break;
    }

    IEnumerator Co_SlimeAI()
    {
        while (!isDead)
        {
            if (isDead) break;
            if (isStunned) { yield return null; continue; }
            if (isAttacking) { yield return null; continue; }

            yield return StartCoroutine(Co_IdleState());

            if (isDead) break;
            if (isStunned) continue;

            // ★ 수정됨: AI 판단 기준도 '장판 위치'로 변경
            Vector3 checkPos = transform.position; // 기본값 (장판 없으면 몸통 기준)
            if (attackIndicator != null) checkPos = attackIndicator.transform.position;

            float dist = Vector2.Distance(checkPos, target.position);

            if (target != null && dist <= attackRange)
            {
                yield return StartCoroutine(Co_AttackSequence());
            }
            else
            {
                yield return StartCoroutine(Co_MoveState());
            }
        }
    }

    IEnumerator Co_IdleState()
    {
        if (anim != null) anim.SetBool("isMoving", false);

        float timer = 0f;
        while (timer < idleTime)
        {
            if (isDead) yield break;
            while (isStunned) yield return null;
            timer += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator Co_MoveState()
    {
        float timer = 0f;
        if (anim != null) anim.SetBool("isMoving", true);

        while (timer < moveTime)
        {
            if (isDead || target == null) yield break;

            while (isStunned)
            {
                if (anim != null) anim.SetBool("isMoving", false);
                yield return null;
            }

            if (anim != null) anim.SetBool("isMoving", true);

            // ★ 핵심 수정: 이동의 기준점도 '장판(AttackIndicator)'으로 변경
            Vector3 moveCenter = transform.position; // 기본값
            if (attackIndicator != null) moveCenter = attackIndicator.transform.position;

            // "장판이 플레이어 쪽으로 가려면 어느 쪽으로 가야 하지?"를 계산
            Vector2 dir = (target.position - moveCenter).normalized;

            transform.Translate(dir * moveSpeed * Time.deltaTime);

            timer += Time.deltaTime;
            yield return null;
        }

        if (anim != null) anim.SetBool("isMoving", false);
    }

    IEnumerator Co_AttackSequence()
    {
        isAttacking = true;
        if (anim != null) anim.SetBool("isMoving", false);

        // --- [Step 1: 차징] ---
        if (rangeBackground != null)
        {
            rangeBackground.SetActive(true);
            rangeBackground.transform.localScale = new Vector3(indicatorScale, indicatorScale, 1f);
        }

        if (attackIndicator != null)
        {
            attackIndicator.SetActive(true);
            attackIndicator.transform.localScale = Vector3.zero;

            float chargeTimer = 0f;
            while (chargeTimer < chargeTime)
            {
                if (isDead) { DisableIndicators(); yield break; }

                chargeTimer += Time.deltaTime;
                float progress = chargeTimer / chargeTime;
                float currentScale = Mathf.Lerp(0f, indicatorScale, progress);
                attackIndicator.transform.localScale = new Vector3(currentScale, currentScale, 1f);
                yield return null;
            }
            attackIndicator.transform.localScale = new Vector3(indicatorScale, indicatorScale, 1f);
        }
        else
        {
            yield return new WaitForSeconds(chargeTime);
        }

        if (isDead) { DisableIndicators(); yield break; }

        if (anim != null) anim.SetTrigger("Attack");

        yield return new WaitForSeconds(attackDelay); // 딜레이 대기

        if (isDead) { DisableIndicators(); yield break; }

        // ====================================================
        // ★ [여기가 타격 순간!] 이펙트 생성 로직 추가
        // ====================================================

        // 1. 이펙트 생성 위치 결정 (장판 중심)
        Vector2 impactPos = transform.position;
        if (attackIndicator != null) impactPos = attackIndicator.transform.position;

        // 2. 파티클 생성
        if (slamEffectPrefab != null)
        {
            GameObject vfx = Instantiate(slamEffectPrefab, impactPos, Quaternion.identity);

            // (옵션) 공격 범위(AttackRadius)에 맞춰 이펙트 크기를 키우고 싶다면?
            // vfx.transform.localScale = Vector3.one * attackRadius; 

            Destroy(vfx, 1.5f); // 찌꺼기 청소
        }

        // --- [Step 2: 공격 발동 (기존 로직)] ---
        Collider2D hit = Physics2D.OverlapCircle(impactPos, attackRadius, targetLayer); // impactPos 재활용

        if (hit != null)
        {
            Player player = hit.GetComponent<Player>();
            if (player != null)
            {
                Debug.Log($"<color=red>펑! 플레이어 피격!</color>");
                player.OnDamage(damage);
            }
        }

        // --- [Step 3: 정리] ---
        DisableIndicators();

        yield return new WaitForSeconds(attackCooldown);

        isAttacking = false;
    }

    private void DisableIndicators()
    {
        if (rangeBackground != null) rangeBackground.SetActive(false);
        if (attackIndicator != null) attackIndicator.SetActive(false);
    }

    // ★ 기즈모: 이제 빨간 원(타격), 노란 원(감지) 모두 장판 중심으로 그려짐
    private void OnDrawGizmos()
    {
        Vector3 center = transform.position;
        if (attackIndicator != null) center = attackIndicator.transform.position;

        // 1. 타격 범위 (빨강)
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawSphere(center, attackRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, attackRadius);

        // 2. 감지 범위 (노랑) - 이제 얘도 장판 기준!
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, attackRange);
    }
}