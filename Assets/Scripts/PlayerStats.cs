using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Player Stats")]
    public int maxHealth = 100;
    public int maxMana = 50;
    public int maxAmmo = 500;
    float testTimer = 0f;
    float testTimer2 = 0f;

    [Header("Player Stats")]
    public int currentHealth;
    public int currentMana;
    public int currentAmmo;

    private void Awake()
    {

    }

    private void Update()
    {
        // 아래는 테스트 코드들
        testTimer += Time.deltaTime;
        testTimer2 += Time.deltaTime;

        if (testTimer >= 0.1f)
        {
            testTimer = 0f; 

            currentHealth = Mathf.Min(currentHealth + 1, maxHealth);
            currentMana = Mathf.Min(currentMana + 1, maxMana); 
        }

        if (testTimer2 >= 0.02f) 
        {
            testTimer2 = 0f; 

            currentAmmo = Mathf.Min(currentAmmo + 1, maxAmmo);
        }
    }
}
