using UnityEngine;

[System.Serializable]
public class GameData
{
    public int totalGems = 0;            // 획득한 누적 보석
    public float totalPlayTime = 0f;     // 전체 누적 플레이 타임
    public bool isTutorialClear = false; // 튜토리얼 완료 유무

    public int masterVolume = 5;         // 마스터 볼륨 (0~10)
    public int bgmVolume = 5;            // 배경음악 볼륨 (0~10)
    public int sfxVolume = 5;            // 효과음 볼륨 (0~10)
    public int screenMode = 1;           // 화면 모드 (0: 창모드, 1: 테두리 없음, 2: 전체화면)
    public int resolution = 1;           // 해상도 (0: 720p, 1: 1080p, 2: 1440p)
    public int cameraShake = 1;          // 화면 흔들림 (0: OFF, 1: ON)

    public bool hasSavedRun = false;     // 진행 중인 게임 존재 여부
    public int currentRunHp = 100;       // 이어하기용 현재 체력
}