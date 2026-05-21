using System.Collections;
using UnityEngine;

public class EnemyDummy : Enemy
{
    private Rigidbody2D _rigid;

    protected override void Awake()
    {
        base.Awake();
        _rigid = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (isDead)
        {
            if (_rigid) _rigid.linearVelocity = Vector2.zero;
            return;
        }
    }

    protected override void OnHit()
    {
        if (isDead) return;
        if (Anim)
        {
            Anim.Play("Enemy_Hit", -1, 0f);
        }
    }
}