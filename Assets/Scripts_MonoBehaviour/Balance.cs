using UnityEngine;
using UnityEngine.InputSystem;

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

    // ★ 추가됨: 생성 직후 이벤트 매니저가 이 함수를 통해 확률 수치를 덮어씌웁니다.
    public void Setup(float weightValue)
    {
        weightPercent = weightValue;
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

                _isCollected = true;
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