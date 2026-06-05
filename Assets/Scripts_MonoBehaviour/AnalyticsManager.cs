using UnityEngine;
using GameAnalyticsSDK;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        GameAnalytics.Initialize();
    }
    
    // 분석 1 : 플레이어가 어디에서 가장 많이 죽는가?
    public void LogPlayerDeath(string stageName, string progressInfo)
    {
        GameAnalytics.NewProgressionEvent(GAProgressionStatus.Fail, stageName, progressInfo);
    }
}