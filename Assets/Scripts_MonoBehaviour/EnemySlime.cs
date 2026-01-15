using System.Collections;
using UnityEngine;

public class EnemySlime : Enemy // ★ 부모 상속
{
    [Header("AI Movement")]
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float idleTime = 1.0f;
    [SerializeField] private float moveTime = 2.0f;
    [SerializeField] private float spriteScale = 1.4f;

    [Header("AI Attack")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 2.0f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackRadius = 1.15f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Attack VFX")]
    [SerializeField] private GameObject slamEffectPrefab;
    [SerializeField] private GameObject rangeBackground;
    [SerializeField] private GameObject attackIndicator;
    [SerializeField] private float indicatorScale = 0.45f;
    [SerializeField] private float chargeTime = 1.0f;
    [SerializeField] private float attackDelay = 0.1f;

    [Header("Hit Reaction")]
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
        base.Awake(); // ★ 부모 Awake 실행 필수
        rigid = GetComponent<Rigidbody2D>();
    }

    protected override void Start()
    {
        base.Start(); // ★ 부모 Start 실행 필수 (매테리얼 저장, HP바 숨기기 등)

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) target = playerObj.transform;

        if (rangeBackground != null) rangeBackground.SetActive(false);
        if (attackIndicator != null) attackIndicator.SetActive(false);

        StartCoroutine(Co_SlimeAI());
    }

    // 부모의 OnHit를 덮어쓰기 (슬라임만의 반응 추가)
    protected override void OnHit()
    {
        if (isDead) return;

        // 1. 애니메이션 (★ 주의: 애니메이션 클립에서 Color 변경 키프레임 삭제했는지 확인!)
        if (anim != null) anim.Play("Enemy_Hit", -1, 0f);

        // 2. 넉백
        if (rigid != null && target != null)
        {
            Vector2 knockbackDir = (transform.position - target.position).normalized;
            rigid.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
        }

        // 3. 스턴 (잠깐 멈춤)
        if (!isStunned)
            StartCoroutine(Co_HitRecovery());
    }

    IEnumerator Co_HitRecovery()
    {
        isStunned = true;
        yield return new WaitForSeconds(stunTime);
        isStunned = false;
        // 스턴 끝나면 움직임 재개는 Update/AI에서 처리됨
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
        Vector3 centerPos = transform.position;
        if (attackIndicator != null) centerPos = attackIndicator.transform.position;

        float dirX = target.position.x - centerPos.x;

        if (dirX > 0)
            transform.localScale = new Vector3(-spriteScale, spriteScale, 1);
        else
            transform.localScale = new Vector3(spriteScale, spriteScale, 1);

        // 체력바 반전 보정
        if (hpBarRoot != null)
        {
            if (transform.localScale.x < 0) hpBarRoot.localScale = new Vector3(-1, 1, 1);
            else hpBarRoot.localScale = new Vector3(1, 1, 1);
        }
    }

    IEnumerator Co_SlimeAI()
    {
        while (!isDead)
        {
            if (isDead) break;
            if (isStunned) { yield return null; continue; }
            if (isAttacking) { yield return null; continue; }

            // 대기 -> 거리 체크 -> 공격 or 이동
            yield return StartCoroutine(Co_IdleState());

            if (isDead) break;
            if (isStunned) continue;

            Vector3 checkPos = transform.position;
            if (attackIndicator != null) checkPos = attackIndicator.transform.position;
            float dist = Vector2.Distance(checkPos, target.position);

            if (target != null && dist <= attackRange)
                yield return StartCoroutine(Co_AttackSequence());
            else
                yield return StartCoroutine(Co_MoveState());
        }
    }

    // ... (Co_IdleState, Co_MoveState, Co_AttackSequence는 기존 로직 그대로 유지) ...
    // 내용이 길어서 생략했지만, 기존에 작성하신 완벽한 로직 그대로 두시면 됩니다!

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

            Vector3 moveCenter = transform.position;
            if (attackIndicator != null) moveCenter = attackIndicator.transform.position;
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
        else yield return new WaitForSeconds(chargeTime);

        if (isDead) { DisableIndicators(); yield break; }
        if (anim != null) anim.SetTrigger("Attack");

        yield return new WaitForSeconds(attackDelay);

        if (isDead) { DisableIndicators(); yield break; }

        Vector2 impactPos = transform.position;
        if (attackIndicator != null) impactPos = attackIndicator.transform.position;

        if (slamEffectPrefab != null)
        {
            GameObject vfx = Instantiate(slamEffectPrefab, impactPos, Quaternion.identity);
            Destroy(vfx, 1.5f);
        }

        Collider2D hit = Physics2D.OverlapCircle(impactPos, attackRadius, targetLayer);
        if (hit != null)
        {
            Player player = hit.GetComponent<Player>();
            if (player != null) player.OnDamage(damage);
        }

        DisableIndicators();
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    private void DisableIndicators()
    {
        if (rangeBackground != null) rangeBackground.SetActive(false);
        if (attackIndicator != null) attackIndicator.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        Vector3 center = transform.position;
        if (attackIndicator != null) center = attackIndicator.transform.position;

        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawSphere(center, attackRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, attackRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, attackRange);
    }
}