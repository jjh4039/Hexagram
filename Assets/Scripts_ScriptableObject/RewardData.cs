using UnityEngine;

[CreateAssetMenu(
    fileName = "RewardData",
    menuName = "Hexagram/RewardData"
)]
public class RewardData : ScriptableObject
{
    [Header("UI")]
    public string titleText;          // 공격력, 공격속도 등
    public string valueText;     // +10%, +1.5초 등
    public Color valueTextColor;        // 타이틀 색상
    // 추후에 수치 반영할 때 사용
}
