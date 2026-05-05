using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ScrapPile : MonoBehaviour, IRewardItem
{
    [Header("Scrap Settings")]
    [SerializeField] private GameObject scrapPrefab;     // 생성할 스크랩 프리팹
    [SerializeField] private int minScrapCount = 10;     // 최소 드롭 개수
    [SerializeField] private int maxScrapCount = 20;     // 최대 드롭 개수
    [SerializeField] private int scrapBaseValue = 1;     // 생성될 스크랩의 기본 가치

    [Header("Effects")]
    [SerializeField] private AudioClip burstSound;       // 파괴 시 재생할 사운드
    [SerializeField] private float floatHeight = 1.0f;   // 떠오르는 높이
    [SerializeField] private float burstDuration = 0.5f; // 사라지는 시간

    [SerializeField] private Material[] outLineMaterial; // 아웃라인 머티리얼
    private SpriteRenderer spriteRenderer;               // 자녀 객체의 렌더러
    public GameObject keyGuide;                          // 상호작용 키 가이드

    private bool isPlayerInRange = false;                // 플레이어 범위 진입
    private bool _isCollected = false;                   // 획득 완료 상태

    public bool IsCollected => _isCollected;

    private void Start()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
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
        _isCollected = true; 

        if (burstSound != null && SoundManager.instance != null)
        {
            SoundManager.instance.PlaySFX(burstSound);
        }

        if (scrapPrefab != null)
        {
            int dropCount = Random.Range(minScrapCount, maxScrapCount + 1);
            for (int i = 0; i < dropCount; i++)
            {
                GameObject obj = Instantiate(scrapPrefab, transform.position, Quaternion.identity);
                
                // 생성된 스크랩에 인스펙터에서 설정한 기본 가치를 전달합니다.
                if (obj.TryGetComponent<Scrap>(out Scrap scrap))
                {
                    scrap.SetValue(scrapBaseValue);
                }
            }
        }

        if (keyGuide != null) keyGuide.SetActive(false);
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        UnsubscribeInputs();

        StartCoroutine(BurstAnimationRoutine());
    }

    private IEnumerator BurstAnimationRoutine()
    {
        if (spriteRenderer == null)
        {
            Destroy(gameObject);
            yield break;
        }

        Transform visualTransform = spriteRenderer.transform;
        Vector3 startPos = visualTransform.localPosition;
        Vector3 endPos = startPos + new Vector3(0, floatHeight, 0);
        Vector3 startScale = visualTransform.localScale;
        
        Color startColor = spriteRenderer.color;

        float timer = 0f;
        while (timer < burstDuration)
        {
            timer += Time.deltaTime;
            float t = timer / burstDuration;

            visualTransform.localPosition = Vector3.Lerp(startPos, endPos, t);
            
            visualTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            
            Color c = startColor;
            c.a = Mathf.Lerp(startColor.a, 0f, t);
            spriteRenderer.color = c;

            yield return null;
        }

        Destroy(gameObject); 
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !_isCollected)
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