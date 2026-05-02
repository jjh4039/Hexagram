using UnityEngine;
using UnityEngine.InputSystem;

public class Statue : MonoBehaviour
{
    [Header("Settings")]
    public GameObject interactEffect;          // 상호작용 가능 안내 UI
    public Material[] outLineMaterial;         // 외곽선 머티리얼 배열
    public SpriteRenderer statueWomanRenderer; // 석상 스프라이트 렌더러

    private bool isPlayerNearby = false;       // 플레이어 접근 여부
    private bool isActivated = false;          // 활성화 상태 여부
    private IRewardItem targetReward;          // 확인해야 할 보상 아이템

    private void Start()
    {
        if (interactEffect != null) interactEffect.SetActive(false);
        if (statueWomanRenderer != null && outLineMaterial.Length > 0)
            statueWomanRenderer.material = outLineMaterial[0];
    }

    public void ActivateStatue(IRewardItem reward = null)
    {
        isActivated = true;
        targetReward = reward;
        
        if (isPlayerNearby)
        {
            if (interactEffect != null) interactEffect.SetActive(true);
            if (statueWomanRenderer != null && outLineMaterial.Length > 1) 
                statueWomanRenderer.material = outLineMaterial[1];
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (!isActivated || !isPlayerNearby) return;

        bool isRewardExist = targetReward != null && (targetReward as Object) != null;
        bool hasUncollectedFloorReward = isRewardExist && !targetReward.IsCollected;
        
        // ★ [수정됨] 바닥의 특수 보상을 안 먹었거나, 모듈 강화(기본/아이템)가 아직 떠있거나 대기열이 남았을 때 잠금
        bool hasPendingModuleReward = StageMessageUI.instance != null && !StageMessageUI.instance.IsRewardQueueEmpty;

        if (hasUncollectedFloorReward || hasPendingModuleReward)
        {
            if (PlayerFeedbackUI.Instance != null)
                PlayerFeedbackUI.Instance.ShowWarning(4);
            return;
        }

        if (GameManager.instance != null && GameManager.instance.mapManager != null)
        {
            GameManager.instance.mapManager.ToggleMap();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            
            if (isActivated)
            {
                if (interactEffect != null) interactEffect.SetActive(true);
                if (statueWomanRenderer != null && outLineMaterial.Length > 1) 
                    statueWomanRenderer.material = outLineMaterial[1];
            }

            if (InputStateManager.Instance != null)
            {
                InputStateManager.Instance.Actions.Normal.Interaction.performed += OnInteract;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            
            if (interactEffect != null) interactEffect.SetActive(false);
            if (statueWomanRenderer != null && outLineMaterial.Length > 0) 
                statueWomanRenderer.material = outLineMaterial[0];

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
        }
    }
}