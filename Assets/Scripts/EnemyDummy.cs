using System.Collections;
using UnityEngine;

public class EnemyDummy : Enemy
{
    protected override void Start()
    {
        base.Start(); // Enmey - Start() : 체력 초기화
        anim = GetComponent<Animator>();
    }

    protected override void OnHit()
    {
        // 부모의 SetTrigger 무시하고, 내 방식(Play)으로 재생
        if (anim != null)
        {
            anim.Play("Enemy_Hit", -1, 0f); // 딜레이 없이 즉시 재생
        }
    }
}
