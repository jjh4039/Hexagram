using UnityEngine;
using UnityEngine.InputSystem;

// 아티팩트 획득 창을 여는 필드 아이템
public class Bit : MonoBehaviour, IRewardItem // ★ [수정] IRewardItem 인터페이스 상속
{
    [SerializeField] private Material[] outLineMaterial; // 외곽선 머티리얼 배열
    private SpriteRenderer _spriteRenderer;               // 렌더러 컴포넌트
    public GameObject keyGuide;                          // 상호작용 안내 UI

    private bool _isPlayerInRange = false;                // 플레이어 접근 여부
    private bool _isCollected = false;                   // ★ [추가] 획득 완료 상태

    public bool IsCollected => _isCollected;             // ★ [추가] 인터페이스 구현부

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

                _isCollected = true; // ★ [추가] 파괴 직전 획득 상태 업데이트
                Destroy(gameObject); // 아이템 소멸
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
        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.Actions.Normal.Interaction.performed -= OnInteract;
            InputStateManager.Instance.Actions.Combat.Interaction.performed -= OnInteractCombat;
        }
    }
}