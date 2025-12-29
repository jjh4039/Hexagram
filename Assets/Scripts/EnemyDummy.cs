using System.Collections;
using UnityEngine;

public class EnemyDummy : Enemy
{
    [Header("visual")]
    private Animator anim;

    protected override void Start()
    {
        base.Start(); // Enmey - Start() : 체력 초기화
        anim = GetComponent<Animator>();
    }

    // 부모의 TakeDamage를 가져와서 기능 추가 (오버라이드)
    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage); // 부모 함수 실행 (체력 깎기)

        anim.SetTrigger("Hit");
    }
}
