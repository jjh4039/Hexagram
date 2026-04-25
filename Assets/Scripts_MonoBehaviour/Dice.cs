using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Dice : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private Dice_UI diceUI;                     // 주사위 연출 UI

    [Header("Data Settings")]
    [SerializeField] public DiceData[] diceList;                 // 주사위의 6개 면 데이터
    [SerializeField] public DiceData defaultData;                // 기본 데이터

    [Header("Probability Settings")]
    [SerializeField] private int[] faceWeights = new int[6] { 100, 100, 100, 100, 100, 100 }; // 각 면의 가중치
    [SerializeField] public float[] displayPercentages = new float[6];                        // 인스펙터 표시용 확률

    [Header("History")]
    public int lastRolledFaceIndex = -1;                         // 마지막으로 나온 면 번호

    private BuffManager _buffManager;

    private void Start()
    {
        _buffManager = GameManager.instance.player.GetComponent<BuffManager>();
        CalculatePercentages();

        if (InputStateManager.Instance != null)
        {
            var actions = InputStateManager.Instance.Actions;
            actions.Normal.Dice.performed += OnDiceInput;
            actions.Combat.Dice.performed += OnDiceInput;
        }
    }

    private void OnDestroy()
    {
        if (InputStateManager.Instance != null)
        {
            var actions = InputStateManager.Instance.Actions;
            actions.Normal.Dice.performed -= OnDiceInput;
            actions.Combat.Dice.performed -= OnDiceInput;
        }
    }

    private void OnDiceInput(InputAction.CallbackContext context)
    {
        if (GameManager.instance == null || GameManager.instance.stats == null) return;

        PlayerStats stats = GameManager.instance.stats;
        
        if (diceUI != null && !diceUI.IsRolling && stats.currentDiceCharge >= 100f)
        {
            RollDice(stats);
        }
    }

    // 수정: 아티팩트 리스트를 사전 추출하여 UI로 전달하도록 변경
    private void RollDice(PlayerStats stats)
    {
        stats.currentDiceCharge -= 100f;
        stats.currentDiceCharge = Mathf.Clamp(stats.currentDiceCharge, 0f, stats.maxDiceCharge);

        if (diceList == null || diceList.Length == 0) return;

        int selectedIndex = GetWeightedRandomIndex();
        bool isConsecutive = (lastRolledFaceIndex == selectedIndex && lastRolledFaceIndex != -1);
        
        lastRolledFaceIndex = selectedIndex;
        DiceData selectedData = diceList[selectedIndex];

        // 1. 발동 예정인 아티팩트 목록 미리 가져오기
        List<ArtifactData> triggeredArtifacts = GetTriggeredArtifacts(selectedIndex, isConsecutive);

        if (diceUI != null && selectedData != null)
        {
            // 2. PlayRollAnimation에 목록 넘기기
            diceUI.PlayRollAnimation(selectedData, selectedIndex, triggeredArtifacts, () =>
            {
                if (GameManager.instance.player)
                {
                    if (_buffManager != null) _buffManager.ApplyDiceBuff(selectedData);
                    ApplyPrecalculatedArtifacts(triggeredArtifacts);
                }
            });
        }
    }

    // 분리 및 신규: 기존 로직에서 발동될 아티팩트를 찾아 리스트로 반환하는 기능만 분리
    private List<ArtifactData> GetTriggeredArtifacts(int rolledIndex, bool isConsecutive)
    {
        List<ArtifactData> list = new List<ArtifactData>();
        if (ArtifactManager.instance == null) return list;

        ConditionType targetCondition = ConditionType.None;

        switch (rolledIndex)
        {
            case 0: targetCondition = ConditionType.OnDiceRoll1; break;
            case 1: targetCondition = ConditionType.OnDiceRoll2; break;
            case 2: targetCondition = ConditionType.OnDiceRoll3; break;
            case 3: targetCondition = ConditionType.OnDiceRoll4; break;
            case 4: targetCondition = ConditionType.OnDiceRoll5; break;
            case 5: targetCondition = ConditionType.OnDiceRoll6; break;
        }

        foreach (var artifact in ArtifactManager.instance.myArtifacts)
        {
            if (artifact.type != ArtifactType.Trigger) continue;

            if (artifact.condition == targetCondition) list.Add(artifact);
            if (isConsecutive && artifact.condition == ConditionType.OnConsecutiveSameDice) list.Add(artifact);
        }
        return list;
    }

    // 분리 및 신규: 미리 계산된 아티팩트들을 실제로 적용하는 기능만 분리
    private void ApplyPrecalculatedArtifacts(List<ArtifactData> artifacts)
    {
        if (_buffManager == null) return;
        foreach (var artifact in artifacts)
        {
            _buffManager.ApplyArtifactBuff(artifact);
            Debug.Log($"주사위 연동 아티팩트 발동: {artifact.artifactName}");
        }
    }

    public void AddChargeFromHit()
    {
        if (GameManager.instance != null && GameManager.instance.stats != null)
            GameManager.instance.stats.AddDiceChargeFromHit();
    }

    public void AddPercentToFace(int faceIndex, float percentIncrease)
    {
        if (faceIndex < 0 || faceIndex >= faceWeights.Length) return;

        int totalWeight = 0;
        for (int i = 0; i < faceWeights.Length; i++) totalWeight += faceWeights[i];
        if (totalWeight == 0) return;

        float currentPercent = (float)faceWeights[faceIndex] / totalWeight;
        float targetPercent = currentPercent + (percentIncrease / 100f);

        if (targetPercent >= 1f) targetPercent = 0.999f;

        float requiredWeightFloat = ((targetPercent * totalWeight) - faceWeights[faceIndex]) / (1f - targetPercent);
        int addedWeight = Mathf.Max(0, Mathf.RoundToInt(requiredWeightFloat));

        faceWeights[faceIndex] += addedWeight;
        CalculatePercentages();
    }

    public float[] GetPredictedPercentages(int faceIndex, float percentIncrease)
    {
        float[] predicted = new float[6];
        int totalWeight = 0;

        for (int i = 0; i < faceWeights.Length; i++) totalWeight += faceWeights[i];
        if (totalWeight == 0) return predicted;

        float currentPercent = (float)faceWeights[faceIndex] / totalWeight;
        float targetPercent = currentPercent + (percentIncrease / 100f);

        if (targetPercent >= 1f) targetPercent = 0.999f;

        float requiredWeightFloat = ((targetPercent * totalWeight) - faceWeights[faceIndex]) / (1f - targetPercent);
        int addedWeight = Mathf.Max(0, Mathf.RoundToInt(requiredWeightFloat));

        int newTotalWeight = totalWeight + addedWeight;

        for (int i = 0; i < faceWeights.Length; i++)
        {
            int tempWeight = faceWeights[i];
            if (i == faceIndex) tempWeight += addedWeight;
            predicted[i] = ((float)tempWeight / newTotalWeight) * 100f;
        }

        return predicted;
    }

    private void CalculatePercentages()
    {
        int totalWeight = 0;
        for (int i = 0; i < faceWeights.Length; i++) totalWeight += faceWeights[i];
        if (totalWeight == 0) return;

        for (int i = 0; i < faceWeights.Length; i++)
        {
            displayPercentages[i] = ((float)faceWeights[i] / totalWeight) * 100f;
        }
    }

    private int GetWeightedRandomIndex()
    {
        int totalWeight = 0;
        for (int i = 0; i < faceWeights.Length; i++) totalWeight += faceWeights[i];

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        for (int i = 0; i < faceWeights.Length; i++)
        {
            currentWeight += faceWeights[i];
            if (randomValue < currentWeight) return i;
        }

        return 0;
    }
}