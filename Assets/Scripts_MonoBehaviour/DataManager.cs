using UnityEngine;
using System.IO;

public class DataManager : MonoBehaviour
{
    public static DataManager instance;

    public GameData data;
    private string savePath;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            savePath = Application.persistentDataPath + "/SaveData.json";

            LoadGame();
            ApplyScreenMode(); // 시스템에 화면 설정 강제 적용
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

    [ContextMenu("Reset Data")]
    public void ResetData()
    {
        data = new GameData(); // 데이터 초기화
        SaveGame(); // 파일 저장
    }

    private void ApplyScreenMode()
    {
        FullScreenMode mode = FullScreenMode.Windowed;
        if (data.screenMode == 1) mode = FullScreenMode.FullScreenWindow;
        else if (data.screenMode == 2) mode = FullScreenMode.ExclusiveFullScreen;

        int width = 1920;
        int height = 1080;

        if (data.resolution == 0)
        {
            width = 1280;
            height = 720;
        }
        else if (data.resolution == 1)
        {
            width = 1920;
            height = 1080;
        }
        else if (data.resolution == 2)
        {
            width = 2560;
            height = 1440;
        }
        else if (data.resolution == 3)
        {
            width = 3840;
            height = 2160;
        }

        if (mode == FullScreenMode.FullScreenWindow)
        {
            Resolution nativeRes = Screen.resolutions[Screen.resolutions.Length - 1];
            width = nativeRes.width;
            height = nativeRes.height;
        }

        Screen.SetResolution(width, height, mode);
    }
}