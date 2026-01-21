using UnityEngine;

[CreateAssetMenu(fileName = "NewStageData", menuName = "Hexagram/StageData")]
public class StageData : ScriptableObject
{
    public string stageName;       // 예: 엘리트, 상점
    [TextArea]
    public string description;     // 노드 설명 (강력한 적과 마주합니다... 등)
    public GameObject stagePrefab; // 실제 생성될 방 프리팹
    public Color themeColor;       // UI 발광 및 텍스트에 쓸 퍼스널 컬러
}