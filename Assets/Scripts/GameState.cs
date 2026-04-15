using UnityEngine;

// 게임의 전체적인 상황
public enum GamePhase
{
    SafeZone, // 적이 없는 안전한 구역
    InCombat  // 몬스터와 싸우는 전투 상황
}

// 플레이어의 현재 조작 상태
public enum InputState
{
    Normal, // 이동과 탐색이 자유로운 평상시 조작
    Combat, // 가방 열기 등이 제한되는 전투용 조작
    UI      // 캐릭터가 멈추고 마우스만 사용하는 화면 조작
}

// 상태가 변할 때 함께 전달할 데이터
public struct StateChangeContext
{
    public InputState PreviousState; // 변경되기 이전의 조작 상태
    public InputState NewState;      // 새롭게 변경된 조작 상태
    public object RawData;           // 추가로 전달할 임의의 데이터
}