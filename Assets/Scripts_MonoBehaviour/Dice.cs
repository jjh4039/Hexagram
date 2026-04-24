using UnityEngine;
using UnityEngine.InputSystem;

// 주사위 굴리기와 확률 가중치를 관리하는 스크립트
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
        CalculatePercentages();                                  // 초기 확률 계산

        // 입력 시스템 이벤트 연결 (E키 등)
        if (InputStateManager.Instance != null)
        {
            var actions = InputStateManager.Instance.Actions;
            actions.Normal.Dice.performed += OnDiceInput;
            actions.Combat.Dice.performed += OnDiceInput;
        }
    }

    private void OnDestroy()
    {
        // 메모리 해제를 위한 이벤트 구독 해제
        if (InputStateManager.Instance != null)
        {
            var actions = InputStateManager.Instance.Actions;
            actions.Normal.Dice.performed -= OnDiceInput;
            actions.Combat.Dice.performed -= OnDiceInput;
        }
    }

    // 주사위 키 입력 시 호출되는 콜백
    private void OnDiceInput(InputAction.CallbackContext context)
    {
        if (GameManager.instance == null || GameManager.instance.stats == null) return;

        PlayerStats stats = GameManager.instance.stats;
        
        // 연출 중이 아니고 차지가 가득 찼을 때만 실행
        if (diceUI != null && !diceUI.IsRolling && stats.currentDiceCharge >= 100f)
        {
            RollDice(stats);
        }
    }

    private void RollDice(PlayerStats stats)
    {
        // 차지 소모 처리
        stats.currentDiceCharge -= 100f;
        stats.currentDiceCharge = Mathf.Clamp(stats.currentDiceCharge, 0f, stats.maxDiceCharge);

        if (diceList == null || diceList.Length == 0) return;

        int selectedIndex = GetWeightedRandomIndex();                           // 가중치 기반 랜덤 선택
        bool isConsecutive = (lastRolledFaceIndex == selectedIndex && lastRolledFaceIndex != -1); // 이전 굴림과 동일한지 확인
        
        lastRolledFaceIndex = selectedIndex;
        DiceData selectedData = diceList[selectedIndex];

        // UI 연출 실행 및 버프 적용
        if (diceUI != null && selectedData != null)
        {
            diceUI.PlayRollAnimation(selectedData, selectedIndex, () =>
            {
                if (GameManager.instance.player)
                {
                    // 1. 기존 주사위 버프 적용
                    if (_buffManager != null) _buffManager.ApplyDiceBuff(selectedData);
                    
                    // 2. 신규 아티팩트 버프 적용 (현재 굴려진 인덱스와 연속 여부 전달)
                    CheckAndApplyDiceTriggerArtifacts(selectedIndex, isConsecutive);
                }
            });
        }
    }

    private void CheckAndApplyDiceTriggerArtifacts(int rolledIndex, bool isConsecutive)
    {
        if (ArtifactManager.instance == null || _buffManager == null) return;

        ConditionType targetCondition = ConditionType.None;

        // 배열 인덱스(0~5)를 조건부 타입(1~6)으로 매핑
        switch (rolledIndex)
        {
            case 0: targetCondition = ConditionType.OnDiceRoll1; break;
            case 1: targetCondition = ConditionType.OnDiceRoll2; break;
            case 2: targetCondition = ConditionType.OnDiceRoll3; break;
            case 3: targetCondition = ConditionType.OnDiceRoll4; break;
            case 4: targetCondition = ConditionType.OnDiceRoll5; break;
            case 5: targetCondition = ConditionType.OnDiceRoll6; break;
        }

        // 인벤토리의 아티팩트를 순회하며 조건이 일치하면 버프 발동
        foreach (var artifact in ArtifactManager.instance.myArtifacts)
        {
            if (artifact.type != ArtifactType.Trigger) continue;

            // 특정 면이 나왔을 때 발동
            if (artifact.condition == targetCondition)
            {
                _buffManager.ApplyArtifactBuff(artifact);
                Debug.Log($"주사위 아티팩트 발동: {artifact.artifactName}");
            }

            // 연속으로 같은 면이 나왔을 때 발동
            if (isConsecutive && artifact.condition == ConditionType.OnConsecutiveSameDice)
            {
                _buffManager.ApplyArtifactBuff(artifact);
                Debug.Log($"연속 굴림 아티팩트 발동: {artifact.artifactName}");
            }
        }
    }

    public void AddChargeFromHit()
    {
        if (GameManager.instance != null && GameManager.instance.stats != null)
            GameManager.instance.stats.AddDiceChargeFromHit();
    }

    // 확률 증가 아이템 사용 시 호출
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

    // UI 예측용 확률 계산 함수
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