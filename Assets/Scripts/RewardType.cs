using UnityEngine;

public enum RewardType
{
    None = 0,

    Scrap, // 스크랩 획득
    DiceFaceChanceUp, // 특정 면 확률 상승
    Artifact, // 아티팩트 획득  
    MaxHpUp, // 최대 체력 상승
    ModuleEnhanceChoice // 모듈 강화 선택지 부여
}