using UnityEngine;

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
    MaxHp, AttackPower, AttackSpeed, MoveSpeed, ScrapGain, 
    DiceSpeed, ChargeSpeed, CritChance, CritDamage, FinalDamage,
    DefenseFirstHit, DamageGlassCannon, PaybackScrap, DiceReroll, BuffInfinity
}

public enum ConditionType
{
    None,                   
    OnDiceRoll1,           
    OnDiceRoll2,           
    OnDiceRoll3,           
    OnDiceRoll4,           
    OnDiceRoll5,           
    OnDiceRoll6,           
    OnStageEnter,           // 스테이지를 이동할 때 (시작 시) 발동
    OnConsecutiveSameDice   
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
    public ArtifactType type;                       // 작동 방식
    public ConditionType condition;                 // 발동 조건

    [Header("버프 설정 (Trigger 전용)")]
    public float buffDuration = 10f;                // 버프 지속 시간 (기본 10초)
    public bool isInfiniteBuff = false;             // 체크 시 무한 지속 (피격 등 특정 조건 전까지)

    [Header("첫 번째 효과")]
    public ArtifactEffectType effectType;           
    public float value;                             
    public bool isPercent;                          

    [Header("두 번째 효과 (옵션)")]
    public ArtifactEffectType effectType2 = ArtifactEffectType.None; 
    public float value2;                            
    public bool isPercent2;                         
}