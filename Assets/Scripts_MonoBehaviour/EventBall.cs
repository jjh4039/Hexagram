using UnityEngine;
using UnityEngine.InputSystem;

public class EventBall : MonoBehaviour
{
    private static readonly int Off = Animator.StringToHash("Off");
    private static readonly int On = Animator.StringToHash("On");

    [Header("Settings")]
    [SerializeField] private GameObject interactEffect;        // 상호작용 안내 이펙트
    [SerializeField] private Material[] outLineMaterial;       // 외곽선 머티리얼
    [SerializeField] private SpriteRenderer robotRenderer;     // 로봇 본체 렌더러
    [SerializeField] private SpriteRenderer screenRenderer;    // 로봇 화면 렌더러
    [SerializeField] private Animator animator;                // 로봇 애니메이터
    [SerializeField] private EventUIController uiController;   // 연결할 이벤트 UI 컨트롤러

    private bool _isUsed;                                      // 사용 완료 여부

    private void Start()
    {
        if (uiController == null)
        {
            uiController = FindFirstObjectByType<EventUIController>();
        }

        if (interactEffect != null) interactEffect.SetActive(false);

        if (robotRenderer != null && outLineMaterial != null && outLineMaterial.Length > 0)
            robotRenderer.material = outLineMaterial[0];
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (uiController != null && uiController.IsOpen) return;

        if (_isUsed)
        {
            if (PlayerFeedbackUI.Instance != null)
                PlayerFeedbackUI.Instance.ShowWarning(3);
            return;
        }

        if (EventManager.Instance == null || uiController == null) return;

        EventManager.Instance.GenerateRandomEvent();
        
        _isUsed = true;

        if (animator != null) animator.SetTrigger(Off);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        if (!_isUsed)
        {
            if (animator != null) animator.SetTrigger(On);
        }

        // 외곽선은 사용 여부와 무관하게 항상 표시되도록 밖으로 분리
        if (robotRenderer != null && outLineMaterial.Length > 1) 
            robotRenderer.material = outLineMaterial[1];

        if (uiController != null && !uiController.IsOpen) ShowInteractEffect(true);

        if (InputStateManager.Instance != null)
            InputStateManager.Instance.Actions.Normal.Interaction.performed += OnInteract;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        if (!_isUsed)
        {
            if (animator != null) animator.SetTrigger(Off);
        }

        // 벗어나면 외곽선은 항상 꺼지도록 처리
        if (robotRenderer != null && outLineMaterial.Length > 0) 
            robotRenderer.material = outLineMaterial[0];

        ShowInteractEffect(false);

        if (InputStateManager.Instance != null)
            InputStateManager.Instance.Actions.Normal.Interaction.performed -= OnInteract;
    }

    private void ShowInteractEffect(bool show)
    {
        if (interactEffect != null) interactEffect.SetActive(show);
    }
}