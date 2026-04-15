using UnityEngine;
using UnityEngine.InputSystem;

// 상점 UI를 열고 닫는 상호작용 로봇 오브젝트
public class ShopRobot : MonoBehaviour
{
    private static readonly int Off = Animator.StringToHash("Off");
    private static readonly int On = Animator.StringToHash("On");

    [Header("Settings")]
    [SerializeField] private GameObject interactEffect;        // 상호작용 안내 이펙트 (F키 등)
    [SerializeField] private Material[] outLineMaterial;       // 외곽선 머티리얼 (0:꺼짐, 1:켜짐)
    [SerializeField] private SpriteRenderer robotRenderer;     // 로봇 본체 렌더러
    [SerializeField] private SpriteRenderer screenRenderer;    // 로봇 화면 렌더러
    [SerializeField] private Sprite offScreenSprite;          // 비활성 화면 스프라이트
    [SerializeField] private Sprite onScreenSprite;           // 활성 화면 스프라이트
    [SerializeField] private Animator animator;                // 로봇 애니메이터

    private ShopUIController _shopUIController;                // 상점 UI 컨트롤러 참조
    private bool _isPlayerNearby;                              // 플레이어가 근처에 있는지 확인

    private void Start()
    {
        if (GameManager.instance != null)
        {
            _shopUIController = GameManager.instance.shopUIController;
            
            // ★ [핵심 추가] 상점 UI의 상태 변화 이벤트를 구독합니다.
            if (_shopUIController != null)
                _shopUIController.OnShopStateChanged += HandleShopStateChanged;
        }

        if (interactEffect != null) interactEffect.SetActive(false);

        if (robotRenderer != null && outLineMaterial != null && outLineMaterial.Length > 0)
            robotRenderer.material = outLineMaterial[0];

        if (screenRenderer != null && offScreenSprite != null)
            screenRenderer.sprite = offScreenSprite;
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지
        if (_shopUIController != null)
            _shopUIController.OnShopStateChanged -= HandleShopStateChanged;
    }

    // UI가 열리거나 닫힐 때 자동으로 호출되는 함수
    private void HandleShopStateChanged(bool isShopOpen)
    {
        // 플레이어가 근처에 있을 때만, 상점이 닫히면 가이드를 켜고 열리면 끕니다.
        if (_isPlayerNearby)
        {
            ShowInteractEffect(!isShopOpen);
        }
    }

    // F키(상호작용) 입력 시 호출되는 콜백
    private void OnInteract(InputAction.CallbackContext context)
    {
        if (_shopUIController == null) return;

        if (_shopUIController.IsOpen) _shopUIController.CloseShop();
        else _shopUIController.OpenShop();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _isPlayerNearby = true;

        if (animator != null) animator.SetTrigger(On);
        if (robotRenderer != null && outLineMaterial.Length > 1) robotRenderer.material = outLineMaterial[1];
        if (screenRenderer != null) screenRenderer.sprite = onScreenSprite;

        if (_shopUIController != null && !_shopUIController.IsOpen) ShowInteractEffect(true);

        if (InputStateManager.Instance != null)
            InputStateManager.Instance.Actions.Normal.Interaction.performed += OnInteract;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _isPlayerNearby = false;

        if (animator != null) animator.SetTrigger(Off);
        if (robotRenderer != null) robotRenderer.material = outLineMaterial[0];
        if (screenRenderer != null) screenRenderer.sprite = offScreenSprite;

        ShowInteractEffect(false);

        if (InputStateManager.Instance != null)
            InputStateManager.Instance.Actions.Normal.Interaction.performed -= OnInteract;
    }

    private void ShowInteractEffect(bool show)
    {
        if (interactEffect != null) interactEffect.SetActive(show);
    }
}