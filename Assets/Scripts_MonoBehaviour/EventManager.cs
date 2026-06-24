using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[Serializable]
public class EventSelectionData
{
    public RiskData selectedRisk;
    public RewardData selectedReward;

    public bool IsValid()
    {
        return selectedRisk && selectedReward;
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

    [Header("Event Data Pool")] 
    [SerializeField] private List<RiskData> riskDataList = new List<RiskData>();
    [SerializeField] private List<RewardData> rewardDataList = new List<RewardData>();

    [Header("Current Selection")] 
    [SerializeField] private EventSelectionData currentEventSelection = new EventSelectionData();

    [Header("References")] 
    [SerializeField] private EventUIController eventUIController;

    [Header("Reward Prefabs")] 
    [SerializeField] private GameObject balancePrefab;

    [Header("Activation Feedback")] 
    [SerializeField] private TextMeshProUGUI activationText;
    [SerializeField] private float textFadeDuration = 0.2f;
    [SerializeField] private float textDisplayDuration = 1f;

    public EventSelectionData CurrentEventSelection => currentEventSelection;

    public Vector3 eventOriginPos;
    
    [HideInInspector] public Transform eventOriginTransform;

    private Coroutine _activationTextRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (activationText != null)
        {
            Color c = activationText.color;
            c.a = 0f;
            activationText.color = c;
            activationText.gameObject.SetActive(false);
        }
    }

    public void GenerateRandomEvent()
    {
        RiskData randomRisk = GetRandomRiskData();
        RewardData randomReward = GetRandomRewardData();

        currentEventSelection = new EventSelectionData { selectedRisk = randomRisk, selectedReward = randomReward };

        if (!currentEventSelection.IsValid()) return;

        eventUIController.OpenEvent();
    }

    public void ApplyCurrentEvent(int destinyIndex)
    {
        if (!currentEventSelection.IsValid()) return;

        int intensityLevel = destinyIndex + 1;

        ApplyRisk(currentEventSelection.selectedRisk, intensityLevel);
        ApplyReward(currentEventSelection.selectedReward, intensityLevel);
        
        // GameAnalytics 6번 정보 전송 (이벤트)
        if (AnalyticsManager.Instance != null)
        {
            string rType = currentEventSelection.selectedRisk.riskType.ToString();
            string rewType = currentEventSelection.selectedReward.rewardType.ToString();
            AnalyticsManager.Instance.LogEventSelection(rType, rewType, intensityLevel);
        }

        if (activationText != null)
        {
            if (_activationTextRoutine != null) StopCoroutine(_activationTextRoutine);
            _activationTextRoutine = StartCoroutine(Co_ShowActivationText());
        }

        Debug.Log($"이벤트 보상 및 리스크 적용 완료! (강도: {intensityLevel}단계)");
    }

    private IEnumerator Co_ShowActivationText()
    {
        activationText.text = "운명의 저울이 기울었습니다...";
        activationText.gameObject.SetActive(true);
        Color c = activationText.color;

        float t = 0f;
        while (t < textFadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / textFadeDuration);
            activationText.color = c;
            yield return null;
        }

        yield return new WaitForSeconds(textDisplayDuration);

        t = 0f;
        while (t < textFadeDuration)
        {
            t += Time.deltaTime;
            c.a = 1f - Mathf.Clamp01(t / textFadeDuration);
            activationText.color = c;
            yield return null;
        }

        activationText.gameObject.SetActive(false);
        _activationTextRoutine = null;
    }

    private void ApplyRisk(RiskData risk, int stage)
    {
        float value = risk.GetValue(stage);
        PlayerStats stats = GameManager.instance.stats;
        BuffManager buffManager = GameManager.instance.player.buffManager;

        switch (risk.riskType)
        {
            case RiskType.CurrentHpCost:
                if (value <= 0f)
                {
                    Debug.Log("[디버프 무시] 체력 소모가 0%이므로 피해를 입지 않습니다.");
                    break;
                }

                int hpCost = Mathf.RoundToInt(stats.currentHealth * (value / 100f));
                if (hpCost > 0)
                {
                    stats.TakeDamage(hpCost);
                }

                break;

            case RiskType.DiceChargeReduction:
                buffManager.ApplyStageDebuff(StageDebuffType.DiceEffectHalf, 0f, (int)value, risk.symbolSprite);
                break;

            case RiskType.NextStageDamageIncrease:
                buffManager.ApplyStageDebuff(StageDebuffType.TakeMoreDamage, value, 1, risk.symbolSprite);
                break;

            case RiskType.NoHealStages:
                buffManager.ApplyStageDebuff(StageDebuffType.CannotHeal, 0f, (int)value, risk.symbolSprite);
                break;

            case RiskType.BossHealthIncrease:
                GameManager.instance.eventBossHealthMultiplier += (value / 100f);
                break;
        }
    }

    private void ApplyReward(RewardData reward, int stage)
    {
        float value = reward.GetValue(stage);
        PlayerStats stats = GameManager.instance.stats;

        switch (reward.rewardType)
        {
            case RewardType.MaxHpUp:
                stats.ApplyPercentMaxHealth(value / 100f);
                break;

            case RewardType.Scrap:
                GameManager.instance.AddScrap(Mathf.RoundToInt(value));
                break;

            case RewardType.Artifact:
                ArtifactGrade targetGrade = ArtifactGrade.Common;
                if (stage == 1) targetGrade = ArtifactGrade.Common;
                else if (stage == 2) targetGrade = ArtifactGrade.Rare;
                else if (stage >= 3) targetGrade = ArtifactGrade.Epic;

                if (ArtifactManager.Instance != null)
                {
                    ArtifactManager.Instance.GiveRandomArtifactByGrade(targetGrade);

                    if (stats != null)
                    {
                        stats.SpawnDamageText("ARTIFACT!", new Color(0.6f, 0.87f, 1f), 4f);
                    }
                }

                break;

            case RewardType.DiceFaceChanceUp:
                if (balancePrefab != null)
                {
                    Vector3 spawnPos = eventOriginPos + new Vector3(0, -2.5f, 0);
                    
                    GameObject balanceObj = Instantiate(balancePrefab, spawnPos, Quaternion.identity);
                    
                    if (eventOriginTransform != null)
                    {
                        balanceObj.transform.SetParent(eventOriginTransform, true);
                    }

                    Balance balanceScript = balanceObj.GetComponent<Balance>();
                    if (balanceScript != null)
                    {
                        balanceScript.Setup(value);
                    }

                    Debug.Log($"Balance item spawned at {spawnPos}");
                }
                else
                {
                    Debug.LogWarning("Balance prefab is missing");
                }

                break;

            case RewardType.ModuleEnhanceChoice:
                if (StageMessageUI.instance != null)
                {
                    int rewardCount = Mathf.Max(1, Mathf.RoundToInt(value));
                    StageMessageUI.instance.ShowModuleRewardOnly(rewardCount);
                }
                else
                {
                    Debug.LogWarning("StageMessageUI instance is missing");
                }

                break;
        }
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

    public List<RiskData> GetRiskList() => riskDataList;
    public List<RewardData> GetRewardList() => rewardDataList;
}