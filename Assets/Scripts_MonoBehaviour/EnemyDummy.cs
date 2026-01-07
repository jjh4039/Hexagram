using System.Collections;
using UnityEngine;

public class EnemyDummy : Enemy
{
    [Header("UI Fix")]
    [SerializeField] private Transform hpBarRoot; // 구조 통일을 위해 추가

    private Rigidbody2D rigid;

    protected override void Awake()
    {
        base.Awake();
        rigid = GetComponent<Rigidbody2D>();
    }

    protected override void Start()
    {
        base.Start();
        // 더미는 AI 코루틴(이동/공격)을 실행하지 않습니다.
    }

    private void Update()
    {
        // 죽었을 때 물리적인 미끄러짐 방지 (슬라임과 동일 로직)
        if (isDead)
        {
            if (rigid != null) rigid.linearVelocity = Vector2.zero; // ★ 최신 문법
            return;
        }
    }

    // 부모의 기능을 덮어쓰기 (Override)
    protected override void OnHit()
    {
        if (isDead) return;

        // 더미 특유의 기능: 딜레이 없이 즉시 타격 모션 재생 (샌드백 느낌)
        if (anim != null)
        {
            anim.Play("Enemy_Hit", -1, 0f);
        }

        // 더미는 넉백(밀려남)이나 경직(Stun) 로직을 넣지 않았습니다.
        // 만약 더미도 밀리게 하고 싶다면 슬라임의 knockback 코드를 가져오면 됩니다.
    }
}
