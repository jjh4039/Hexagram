using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 10f;

    [Header("Movement")]
    [SerializeField] private float speed = 8f;

    [Header("Lifetime")]
    [SerializeField] private float lifeTime = 5f;

    private Rigidbody2D rigid;
    private Vector2 moveDir;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void Initialize(Vector2 direction, float overrideSpeed)
    {
        moveDir = direction.normalized;
        speed = overrideSpeed;

        RotateToDirection();

        rigid.linearVelocity = moveDir * speed;
    }

    void RotateToDirection()
    {
        float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            if (player != null)
                player.OnDamage(damage);

            Destroy(gameObject);
        }

        if (collision.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}
