using UnityEngine;
using UnityEngine.InputSystem;

public class Dice : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private Dice_UI diceUI;

    [Header("Data Settings")]
    [SerializeField] public DiceData[] diceList;
    [SerializeField] public DiceData defaultData;

    [Header("Probability Settings")]
    [SerializeField] private int[] faceWeights = new int[6] { 100, 100, 100, 100, 100, 100 };
    [SerializeField] public float[] displayPercentages = new float[6];

    [Header("History")]
    public int lastRolledFaceIndex = -1;

    private void Start()
    {
        CalculatePercentages();
    }

    private void Update()
    {
        if (GameManager.instance == null || GameManager.instance.stats == null || GameManager.instance.player == null)
            return;

        PlayerStats stats = GameManager.instance.stats;
        Player player = GameManager.instance.player;

        HandleInput(stats, player);
    }

    private void HandleInput(PlayerStats stats, Player player)
    {
        if (!Keyboard.current.eKey.wasPressedThisFrame)
            return;

        if (diceUI != null && !diceUI.IsRolling && stats.currentDiceCharge >= 100f)
        {
            RollDice(stats, player);
        }
    }

    private void RollDice(PlayerStats stats, Player player)
    {
        stats.currentDiceCharge -= 100f;
        stats.currentDiceCharge = Mathf.Clamp(stats.currentDiceCharge, 0f, stats.maxDiceCharge);

        if (diceList == null || diceList.Length == 0)
            return;

        int selectedIndex = GetWeightedRandomIndex();
        lastRolledFaceIndex = selectedIndex;
        DiceData selectedData = diceList[selectedIndex];

        if (diceUI != null && selectedData != null)
        {
            diceUI.PlayRollAnimation(selectedData, selectedIndex, () =>
            {
                if (player != null)
                {
                    BuffManager buffManager = player.GetComponent<BuffManager>();
                    if (buffManager != null)
                    {
                        buffManager.ApplyDiceBuff(selectedData);
                    }
                    else
                    {
                        Debug.LogWarning("Player에 BuffManager 컴포넌트가 없습니다!");
                    }
                }
            });
        }
    }

    public void AddChargeFromHit()
    {
        if (GameManager.instance == null || GameManager.instance.stats == null)
            return;

        GameManager.instance.stats.AddDiceChargeFromHit();
    }

    // =========================================================
    // 확률 및 무게추 관련 로직 (퍼센트 기반)
    // =========================================================

    public void AddPercentToFace(int faceIndex, float percentIncrease)
    {
        if (faceIndex < 0 || faceIndex >= faceWeights.Length)
            return;

        int totalWeight = 0;
        for (int i = 0; i < faceWeights.Length; i++)
        {
            totalWeight += faceWeights[i];
        }

        if (totalWeight == 0) return;

        float currentPercent = (float)faceWeights[faceIndex] / totalWeight;
        float targetPercent = currentPercent + (percentIncrease / 100f);

        if (targetPercent >= 1f) targetPercent = 0.999f;

        float requiredWeightFloat = ((targetPercent * totalWeight) - faceWeights[faceIndex]) / (1f - targetPercent);
        int addedWeight = Mathf.Max(0, Mathf.RoundToInt(requiredWeightFloat));

        faceWeights[faceIndex] += addedWeight;
        CalculatePercentages();

        Debug.Log($"[{faceIndex + 1}번 면] {percentIncrease}% 증가를 위해 가중치 {addedWeight} 추가됨");
    }

    // 실제 스탯 변경 없이 UI에 보여줄 예측 퍼센트만 계산해서 반환하는 함수
    public float[] GetPredictedPercentages(int faceIndex, float percentIncrease)
    {
        float[] predicted = new float[6];
        int totalWeight = 0;

        for (int i = 0; i < faceWeights.Length; i++)
            totalWeight += faceWeights[i];

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
        for (int i = 0; i < faceWeights.Length; i++)
        {
            totalWeight += faceWeights[i];
        }

        if (totalWeight == 0) return;

        for (int i = 0; i < faceWeights.Length; i++)
        {
            displayPercentages[i] = ((float)faceWeights[i] / totalWeight) * 100f;
        }
    }

    private int GetWeightedRandomIndex()
    {
        int totalWeight = 0;
        for (int i = 0; i < faceWeights.Length; i++)
        {
            totalWeight += faceWeights[i];
        }

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        for (int i = 0; i < faceWeights.Length; i++)
        {
            currentWeight += faceWeights[i];
            if (randomValue < currentWeight)
            {
                return i;
            }
        }

        return 0;
    }
}