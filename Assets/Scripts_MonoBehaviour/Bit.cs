using UnityEngine;
using UnityEngine.InputSystem;

// 아티팩트 획득 창을 여는 필드 아이템
public class Bit : MonoBehaviour, IRewardItem 
{
    [SerializeField] private Material[] outLineMaterial; 
    private SpriteRenderer _spriteRenderer;               
    public GameObject keyGuide;                          

    private bool _isPlayerInRange = false;                
    private bool _isCollected = false;                   

    public bool IsCollected => _isCollected;             

    private void Start()
    {
        _spriteRenderer = GetComponentInChildren(typeof(SpriteRenderer)) as SpriteRenderer;
        if (keyGuide != null) keyGuide.SetActive(false);
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (!_isPlayerInRange) return;
        
        if (ArtifactManager.Instance != null && ArtifactManager.Instance.myArtifacts.Count >= 10)
        {
            if (PlayerFeedbackUI.Instance != null)
                PlayerFeedbackUI.Instance.ShowWarning(2);

            _isCollected = true;
            Destroy(gameObject);
            return;
        }

        OpenBitSelection();
    }

    private void OnInteractCombat(InputAction.CallbackContext context)
    {
        if (!_isPlayerInRange) return;

        if (PlayerFeedbackUI.Instance != null)
            PlayerFeedbackUI.Instance.ShowWarning(1);
    }

    private void OpenBitSelection()
    {
        if (GameManager.instance != null && GameManager.instance.bitManager != null)
        {
            if (!GameManager.instance.bitManager.gameObject.activeInHierarchy)
            {
                GameManager.instance.bitManager.gameObject.SetActive(true);
                GameManager.instance.bitManager.OpenBitUI();

                _isCollected = true; 
                Destroy(gameObject); 
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInRange = true;
            _spriteRenderer.material = outLineMaterial[1];
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
            _isPlayerInRange = false;
            _spriteRenderer.material = outLineMaterial[0];
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