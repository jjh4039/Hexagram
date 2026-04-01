using UnityEngine;

[CreateAssetMenu(fileName = "New Risk Data", menuName = "Hexagram/Event/RiskData")]
public class RiskData : ScriptableObject
{
    [Header("기본 정보")]
    public string riskName;
    [TextArea] public string[] description;

    [Header("메커니즘 설정")]
    public RiskType riskType;

    [Header("수치 설정")]
    public StageValueData stageValues;

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

    public string GetDescription(int index)
    {
        if (description == null || description.Length == 0)
            return string.Empty;

        if (index < 0 || index >= description.Length)
            return description[0];

        return description[index];
    }
}