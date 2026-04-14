using UnityEngine;
using UnityEngine.InputSystem;

public class Balance : MonoBehaviour
{
    [SerializeField] private Material[] outLineMaterial;
    private SpriteRenderer spriteRenderer;
    public GameObject keyGuide;

    private bool isPlayerInRange = false;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (keyGuide != null) keyGuide.SetActive(false);
    }

    private void Update()
    {
        if (isPlayerInRange && Keyboard.current.fKey.wasPressedThisFrame)
        {
            OpenBalanceSelection();
        }
    }

    private void OpenBalanceSelection()
    {
        // 1. GameManager를 통해 BalanceManager 접근
        if (GameManager.instance != null && GameManager.instance.balanceManager != null)
        {
            if (!GameManager.instance.balanceManager.gameObject.activeInHierarchy)
            {
                // BalanceManager를 켜고 초기화 함수 호출
                GameManager.instance.balanceManager.gameObject.SetActive(true);
                GameManager.instance.balanceManager.OpenBalanceUI();

                // 2. 사용된 무게추 오브젝트 삭제
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            spriteRenderer.material = outLineMaterial[1];
            if (keyGuide != null) keyGuide.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            spriteRenderer.material = outLineMaterial[0];
            if (keyGuide != null) keyGuide.SetActive(false);
        }
    }
}