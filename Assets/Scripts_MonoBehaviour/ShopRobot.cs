using UnityEngine;
using UnityEngine.InputSystem;

public class ShopRobot : MonoBehaviour
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
    [SerializeField] private Collider2D myInteractCollider; // ★ 추가: 로봇 본체의 전용 감지 콜라이더

    private ShopUIController _shopUIController;                
    private bool _isPlayerNearby;                              

    private void Start()
    {
        if (GameManager.instance != null)
        {
            _shopUIController = GameManager.instance.shopUIController;
            if (_shopUIController != null)
                _shopUIController.OnShopStateChanged += HandleShopStateChanged;
        }

        if (interactEffect != null) interactEffect.SetActive(false);

        if (robotRenderer != null && outLineMaterial != null && outLineMaterial.Length > 0)
            robotRenderer.material = outLineMaterial[0];

        // 인스펙터 할당을 깜빡했을 경우를 대비한 방어 코드
        if (myInteractCollider == null) myInteractCollider = GetComponent<Collider2D>();
    }

    private void OnDestroy()
    {
        if (_shopUIController != null)
            _shopUIController.OnShopStateChanged -= HandleShopStateChanged;
    }

    private void HandleShopStateChanged(bool isShopOpen)
    {
        if (_isPlayerNearby) ShowInteractEffect(!isShopOpen);
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (_shopUIController == null) return;

        if (_shopUIController.IsOpen) 
            _shopUIController.CloseShop();
        else 
            _shopUIController.OpenShop(this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // ★ 핵심: 자식인 Balance에 닿아서 이벤트가 올라온 경우를 차단 (진짜 내 콜라이더에 닿았는가?)
        if (myInteractCollider != null && !myInteractCollider.IsTouching(other)) return;

        _isPlayerNearby = true;

        if (animator != null) animator.SetTrigger(On);
        if (robotRenderer != null && outLineMaterial.Length > 1) robotRenderer.material = outLineMaterial[1];

        if (_shopUIController != null && !_shopUIController.IsOpen) ShowInteractEffect(true);

        if (InputStateManager.Instance != null)
            InputStateManager.Instance.Actions.Normal.Interaction.performed += OnInteract;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // ★ 핵심: Balance에서 나갔을 때 Exit 이벤트가 발생하는 것을 방어 
        // (플레이어가 로봇 본체 콜라이더 안에 여전히 머물고 있다면 무시)
        if (myInteractCollider != null && myInteractCollider.IsTouching(other)) return;

        _isPlayerNearby = false;

        if (animator != null) animator.SetTrigger(Off);
        if (robotRenderer != null) robotRenderer.material = outLineMaterial[0];

        ShowInteractEffect(false);

        if (InputStateManager.Instance != null)
            InputStateManager.Instance.Actions.Normal.Interaction.performed -= OnInteract;
    }

    private void ShowInteractEffect(bool show)
    {
        if (interactEffect != null) interactEffect.SetActive(show);
    }
}