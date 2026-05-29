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

        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            target = GameManager.instance.player.transform;
        }

        if (rangeBackground != null) rangeBackground.SetActive(false);
        if (attackIndicator != null) attackIndicator.SetActive(false);

        StartCoroutine(Co_SlimeAI());
    }

    protected override void OnHit()
    {
        if (isDead) return;

        if (Anim)
            Anim.Play("Enemy_Hit", -1, 0f);

        if (rigid && target)
        {
            rigid.linearVelocity = Vector2.zero;
            Vector2 knockbackDir = (transform.position - target.position).normalized;
            rigid.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
        }

        if (!isStunned)
            StartCoroutine(Co_HitRecovery());
    }

    IEnumerator Co_HitRecovery()
    {
        isStunned = true;
        yield return new WaitForSeconds(stunTime);
        isStunned = false;
    }

    private void Update()
    {
        if (isDead || !target) return;
        LookAtTarget();
    }

    private void LookAtTarget()
    {
        float dirX = target.position.x - transform.position.x;

        if (dirX > 0)
            transform.localScale = new Vector3(-spriteScale, spriteScale, 1);
        else
            transform.localScale = new Vector3(spriteScale, spriteScale, 1);
    }

    IEnumerator Co_SlimeAI()
    {
        while (!isDead)
        {
            if (isStunned || isAttacking)
            {
                yield return null;
                continue;
            }

            yield return StartCoroutine(Co_IdleState());

            if (isDead || isStunned) continue;

            float dist = Vector2.Distance(transform.position, target.position);

            if (dist <= attackRange)
                yield return StartCoroutine(Co_AttackSequence());
            else
                yield return StartCoroutine(Co_MoveState());
        }
    }

    IEnumerator Co_IdleState()
    {
        if (Anim) Anim.SetBool("isMoving", false);
        yield return new WaitForSeconds(idleTime);
    }

    IEnumerator Co_MoveState()
    {
        float timer = 0f;
        if (Anim) Anim.SetBool("isMoving", true);

        while (timer < moveTime)
        {
            if (isDead || isStunned)
            {
                if (rigid) rigid.linearVelocity = Vector2.zero;
                yield break;
            }

            Vector2 dir = (target.position - transform.position).normalized;

            if (rigid)
                rigid.linearVelocity = dir * moveSpeed;

            timer += Time.fixedDeltaTime; 
            yield return new WaitForFixedUpdate(); 
        }

        if (rigid) rigid.linearVelocity = Vector2.zero; 
        if (Anim) Anim.SetBool("isMoving", false);
    }

    IEnumerator Co_AttackSequence()
    {
        isAttacking = true;

        if (Anim)
            Anim.SetBool("isMoving", false);

        if (rangeBackground)
        {
            rangeBackground.SetActive(true);
            rangeBackground.transform.localScale =
                new Vector3(indicatorScale, indicatorScale, 1f);
        }

        if (attackIndicator)
        {
            attackIndicator.SetActive(true);
            attackIndicator.transform.localScale = Vector3.zero;

            float chargeTimer = 0f;
            while (chargeTimer < chargeTime)
            {
                if (isDead) { DisableIndicators(); yield break; }

                chargeTimer += Time.deltaTime;
                float progress = chargeTimer / chargeTime;

                float currentScale =
                    Mathf.Lerp(0f, indicatorScale, progress);

                attackIndicator.transform.localScale =
                    new Vector3(currentScale, currentScale, 1f);

                yield return null;
            }

            attackIndicator.transform.localScale =
                new Vector3(indicatorScale, indicatorScale, 1f);
        }
        else
        {
            yield return new WaitForSeconds(chargeTime);
        }

        if (Anim)
            Anim.SetTrigger("Attack");

        yield return new WaitForSeconds(attackDelay);

        Vector2 impactPos = transform.position;

        if (slamEffectPrefab)
        {
            GameObject vfx =
                Instantiate(slamEffectPrefab, impactPos, Quaternion.identity);
            Destroy(vfx, 1.5f);
        }

        Collider2D hit =
            Physics2D.OverlapCircle(impactPos, attackRadius, targetLayer);

        if (hit)
        {
            Player player = hit.GetComponent<Player>();
            if (player)
                player.OnDamage(damage);
        }

        DisableIndicators();

        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    private void DisableIndicators()
    {
        if (rangeBackground) rangeBackground.SetActive(false);
        if (attackIndicator) attackIndicator.SetActive(false);
    }
}