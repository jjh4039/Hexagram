using UnityEngine;
using UnityEngine.InputSystem;

public class ScrapPile : MonoBehaviour, IRewardItem
{
    [Header("Scrap Settings")]
    [SerializeField] private GameObject scrapPrefab; // 생성할 스크랩 프리팹
    [SerializeField] private int minScrapCount = 10; // 최소 드롭 개수
    [SerializeField] private int maxScrapCount = 20; // 최대 드롭 개수

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

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (!isPlayerInRange || _isCollected) return;

        BurstScraps();
    }

    private void OnInteractCombat(InputAction.CallbackContext context)
    {
        if (!isPlayerInRange) return;

        if (PlayerFeedbackUI.Instance != null)
            PlayerFeedbackUI.Instance.ShowWarning(1);
    }

    private void BurstScraps()
    {
        _isCollected = true; // 파괴 직전 획득 상태 업데이트

        if (scrapPrefab != null)
        {
            int dropCount = Random.Range(minScrapCount, maxScrapCount + 1);
            for (int i = 0; i < dropCount; i++)
            {
                Instantiate(scrapPrefab, transform.position, Quaternion.identity);
            }
        }

        Destroy(gameObject); // 스크랩 더미 소멸
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
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
        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.Actions.Normal.Interaction.performed -= OnInteract;
            InputStateManager.Instance.Actions.Combat.Interaction.performed -= OnInteractCombat;
        }
    }
}