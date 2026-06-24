using UnityEngine;

public enum RiskType
{
    None = 0,

    BossHealthIncrease, // 해당 스테이지 보스 체력 증가
    NextStageDamageIncrease, // 다음 전투 스테이지 받는 피해 증가
    NoHealStages, // n스테이지 회복 불가
    DiceChargeReduction, // 주사위 모듈 충전량 감소
    CurrentHpCost // 현재 체력 소비
}