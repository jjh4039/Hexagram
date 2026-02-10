using System.Collections;
using UnityEngine;

public class EnemyBee : Enemy
{
    [Header("Movement Base")]
    [SerializeField] protected float moveSpeed = 2.0f;
    [SerializeField] protected float spriteScale = 1.2f;

    [Header("Hit Reaction")]
    [SerializeField] private float knockbackForce = 4.0f;
    [SerializeField] private float stunTime = 0.3f;

    protected Transform target;
    protected Rigidbody2D rigid;

    protected bool isStunned = false;

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
    }

    protected virtual void Update()
    {
        if (isDead)
        {
            if (rigid != null)
                rigid.linearVelocity = Vector2.zero;
            return;
        }

        if (target == null) return;

        LookAtTarget();

        if (isStunned) return;

        // ★ 이동 / 공격 AI는 여기서 파생 클래스에서 구현
    }

    // =========================
    // 피격 처리
    // =========================
    protected override void OnHit()
    {
        if (isDead) return;

        if (anim != null)
            anim.Play("Enemy_Hit", -1, 0f);

        ApplyKnockback();

        if (!isStunned)
            StartCoroutine(Co_Stun());
    }

    protected virtual void ApplyKnockback()
    {
        if (rigid == null || target == null) return;

        Vector2 dir = (transform.position - target.position).normalized;
        rigid.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
    }

    IEnumerator Co_Stun()
    {
        isStunned = true;
        yield return new WaitForSeconds(stunTime);
        isStunned = false;
    }

    // =========================
    // 플레이어 바라보기
    // =========================
    protected virtual void LookAtTarget()
    {
        float dirX = target.position.x - transform.position.x;

        if (dirX > 0)
            transform.localScale = new Vector3(-spriteScale, spriteScale, 1);
        else
            transform.localScale = new Vector3(spriteScale, spriteScale, 1);

        // ★ 체력바 반전 보정 (Enemy 부모 hpBarRoot 사용)
        if (hpBarRoot != null)
        {
            if (transform.localScale.x < 0)
                hpBarRoot.localScale = new Vector3(-1, 1, 1);
            else
                hpBarRoot.localScale = new Vector3(1, 1, 1);
        }
    }
}
