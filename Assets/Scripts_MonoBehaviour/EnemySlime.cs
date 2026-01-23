using System.Collections;
using UnityEngine;

public class EnemySlime : Enemy
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

    // ★ [삭제됨] hpBarRoot 변수 선언은 부모(Enemy)로 이동했습니다.
    // 하지만 아래 코드에서 hpBarRoot를 쓰는데 문제없습니다 (상속받았으니까요!)

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
        base.Start(); // ★ 부모 Start를 호출해야 고철을 받아옵니다!

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) target = playerObj.transform;

        if (rangeBackground != null) rangeBackground.SetActive(false);
        if (attackIndicator != null) attackIndicator.SetActive(false);

        StartCoroutine(Co_SlimeAI());
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

        if (!isStunned) StartCoroutine(Co_HitRecovery());
    }

    IEnumerator Co_HitRecovery()
    {
        isStunned = true;
        yield return new WaitForSeconds(stunTime);
        isStunned = false;
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

        // ★ 스프라이트 반전
        if (dirX > 0) transform.localScale = new Vector3(-spriteScale, spriteScale, 1);
        else transform.localScale = new Vector3(spriteScale, spriteScale, 1);

        // ★ 체력바 반전 보정 (부모의 hpBarRoot 사용)
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
}