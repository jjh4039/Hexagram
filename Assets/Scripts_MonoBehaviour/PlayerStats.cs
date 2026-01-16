using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    public int maxMana = 50;
    public int maxAmmo = 500;

    [Header("Current Stats")]
    public int currentHealth;
    public int currentMana;
    public int currentAmmo;

    [Header("ATK")]
    public float meleeAttackPower = 10f; // 근거리 공격력 (칼)
    public float rangeAttackPower = 7f;  // 원거리 공격력 (총)

    [Header("Player Stats - Precision (0.1 = 10%)")]
    [Range(0f, 0.5f)] public float meleeDamageVariance = 0.4f;
    [Range(0f, 0.5f)] public float rangedDamageVariance = 0.5f; 

    float testTimer = 0f;
    float testTimer2 = 0f;

    private void Start()
    {
        // 시작할 때 체력 꽉 채우기
        currentHealth = maxHealth;
        currentMana = maxMana;
        currentAmmo = maxAmmo;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"플레이어 체력 감소: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("!!! GAME OVER !!!");

        Player player = GameManager.instance.player;

        if (player != null)
        {
            player.OnDie();
        }

        // (나중에 여기에 GameManager.instance.ShowGameOverUI() 같은 거 넣을 예정)
    }

    private void Update()
    {
        // 아래는 테스트 코드들
        testTimer += Time.deltaTime;
        testTimer2 += Time.deltaTime;

        if (testTimer >= 2f)
        {
            testTimer = 0f; 

            currentHealth = Mathf.Min(currentHealth + 1, maxHealth);
            currentMana = Mathf.Min(currentMana + 1, maxMana); 
        }

       if (testTimer2 >= 0.01f) 
{
            testTimer2 = 0f; 

            currentAmmo = Mathf.Min(currentAmmo + 1, maxAmmo);
        } 
    }
}
