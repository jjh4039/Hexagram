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
    // 확률 및 무게추 관련 로직 (퍼센트 기반으로 개편)
    // =========================================================

    // 특정 면의 확률을 원하는 퍼센트(%)만큼 정확히 상승시킵니다.
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

        // 현재 퍼센트와 목표 퍼센트 계산
        float currentPercent = (float)faceWeights[faceIndex] / totalWeight;
        float targetPercent = currentPercent + (percentIncrease / 100f);

        // 확률이 100% 이상이 되는 것을 방지
        if (targetPercent >= 1f) targetPercent = 0.999f;

        // 목표 퍼센트에 도달하기 위해 필요한 추가 티켓 수 역산 공식
        // x = (목표확률 * 전체합 - 현재가중치) / (1 - 목표확률)
        float requiredWeightFloat = ((targetPercent * totalWeight) - faceWeights[faceIndex]) / (1f - targetPercent);
        int addedWeight = Mathf.Max(0, Mathf.RoundToInt(requiredWeightFloat));

        faceWeights[faceIndex] += addedWeight;
        CalculatePercentages();

        Debug.Log($"[{faceIndex + 1}번 면] {percentIncrease}% 증가를 위해 가중치 {addedWeight} 추가됨");
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