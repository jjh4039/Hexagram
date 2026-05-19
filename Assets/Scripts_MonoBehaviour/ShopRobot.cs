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
            _shopUIController.OpenShop(this); // ★ 핵심: 상점 열 때 이 로봇의 정보를 넘김
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

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