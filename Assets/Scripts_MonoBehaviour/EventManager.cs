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
    public static EventManager Instance;

    [Header("이벤트 데이터 풀")]
    [SerializeField] private List<RiskData> riskDataList = new List<RiskData>();
    [SerializeField] private List<RewardData> rewardDataList = new List<RewardData>();

    [Header("현재 선택된 이벤트")]
    [SerializeField] private EventSelectionData currentEventSelection = new EventSelectionData();

    [Header("연결")]
    [SerializeField] private EventUIController eventUIController;

    [Header("디버그 입력")]
    [SerializeField] private bool enableDebugInput = true;

    public EventSelectionData CurrentEventSelection => currentEventSelection;

    private Keyboard keyboard;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        keyboard = Keyboard.current;
    }

    private void Update()
    {
        if (!enableDebugInput)
            return;

        if (Keyboard.current == null)
            return;

        keyboard = Keyboard.current;

        if (keyboard.oKey.wasPressedThisFrame)
        {
            ToggleRandomEventUI();
        }
    }

    private void ToggleRandomEventUI()
    {
        if (eventUIController == null)
        {
            Debug.LogWarning("EventUIController가 연결되지 않았습니다.");
            return;
        }

        if (eventUIController.IsOpen)
        {
            eventUIController.CloseEvent();
            return;
        }

        GenerateRandomEvent();
    }

    public void GenerateRandomEvent()
    {
        RiskData randomRisk = GetRandomRiskData();
        RewardData randomReward = GetRandomRewardData();

        currentEventSelection = new EventSelectionData
        {
            selectedRisk = randomRisk,
            selectedReward = randomReward
        };

        if (!currentEventSelection.IsValid())
        {
            Debug.LogWarning("이벤트 생성 실패: 리스크 또는 보상 데이터가 비어 있습니다.");
            return;
        }

        Debug.Log("=== 이벤트 생성 완료 ===");
        Debug.Log($"리스크: {currentEventSelection.selectedRisk.riskName}");
        Debug.Log($"보상: {currentEventSelection.selectedReward.rewardName}");

        eventUIController.OpenEvent();
    }

    private RiskData GetRandomRiskData()
    {
        if (riskDataList == null || riskDataList.Count == 0)
        {
            Debug.LogWarning("RiskData 리스트가 비어 있습니다.");
            return null;
        }

        int index = UnityEngine.Random.Range(0, riskDataList.Count);
        return riskDataList[index];
    }

    private RewardData GetRandomRewardData()
    {
        if (rewardDataList == null || rewardDataList.Count == 0)
        {
            Debug.LogWarning("RewardData 리스트가 비어 있습니다.");
            return null;
        }

        int index = UnityEngine.Random.Range(0, rewardDataList.Count);
        return rewardDataList[index];
    }

    public List<RiskData> GetRiskList()
    {
        return riskDataList;
    }

    public List<RewardData> GetRewardList()
    {
        return rewardDataList;
    }
}