using UnityEngine;
using UnityEngine.InputSystem;

public class EventBall : MonoBehaviour, IRewardItem
{
    private static readonly int Off = Animator.StringToHash("Off");
    private static readonly int On = Animator.StringToHash("On");

    [Header("Settings")]
    [SerializeField] private GameObject interactEffect;
    [SerializeField] private Material[] outLineMaterial;
    [SerializeField] private SpriteRenderer robotRenderer;
    [SerializeField] private SpriteRenderer screenRenderer;
    [SerializeField] private Animator animator;

    [Header("Collider Settings")]
    [SerializeField] private Collider2D myInteractCollider; // ★ 추가: 이벤트 구슬 본체의 전용 감지 콜라이더

    private bool _isUsed;

    public bool IsCollected => _isUsed;

    private void Start()
    {
        if (interactEffect != null) interactEffect.SetActive(false);

        if (robotRenderer != null && outLineMaterial != null && outLineMaterial.Length > 0)
            robotRenderer.material = outLineMaterial[0];

        // 인스펙터 할당을 깜빡했을 경우를 대비한 방어 코드
        if (myInteractCollider == null) myInteractCollider = GetComponent<Collider2D>();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (EventUIController.Instance != null && EventUIController.Instance.IsOpen) return;

        if (_isUsed)
        {
            if (PlayerFeedbackUI.Instance != null)
                PlayerFeedbackUI.Instance.ShowWarning(3);
            return;
        }

        if (EventManager.Instance == null) return;

        EventManager.Instance.eventOriginPos = transform.position; // Save ball pos
        
        // ★ 추가: 생성되는 아이템을 이 오브젝트의 자식으로 삼기 위해 Transform 전달
        EventManager.Instance.eventOriginTransform = transform; 
        
        EventManager.Instance.GenerateRandomEvent();

        _isUsed = true;

        if (animator != null) animator.SetTrigger(Off);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // ★ 핵심: 자식인 Balance에 닿아서 이벤트가 올라온 경우를 차단
        if (myInteractCollider != null && !myInteractCollider.IsTouching(other)) return;

        if (!_isUsed)
        {
            if (animator != null) animator.SetTrigger(On);
        }

        if (robotRenderer != null && outLineMaterial.Length > 1)
            robotRenderer.material = outLineMaterial[1];

        if (EventUIController.Instance != null && !EventUIController.Instance.IsOpen) ShowInteractEffect(true);

        if (InputStateManager.Instance != null)
            InputStateManager.Instance.Actions.Normal.Interaction.performed += OnInteract;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        if (myInteractCollider != null && myInteractCollider.IsTouching(other)) return;

        if (!_isUsed)
        {
            if (animator != null) animator.SetTrigger(Off);
        }

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