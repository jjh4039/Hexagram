using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class EventSelectionData
{
    public RiskData selectedRisk;
    public RewardData selectedReward;

    public bool IsValid()
    {
        return selectedRisk != null && selectedReward != null;
    }

    public float GetRiskValue(int stage)
    {
        if (selectedRisk == null) return 0f;
        return selectedRisk.GetValue(stage);
    }

    public float GetRewardValue(int stage)
    {
        if (selectedReward == null) return 0f;
        return selectedReward.GetValue(stage);
    }

    public string GetRiskText(int stage)
    {
        if (selectedRisk == null) return "리스크 없음";
        return $"{selectedRisk.riskName} : {selectedRisk.GetValueText(stage)}";
    }

    public string GetRewardText(int stage)
    {
        if (selectedReward == null) return "보상 없음";
        return $"{selectedReward.rewardName} : {selectedReward.GetValueText(stage)}";
    }
}

public class EventManager : MonoBehaviour
{
    public static EventManager Instance;                           // 전역 접근용 인스턴스

    [Header("Event Data Pool")]
    [SerializeField] private List<RiskData> riskDataList = new List<RiskData>();     // 리스크 데이터 풀
    [SerializeField] private List<RewardData> rewardDataList = new List<RewardData>(); // 보상 데이터 풀

    [Header("Current Selection")]
    [SerializeField] private EventSelectionData currentEventSelection = new EventSelectionData(); // 현재 선택된 데이터

    [Header("References")]
    [SerializeField] private EventUIController eventUIController;  // UI 컨트롤러 참조

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugInput = true;         // 디버그 키 활성화 여부

    public EventSelectionData CurrentEventSelection => currentEventSelection; // 외부 읽기용 프로퍼티

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // 평상시 상태에서만 디버그용 O키 입력 활성화
        if (enableDebugInput && InputStateManager.Instance != null)
        {
            InputStateManager.Instance.Actions.Normal.DebugEvent.performed += OnDebugEventTrigger;
        }
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위한 이벤트 구독 해제
        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.Actions.Normal.DebugEvent.performed -= OnDebugEventTrigger;
        }
    }

    // 디버그 키 입력 시 호출되는 콜백
    private void OnDebugEventTrigger(InputAction.CallbackContext context)
    {
        ToggleRandomEventUI();
    }

    // UI 상태를 확인하여 랜덤 이벤트를 생성하거나 무시합니다
    private void ToggleRandomEventUI()
    {
        if (eventUIController == null) return;
        if (eventUIController.IsOpen) return;                      // 취소 불가 이벤트이므로 열려있으면 무시

        GenerateRandomEvent();
    }

    // 리스크와 보상을 무작위로 추첨하고 UI를 엽니다
    public void GenerateRandomEvent()
    {
        RiskData randomRisk = GetRandomRiskData();
        RewardData randomReward = GetRandomRewardData();

        currentEventSelection = new EventSelectionData { selectedRisk = randomRisk, selectedReward = randomReward };

        if (!currentEventSelection.IsValid()) return;              // 데이터가 유효하지 않으면 중단

        eventUIController.OpenEvent();                             // 이벤트 창 오픈
    }

    private RiskData GetRandomRiskData()
    {
        if (riskDataList == null || riskDataList.Count == 0) return null;
        return riskDataList[UnityEngine.Random.Range(0, riskDataList.Count)];
    }

    private RewardData GetRandomRewardData()
    {
        if (rewardDataList == null || rewardDataList.Count == 0) return null;
        return rewardDataList[UnityEngine.Random.Range(0, rewardDataList.Count)];
    }

    public List<RiskData> GetRiskList() => riskDataList;           // 전체 리스크 리스트 반환
    public List<RewardData> GetRewardList() => rewardDataList;     // 전체 보상 리스트 반환
}