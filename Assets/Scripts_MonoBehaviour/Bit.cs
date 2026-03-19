using UnityEngine;
using UnityEngine.InputSystem; // 최신 Input System 사용

public class Bit : MonoBehaviour
{
    [SerializeField] private Material[] outLineMaterial;
    private SpriteRenderer spriteRenderer;
    public GameObject keyGuide;

    private bool isPlayerInRange = false;

    public void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (keyGuide != null) keyGuide.SetActive(false);
    }

    private void Update()
    {
        // 플레이어가 근처에 있고, F 키를 눌렀을 때
        if (isPlayerInRange && Keyboard.current.fKey.wasPressedThisFrame)
        {
            OpenBitSelection();
        }
    }

    private void OpenBitSelection()
    {
        // 1. BitManager 활성화
        if (GameManager.instance != null && GameManager.instance.bitManager != null)
        {
            // BitManager를 켜고 초기화 함수 호출
            GameManager.instance.bitManager.gameObject.SetActive(true);
            GameManager.instance.bitManager.OpenBitUI();

            // 2. Bit 오브젝트 자신은 삭제 (또는 비활성화)
            Destroy(gameObject);
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