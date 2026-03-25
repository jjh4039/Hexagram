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
    [SerializeField] private Sprite offScreenSprite;
    [SerializeField] private Sprite onScreenSprite;
    [SerializeField] private Animator animator;

    private bool _isPlayerNearby;
    private ShopUIController _shopUIController;

    private void Start()
    {
        if (GameManager.instance != null)
            _shopUIController = GameManager.instance.shopUIController;

        if (interactEffect != null)
            interactEffect.SetActive(false);

        if (robotRenderer != null && outLineMaterial != null && outLineMaterial.Length > 0)
            robotRenderer.material = outLineMaterial[0];

        if (screenRenderer != null && offScreenSprite != null)
            screenRenderer.sprite = offScreenSprite;
    }

    private void Update()
    {
        if (!_isPlayerNearby)
            return;

        if (_shopUIController == null)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (_shopUIController.IsOpen)
            {
                _shopUIController.CloseShop();
                ShowInteractEffect(true);
            }
            else
            {
                _shopUIController.OpenShop();
                ShowInteractEffect(false);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        _isPlayerNearby = true;

        if (animator != null)
            animator.SetTrigger(On);

        if (robotRenderer != null && outLineMaterial != null && outLineMaterial.Length > 1)
            robotRenderer.material = outLineMaterial[1];

        if (screenRenderer != null && onScreenSprite != null)
            screenRenderer.sprite = onScreenSprite;

        if (_shopUIController != null && _shopUIController.IsOpen)
            ShowInteractEffect(false);
        else
            ShowInteractEffect(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        _isPlayerNearby = false;

        if (animator != null)
            animator.SetTrigger(Off);

        ShowInteractEffect(false);

        if (robotRenderer != null && outLineMaterial != null && outLineMaterial.Length > 0)
            robotRenderer.material = outLineMaterial[0];

        if (screenRenderer != null && offScreenSprite != null)
            screenRenderer.sprite = offScreenSprite;
    }

    private void ShowInteractEffect(bool show)
    {
        if (interactEffect != null)
            interactEffect.SetActive(show);
    }
}