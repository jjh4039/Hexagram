using UnityEngine;
using UnityEngine.InputSystem;

// 월드에 드랍되어 플레이어와 상호작용 시 주사위 확률 UI를 여는 아이템입니다.
public class Balance : MonoBehaviour, IRewardItem
{
    [Header("Item Settings")]
    [SerializeField] private float weightPercent = 5f;

    [SerializeField] private Material[] outLineMaterial;
    private SpriteRenderer spriteRenderer;
    public GameObject keyGuide;

    private bool isPlayerInRange = false;
    private bool _isCollected = false;

    public bool IsCollected => _isCollected;

    private void Start()
    {
        spriteRenderer = GetComponentInChildren(typeof(SpriteRenderer)) as SpriteRenderer;
        if (keyGuide != null) keyGuide.SetActive(false);
    }

    public void Setup(float weightValue)
    {
        weightPercent = weightValue;
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        // ★ 추가: 이미 상호작용하여 파괴 대기 중이라면 입력 무시
        if (!isPlayerInRange || _isCollected) return;
        OpenBalanceSelection();
    }

    private void OnInteractCombat(InputAction.CallbackContext context)
    {
        if (!isPlayerInRange || _isCollected) return;

        if (PlayerFeedbackUI.Instance != null)
            PlayerFeedbackUI.Instance.ShowWarning(1);
    }

    private void OpenBalanceSelection()
    {
        if (GameManager.instance && GameManager.instance.balanceManager)
        {
            if (!GameManager.instance.balanceManager.gameObject.activeInHierarchy)
            {
                // ★ 추가: 두 번 먹어지지 않도록 플래그를 가장 먼저 잠금
                _isCollected = true;
                
                // 파괴 전에 입력 이벤트를 직접 떼어줍니다. (메모리 누수 및 중복 실행 방어)
                UnsubscribeInputs();

                GameManager.instance.balanceManager.gameObject.SetActive(true);
                GameManager.instance.balanceManager.OpenBalanceUI(weightPercent);

                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ★ 추가: 이미 수집된 상태면 콜라이더 진입 무시
        if (_isCollected) return;

        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (spriteRenderer != null && outLineMaterial.Length > 1) 
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
            
            if (spriteRenderer != null && outLineMaterial.Length > 0) 
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
        if (InputStateManager.Instance != null && InputStateManager.Instance.Actions != null)
        {
            InputStateManager.Instance.Actions.Normal.Interaction.performed -= OnInteract;
            InputStateManager.Instance.Actions.Combat.Interaction.performed -= OnInteractCombat;
        }
    }
}