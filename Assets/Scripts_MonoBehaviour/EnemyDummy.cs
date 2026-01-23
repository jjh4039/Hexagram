using System.Collections;
using UnityEngine;

public class EnemyDummy : Enemy
{
    // ★ [삭제됨] hpBarRoot 변수는 부모(Enemy)로 이동했습니다.
    // 기존에 [Header("UI Fix")] 도 이제 필요 없습니다.

    private Rigidbody2D rigid;

    protected override void Awake()
    {
        base.Awake();
        rigid = GetComponent<Rigidbody2D>();
    }

    protected override void Start()
    {
        base.Start();
    }

    private void Update()
    {
        if (isDead)
        {
            if (rigid != null) rigid.linearVelocity = Vector2.zero;
            return;
        }
    }

    protected override void OnHit()
    {
        if (isDead) return;
        if (anim != null)
        {
            anim.Play("Enemy_Hit", -1, 0f);
        }
    }
}