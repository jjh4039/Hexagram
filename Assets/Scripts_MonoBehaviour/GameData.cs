using UnityEngine;

[System.Serializable]
public class GameData
{
    public int totalGems = 0;            
    public float totalPlayTime = 0f;     
    public bool isTutorialClear = false; 
    public int masterVolume = 8;         
    public int bgmVolume = 8;            
    public int sfxVolume = 8;            
    public int screenMode = 0;           
    public int resolution = 1;           
    public int vSync = 1;                
    
    public int upgradeHealthLevel = 0;   // 최대 체력 증가 레벨
    public int upgradeAttackLevel = 0;   // 공격력 증가 레벨
    public int upgradeBulletLevel = 0;   // 총알 업그레이드 레벨
    public int difficultyLevel = 0;      // 난이도 상승 레벨
}