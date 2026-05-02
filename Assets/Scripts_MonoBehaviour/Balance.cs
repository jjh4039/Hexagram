using UnityEngine;
using UnityEngine.InputSystem;

// 필드에 배치되는 무게추 아이템
public class Balance : MonoBehaviour, IRewardItem // ★ [수정] IRewardItem 인터페이스 상속
{
    [Header("Item Settings")]
    [SerializeField] private float weightPercent = 5f; // 획득 시 전달할 확률 증가치

    [SerializeField] private Material[] outLineMaterial;
    private SpriteRenderer spriteRenderer;
    public GameObject keyGuide;

    private bool isPlayerInRange = false; // 플레이어 접근 여부
    private bool _isCollected = false;    // ★ [추가] 획득 완료 상태

    public bool IsCollected => _isCollected; // ★ [추가] 인터페이스 구현부

    private void Start()
    {
        spriteRenderer = GetComponentInChildren(typeof(SpriteRenderer)) as SpriteRenderer;
        if (keyGuide != null) keyGuide.SetActive(false);
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (!isPlayerInRange) return;
        OpenBalanceSelection();
    }

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
                GameManager.instance.balanceManager.OpenBalanceUI(weightPercent);

                _isCollected = true; // ★ [추가] 파괴 직전 획득 상태 업데이트
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

            UnsubscribeInputs();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeInputs();
    }

    private void UnsubscribeInputs()
    {
        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.Actions.Normal.Interaction.performed -= OnInteract;
            InputStateManager.Instance.Actions.Combat.Interaction.performed -= OnInteractCombat;
        }
    }
}