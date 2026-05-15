using UnityEngine;
using UnityEngine.InputSystem;

public class Statue : MonoBehaviour
{
    [Header("Settings")]
    public GameObject interactEffect;          // 상호작용 가능 안내 UI
    public Material[] outLineMaterial;         // 외곽선 머티리얼 배열
    public SpriteRenderer statueWomanRenderer; // 석상 스프라이트 렌더러
    public Transform arrowTargetPos;           // 화살표가 가리킬 정확한 목표 위치

    [Header("Tutorial Cutscene")]
    public bool isTutorial = false;
    public Transform cutsceneCameraTarget;    

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
        if (!isPlayerNearby) return;

        // ★ [수정됨] 튜토리얼 모드일 경우 지정된 카메라 타겟으로 컷신 실행
        if (isTutorial)
        {
            if (TutorialManager.Instance != null && !TutorialManager.Instance.IsCutsceneActive)
            {
                // 컷신 시작 전, 상호작용 UI와 아웃라인을 끕니다.
                if (interactEffect != null) interactEffect.SetActive(false);
                if (statueWomanRenderer != null && outLineMaterial.Length > 0)
                    statueWomanRenderer.material = outLineMaterial[0];

                // 카메라 타겟이 비어있으면 석상 자신을 비추도록 방어 코드 추가
                Transform targetTransform = cutsceneCameraTarget != null ? cutsceneCameraTarget : this.transform;

                TutorialManager.Instance.StartFinalCutscene(targetTransform);
            }
            return;
        }

        if (!isActivated) return;

        bool isRewardExist = targetReward != null && (targetReward as Object) != null;
        bool hasUncollectedFloorReward = isRewardExist && !targetReward.IsCollected;

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

            // ★ [수정됨] 활성화되어 있거나 튜토리얼용 석상일 때만 효과 켜기
            if (isActivated || isTutorial)
            {
                // 컷신 진행 중에는 효과를 켜지 않도록 방어 코드 추가
                if (TutorialManager.Instance != null && TutorialManager.Instance.IsCutsceneActive) return;

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