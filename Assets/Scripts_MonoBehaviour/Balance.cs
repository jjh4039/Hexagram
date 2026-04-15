using UnityEngine;
using UnityEngine.InputSystem;

// 필드에 배치되는 무게추 아이템
public class Balance : MonoBehaviour
{
    [Header("Item Settings")]
    [SerializeField] private float weightPercent = 5f; // 획득 시 전달할 확률 증가치

    [SerializeField] private Material[] outLineMaterial;
    private SpriteRenderer spriteRenderer;
    public GameObject keyGuide;

    private bool isPlayerInRange = false; // 플레이어 접근 여부

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (keyGuide != null) keyGuide.SetActive(false);
    }

    // 평화 상태에서의 상호작용 (아이템 획득)
    private void OnInteract(InputAction.CallbackContext context)
    {
        if (!isPlayerInRange) return;
        OpenBalanceSelection();
    }

    // 전투 상태에서의 상호작용 시도 (거부 및 피드백 텍스트 출력)
    private void OnInteractCombat(InputAction.CallbackContext context)
    {
        if (!isPlayerInRange) return;
        
        if (PlayerFeedbackUI.Instance != null) 
            PlayerFeedbackUI.Instance.ShowWarning(1); 
    }

    private void OpenBalanceSelection()
    {
        if (GameManager.instance && GameManager.instance.balanceManager)
        {
            if (!GameManager.instance.balanceManager.gameObject.activeInHierarchy)
            {
                GameManager.instance.balanceManager.gameObject.SetActive(true);

                // 매니저를 열 때, 이 아이템이 가진 퍼센트 수치를 함께 넘겨줍니다
                GameManager.instance.balanceManager.OpenBalanceUI(weightPercent);

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

            // 범위에 들어왔을 때만 입력 이벤트 구독 (성능 최적화 및 겹침 방지)
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

            // 범위를 벗어나면 입력 구독 해제
            UnsubscribeInputs();
        }
    }

    private void OnDestroy()
    {
        // 오브젝트가 파괴(획득)될 때 에러를 막기 위해 반드시 구독 해제
        UnsubscribeInputs();
    }

    // 중복 코드를 막기 위한 구독 해제 전용 함수
    private void UnsubscribeInputs()
    {
        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.Actions.Normal.Interaction.performed -= OnInteract;
            InputStateManager.Instance.Actions.Combat.Interaction.performed -= OnInteractCombat;
        }
    }
}