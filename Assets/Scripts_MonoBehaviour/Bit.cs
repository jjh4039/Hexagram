using UnityEngine;
using UnityEngine.InputSystem;

// 아티팩트 획득 창을 여는 필드 아이템
public class Bit : MonoBehaviour
{
    [SerializeField] private Material[] outLineMaterial; // 외곽선 머티리얼 배열
    private SpriteRenderer spriteRenderer;               // 렌더러 컴포넌트
    public GameObject keyGuide;                          // 상호작용 안내 UI

    private bool isPlayerInRange = false;                // 플레이어 접근 여부

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (keyGuide != null) keyGuide.SetActive(false);
    }

    // 평화 상태에서의 상호작용 (아티팩트 창 열기)
    private void OnInteract(InputAction.CallbackContext context)
    {
        if (!isPlayerInRange) return;
        OpenBitSelection();
    }

    // 전투 상태에서의 상호작용 시도 (거부 및 피드백 텍스트 출력)
    private void OnInteractCombat(InputAction.CallbackContext context)
    {
        if (!isPlayerInRange) return;
        
        if (PlayerFeedbackUI.Instance != null) 
            PlayerFeedbackUI.Instance.ShowWarning(1);
    }

    private void OpenBitSelection()
    {
        if (GameManager.instance != null && GameManager.instance.bitManager != null)
        {
            if (!GameManager.instance.bitManager.gameObject.activeInHierarchy)
            {
                GameManager.instance.bitManager.gameObject.SetActive(true);
                GameManager.instance.bitManager.OpenBitUI();

                Destroy(gameObject); // 아이템 소멸
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

            // 범위에 들어왔을 때만 입력 이벤트 구독
            if (InputStateManager.Instance != null)
            {
                InputStateManager.Instance.Actions.Normal.Interaction.performed += OnInteract;
                InputStateManager.Instance.Actions.Combat.Interaction.performed += OnInteractCombat;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            spriteRenderer.material = outLineMaterial[0];
            if (keyGuide != null) keyGuide.SetActive(false);

            UnsubscribeInputs(); // 범위를 벗어나면 구독 해제
        }
    }

    private void OnDestroy()
    {
        // 획득 시 에러를 막기 위해 파괴 전 구독 해제
        UnsubscribeInputs();
    }

    // 중복 방지용 구독 해제 통합 함수
    private void UnsubscribeInputs()
    {
        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.Actions.Normal.Interaction.performed -= OnInteract;
            InputStateManager.Instance.Actions.Combat.Interaction.performed -= OnInteractCombat;
        }
    }
}