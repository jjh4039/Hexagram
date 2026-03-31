using UnityEngine;

[CreateAssetMenu(fileName = "New Risk Data", menuName = "Hexagram/Event/RiskData")]
public class RiskData : ScriptableObject
{
    [Header("기본 정보")]
    public string riskName;                 // 리스크 이름
    [TextArea] public string description;   // 설명

    [Header("메커니즘 설정")]
    public RiskType riskType;               // 리스크 종류

    [Header("수치 설정")]
    public StageValueData stageValues;      // 1 / 2 / 3단계 값

    public float GetValue(int stage)
    {
        return stageValues.GetValue(stage);
    }

    public string GetValueText(int stage)
    {
        float value = GetValue(stage);

        switch (riskType)
        {
            case RiskType.BossHealthIncrease:
            case RiskType.NextStageDamageIncrease:
            case RiskType.DiceChargeReduction:
            case RiskType.CurrentHpCost:
                return $"{value:0}%";

            case RiskType.NoHealStages:
                return $"{value:0} 스테이지";

            default:
                return value.ToString("0");
        }
    }
}