using UnityEngine;
using UnityEngine.InputSystem;

public class Balance : MonoBehaviour
{
    // 필드 아이템이 가질 확률 증가치 (인스펙터에서 하급 2, 중급 5 등으로 설정)
    [Header("Item Settings")]
    [SerializeField] private float weightPercent = 5f;

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
        if (GameManager.instance != null && GameManager.instance.balanceManager != null)
        {
            if (!GameManager.instance.balanceManager.gameObject.activeInHierarchy)
            {
                GameManager.instance.balanceManager.gameObject.SetActive(true);

                // ★ 매니저를 열 때, 이 아이템이 가진 퍼센트 수치를 함께 넘겨줍니다.
                GameManager.instance.balanceManager.OpenBalanceUI(weightPercent);

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