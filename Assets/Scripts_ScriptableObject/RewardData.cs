using UnityEngine;

[CreateAssetMenu(fileName = "New Reward Data", menuName = "Hexagram/Event/RewardData")]
public class RewardData : ScriptableObject
{
    [Header("기본 정보")]
    public string rewardName;
    [TextArea] public string[] description;

    [Header("메커니즘 설정")]
    public RewardType rewardType;

    [Header("수치 설정")]
    public StageValueData stageValues;

    public float GetValue(int stage)
    {
        return stageValues.GetValue(stage);
    }

    public string GetValueText(int stage)
    {
        float value = GetValue(stage);

        switch (rewardType)
        {
            case RewardType.Scrap:
            case RewardType.MaxHpUp:
            case RewardType.ModuleEnhanceChoice:
                return value.ToString("0");

            case RewardType.DiceFaceChanceUp:
                return $"+{value:0}%";

            case RewardType.Artifact:
                return GetArtifactGradeText(stage);

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

    private string GetArtifactGradeText(int stage)
    {
        switch (stage)
        {
            case 1: return "일반";
            case 2: return "희귀";
            case 3: return "영웅";
            default: return "일반";
        }
    }
}