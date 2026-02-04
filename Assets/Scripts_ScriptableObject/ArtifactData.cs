using UnityEngine;

// 1. 아티팩트의 작동 방식 (가장 큰 분류)
public enum ArtifactType
{
    Stat,           // 영구 스탯 (획득 즉시 적용) -> 예: 쿠키(체력), 교본(공격력)
    Conditional,    // 조건부 스탯 (특정 상황에서만 적용) -> 예: 유리창(안 맞으면 공증)
    Trigger         // 특정 행동 시 발동 (이벤트) -> 예: 멤버십(돈 쓸 때 환급), 믿음(맞을 때 무효)
}

// 2. 아티팩트 등급 (색상 및 희귀도)
public enum ArtifactGrade
{
    Common,     // 흰색 (일반)
    Rare,       // 노란색 (희귀)
    Epic,       // 보라색 (영웅) - *필요할 것 같아서 추가했습니다!
    Legendary   // 빨간색 (전설)
}

// 3. 효과 타입 (어떤 능력을 건드리는가?)
public enum ArtifactEffectType
{
    None,
    // --- 기본 스탯 ---
    MaxHP,          // 최대 체력
    AttackPower,    // 공격력
    AttackSpeed,    // 공격 속도
    MoveSpeed,      // 이동 속도
    ScrapGain,      // 고철 획득량
    DiceSpeed,      // 주사위 굴림 속도
    ChargeSpeed,    // 충전 속도 (원거리)

    // --- 특수 능력 (Trigger/Conditional용) ---
    Defense_FirstHit,   // 첫 피해 무시 (믿음)
    Damage_GlassCannon, // 피격 전까지 공격력 증가 (유리창)
    Payback_Scrap,      // 고철 사용 시 환급 (멤버십)
    Dice_Reroll,        // 주사위 리롤 권한
    Buff_Infinity       // 버프 무한 유지 (천칭)
}

// 4. 발동 조건 (언제 발동하는가?)
public enum ConditionType
{
    None,               // 조건 없음 (Stat 타입은 항상 None)
    HP_Below_5_Percent, // 체력 5% 이하
    No_Damage_Taken,    // 해당 스테이지에서 피격 없음
    Dice_Face_1,        // 주사위 1번 효과 발동 중
    On_Scrap_Use,       // 고철을 소모했을 때
    On_Stage_Start,     // 스테이지 시작 시
    Twice_Same_Dice_Face, // 주사위가 연속으로 같은 값이 나왔을 때 (예: 3-3)
    On_Buff_End         // 버프가 끝날 때 (천칭)
}

[CreateAssetMenu(fileName = "New Artifact", menuName = "Hexagram/ArtifactData")]
public class ArtifactData : ScriptableObject
{
    [Header("기본 정보")]
    public string artifactName;       // 이름
    [TextArea] public string description; // 설명
    public Sprite icon;               // 아이콘
    public ArtifactGrade grade;       // 등급
    public int basePrice = 100;       // 상점 가격

    [Header("메커니즘 설정 : 영구 / 조건부 / 특정 행동 시")]
    public ArtifactType type;         // 작동 방식 (Stat / Conditional / Trigger)
    public ArtifactEffectType effectType; // 효과 종류
    public ConditionType condition;   // 발동 조건

    [Header("수치 설정")]
    public float value;               // 수치 (예: 5, 10, 0.2 ...)
    public bool isPercent;            // % 연산 여부 (체크=곱연산, 해제=합연산)
}