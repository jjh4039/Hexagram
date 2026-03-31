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

        if (keyboard.rKey.wasPressedThisFrame)
        {
            GenerateRandomEvent();
        }

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            ConfirmStageSelection(1);
        }

        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            ConfirmStageSelection(2);
        }

        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            ConfirmStageSelection(3);
        }
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
        Debug.Log($"1단계 -> {currentEventSelection.GetRiskText(1)} / {currentEventSelection.GetRewardText(1)}");
        Debug.Log($"2단계 -> {currentEventSelection.GetRiskText(2)} / {currentEventSelection.GetRewardText(2)}");
        Debug.Log($"3단계 -> {currentEventSelection.GetRiskText(3)} / {currentEventSelection.GetRewardText(3)}");
    }

    public void ConfirmStageSelection(int stage)
    {
        if (!currentEventSelection.IsValid())
        {
            Debug.LogWarning("현재 유효한 이벤트가 없습니다. 먼저 이벤트를 생성하세요.");
            return;
        }

        if (stage < 1 || stage > 3)
        {
            Debug.LogWarning($"잘못된 단계 선택: {stage}");
            return;
        }

        float riskValue = currentEventSelection.GetRiskValue(stage);
        float rewardValue = currentEventSelection.GetRewardValue(stage);

        Debug.Log("=== 단계 선택 완료 ===");
        Debug.Log($"선택 단계: {stage}");
        Debug.Log($"리스크 -> {currentEventSelection.GetRiskText(stage)}");
        Debug.Log($"보상 -> {currentEventSelection.GetRewardText(stage)}");

        ApplyRisk(currentEventSelection.selectedRisk, stage, riskValue);
        ApplyReward(currentEventSelection.selectedReward, stage, rewardValue);
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

    private void ApplyRisk(RiskData riskData, int stage, float value)
    {
        if (riskData == null)
            return;

        switch (riskData.riskType)
        {
            case RiskType.BossHealthIncrease:
                Debug.Log($"[리스크 적용] 해당 스테이지 보스 체력 {value}% 증가");
                break;

            case RiskType.NextStageDamageIncrease:
                Debug.Log($"[리스크 적용] 다음 전투 스테이지 받는 피해 {value}% 증가");
                break;

            case RiskType.NoHealStages:
                Debug.Log($"[리스크 적용] {value:0} 스테이지 동안 회복 불가");
                break;

            case RiskType.DiceChargeReduction:
                Debug.Log($"[리스크 적용] 주사위 모듈 충전량 {value}% 감소");
                break;

            case RiskType.CurrentHpCost:
                Debug.Log($"[리스크 적용] 현재 체력 {value}% 소비");
                break;

            default:
                Debug.LogWarning($"처리되지 않은 리스크 타입: {riskData.riskType}");
                break;
        }
    }

    private void ApplyReward(RewardData rewardData, int stage, float value)
    {
        if (rewardData == null)
            return;

        switch (rewardData.rewardType)
        {
            case RewardType.Scrap:
                Debug.Log($"[보상 적용] 스크랩 {value:0} 획득");
                break;

            case RewardType.DiceFaceChanceUp:
                Debug.Log($"[보상 적용] 특정 면 확률 +{value:0}% 증가");
                break;

            case RewardType.Artifact:
                Debug.Log($"[보상 적용] {rewardData.GetValueText(stage)} 아티팩트 획득");
                break;

            case RewardType.MaxHpUp:
                Debug.Log($"[보상 적용] 최대 체력 {value:0} 증가");
                break;

            case RewardType.ModuleEnhanceChoice:
                Debug.Log($"[보상 적용] 모듈 강화 선택지 {value:0}회 부여");
                break;

            default:
                Debug.LogWarning($"처리되지 않은 보상 타입: {rewardData.rewardType}");
                break;
        }
    }
}
