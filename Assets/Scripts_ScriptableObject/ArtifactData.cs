using UnityEngine;

// 1. 아티팩트의 작동 방식 (Stat, Trigger로 간소화)
public enum ArtifactType
{
    Stat,           // 영구 스탯 (획득 즉시 스탯에 곱적용)
    Trigger         // 특정 조건 만족 시 발동 (버프 부여 및 효과 발동)
}

public enum ArtifactGrade
{
    Common,     
    Rare,       
    Epic,       
    Legendary   
}

public enum ArtifactEffectType
{
    None,
    MaxHp, AttackPower, AttackSpeed, MoveSpeed, ScrapGain, DiceSpeed, ChargeSpeed, CritChance, CritDamage,// 기본 스탯
    DefenseFirstHit, DamageGlassCannon, PaybackScrap, DiceReroll, BuffInfinity // 특수 능력
}

// 2. 발동 조건 간소화 (Trigger 전용)
public enum ConditionType
{
    None,                   // 조건 없음 (Stat 타입 전용)
    OnDiceRoll1,           // 주사위 1이 발동될 때
    OnDiceRoll2,           // 주사위 2가 발동될 때
    OnDiceRoll3,           // 주사위 3이 발동될 때
    OnDiceRoll4,           // 주사위 4가 발동될 때
    OnDiceRoll5,           // 주사위 5가 발동될 때
    OnDiceRoll6,           // 주사위 6이 발동될 때
    OnStageEnter,           // 스테이지를 이동할 때 (시작 시)
    OnConsecutiveSameDice   // 주사위가 연속으로 같은 값이 나왔을 때
}

[CreateAssetMenu(fileName = "New Artifact", menuName = "Hexagram/ArtifactData")]
public class ArtifactData : ScriptableObject
{
    [Header("기본 정보")]
    public string artifactName;                     // 아티팩트 이름
    [TextArea] public string description;           // 설명
    public Sprite icon;                             // 아이콘
    public ArtifactGrade grade;                     // 등급
    public int basePrice = 100;                     // 상점 가격

    [Header("메커니즘 설정")]
    public ArtifactType type;                       // 작동 방식 (Stat / Trigger)
    public ConditionType condition;                 // 발동 조건 (Trigger일 경우)

    [Header("첫 번째 효과")]
    public ArtifactEffectType effectType;           // 첫 번째 효과 종류
    public float value;                             // 수치
    public bool isPercent;                          // % 연산 여부 (체크=곱연산, 해제=합연산)

    [Header("두 번째 효과 (옵션 / 2개 스탯 동시 증가용)")]
    public ArtifactEffectType effectType2 = ArtifactEffectType.None; 
    public float value2;                            // 두 번째 수치
    public bool isPercent2;                         // 두 번째 % 연산 여부
}