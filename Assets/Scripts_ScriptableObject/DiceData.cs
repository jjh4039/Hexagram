using UnityEngine;

// 주사위 효과 종류 (오타 방지용 목록)
public enum DiceEffectType
{
    AttackBuff,     // 빨강: 공격력
    CriticalBuff,   // 주황: 치명타
    GrowthBuff,     // 노랑: 성장(골드/체력)
    Heal,           // 초록: 회복
    SpeedBuff,      // 파랑: 속도
    ChargingBuff    // 보라: 충전속도
}

// [CreateAssetMenu] : 이 코드가 있으면 유니티 우클릭 메뉴에 'Dice Data'가 생깁니다!
[CreateAssetMenu(fileName = "New Dice Data", menuName = "Hexagram/DiceData")]
public class DiceData : ScriptableObject
{
    [Header("--- 기본 정보 ---")]
    public string diceName;          // 주사위 이름 (예: 광전사의 눈)
    [TextArea] public string description; // 설명
    [TextArea] public string shortDescription; // 설명 (UI에 뜰 텍스트)

    [Header("--- 비주얼 ---")]
    public Sprite icon;              // UI 아이콘 (I, II, III...)
    public Color32 particleColor;    // 파티클 색 (진한 색)
    public Color32 uiGlowColor;      // UI 배경 색 (연한 색)

    [Header("--- 능력치 ---")]
    public DiceEffectType effectType; // 효과 종류 (위에서 만든 목록 중 선택)
    public float effectValue;         // 효과 수치 (예: 10)
    public float duration;            // 지속 시간 (예: 10초)

    [Header("--- 총알 파티클 ---")]
    public Material muzzleFlashMaterial;
}
