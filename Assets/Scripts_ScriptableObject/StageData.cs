using UnityEngine;

[CreateAssetMenu(fileName = "NewStageData", menuName = "Hexagram/StageData")]
public class StageData : ScriptableObject
{
    public string stageName;
    [TextArea] public string description;
    public GameObject[] stagePrefabs;

    // ★ [수정] 고정값 대신 범위로 변경
    [Header("Percentage Settings")]
    public int minRise;  // 최소 증가량
    public int maxRise;  // 최대 증가량

    public Color themeColor;
}