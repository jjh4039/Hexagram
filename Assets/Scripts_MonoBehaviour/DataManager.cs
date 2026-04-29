using UnityEngine;
using System.IO;

public class DataManager : MonoBehaviour
{
    public static DataManager instance;     // 싱글톤 인스턴스

    public GameData data;                   // 실제 메모리에 들고 있을 세이브 데이터
    private string savePath;                // 저장될 물리적 파일 경로

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            savePath = Application.persistentDataPath + "/SaveData.json"; // 기기별 안전한 저장 경로
            LoadGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveGame()
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
    }

    public void LoadGame()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            data = JsonUtility.FromJson<GameData>(json);
        }
        else
        {
            data = new GameData();
            SaveGame();
        }
    }
}