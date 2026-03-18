using UnityEngine;

public enum StageModuleType 
{ 
    [InspectorName("연산 (Calculation) | 스탯 및 능력 강화")] 
    Calculation,
    
    [InspectorName("변칙 (Anomaly) | 무작위 이벤트 및 특이 공간")] 
    Anomaly,
    
    [InspectorName("정비 (Maintenance) | 상점 및 체력 회복")] 
    Maintenance,
    
    [InspectorName("과부하 (Overload) | 엘리트 전투 및 아티팩트 획득")] 
    Overload,
    
    [InspectorName("공정 (Process) | 일반 전투 및 대량 재화 획득")] 
    Process,
    
    [InspectorName("개조 (Modification) | 주사위 면 및 무게추 변경")] 
    Modification,
    
    [InspectorName("임계 (Critical) | 보스전 및 계절 종결")] 
    Critical
}

[CreateAssetMenu(fileName = "NewStageData", menuName = "Hexagram/StageData")]
public class StageData : ScriptableObject
{
    [Header("--- Module Settings ---")]
    public StageModuleType moduleType;
    
    // 인스펙터에서 수정하지 않아도 Enum에 따라 한글 이름을 반환
    public string moduleName => GetModuleName(moduleType);

    [TextArea] public string description;
    public Color themeColor; // 계절 공통 색상

    [Header("--- Percentage Settings ---")]
    public int minRise;  // 최소 증가량
    public int maxRise;  // 최대 증가량

    [Header("--- Visuals ---")]
    public Sprite moduleIcon;
    
    [Header("--- Season Prefabs ---")]
    [Tooltip("[0]:봄, [1]:여름, [2]:가을, [3]:겨울 순서로 프리팹을 넣어주세요.")]
    public SeasonPrefabGroup[] seasonPrefabs = new SeasonPrefabGroup[4];

    // 현재 게임의 계절에 맞는 프리팹 배열을 가져오는 함수
    public GameObject[] GetCurrentSeasonPrefabs(Season currentSeason)
    {
        int index = (int)currentSeason;
        if (index >= 0 && index < seasonPrefabs.Length)
        {
            return seasonPrefabs[index].prefabs;
        }
        return null;
    }

    // Enum을 한글 문자열로 변환하는 헬퍼 함수
    private string GetModuleName(StageModuleType type)
    {
        return type switch
        {
            StageModuleType.Calculation => "연산",
            StageModuleType.Anomaly => "변칙",
            StageModuleType.Maintenance => "정비",
            StageModuleType.Overload => "과부하",
            StageModuleType.Process => "공정",
            StageModuleType.Modification => "개조",
            StageModuleType.Critical => "임계",
            _ => "미지정"
        };
    }
}

[System.Serializable]
public class SeasonPrefabGroup
{
    public string seasonLabel; // 인스펙터 식별용 (예: Spring, Summer...)
    public GameObject[] prefabs;
}