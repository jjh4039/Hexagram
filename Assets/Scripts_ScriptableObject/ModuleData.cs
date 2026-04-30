using UnityEngine;

[CreateAssetMenu(
    fileName = "RewardData",
    menuName = "Hexagram/ModuleData"
)]
public class ModuleData : ScriptableObject
{
    [Header("UI")]
    public string titleText;          // 공격력, 공격속도 등
    public string valueText;          // +10%, +1.5초 등
    public Color valueTextColor;      // 타이틀 색상

    [Header("Stat Effect")]
    public ArtifactEffectType effectType; // 어떤 스탯을 올릴 것인가?
    public float valueAmount;             // 실제 수치 (예: 0.1f, 1.5f)
    public bool isPercent;                // 퍼센트(곱연산)인가? 고정 수치(합연산)인가?
}