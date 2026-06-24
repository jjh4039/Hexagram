using UnityEngine;
using GameAnalyticsSDK;
using System.Collections;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance;
    private bool isQuitting = false;

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
        GameAnalytics.StartSession();
        Application.wantsToQuit += OnWantsToQuit;
    }

    private bool OnWantsToQuit()
    {
        if (isQuitting) return true;

        isQuitting = true;

        GameAnalytics.EndSession();

        StartCoroutine(DelayedQuitRoutine());

        return false;
    }

    private IEnumerator DelayedQuitRoutine()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        Application.Quit();
    }

    // 1. 사망 시 도달 스테이지 / 진행도 / 누적 데미지
    public void LogPlayerDeath(string stageName, string progressInfo, int score)
    {
        GameAnalytics.NewProgressionEvent(GAProgressionStatus.Fail, stageName, progressInfo, score);
    }

    // 2. 런 시작 시 주사위 보석으로 어떤 스탯을 가지고 출발했는지
    public void LogUpgradeLoadout(int healthLv, int attackLv, int bulletLv, int diffLv)
    {
        GameAnalytics.NewDesignEvent("UpgradeLoadout:Health", healthLv);
        GameAnalytics.NewDesignEvent("UpgradeLoadout:Attack", attackLv);
        GameAnalytics.NewDesignEvent("UpgradeLoadout:Bullet", bulletLv);
        GameAnalytics.NewDesignEvent("UpgradeLoadout:Difficulty", diffLv);
    }

    // 3. 무게추 선택 시 어떤 눈금을 올렸는지
    public void LogBalanceSelection(int faceIndex, float weight)
    {
        GameAnalytics.NewDesignEvent($"Balance:SelectFace:Face_{faceIndex + 1}", weight);
    }

    // 4. 현재 주사위의 최종 확률 상태
    public void LogDiceBuildState(float[] percentages)
    {
        for (int i = 0; i < percentages.Length; i++)
        {
            GameAnalytics.NewDesignEvent($"DiceBuild:Face_{i + 1}", percentages[i]);
        }
    }

    // 5. 상점 관련
    public void LogShopPurchase(string category, string itemName, int scrapCost)
    {
        GameAnalytics.NewDesignEvent($"Shop:{category}:{itemName}", scrapCost);
    }

    // 6. 이벤트 선택 로그
    public void LogEventSelection(string riskType, string rewardType, int intensity)
    {
        // 전체 유저의 평균적인 강도 선택 비율 (1, 2, 3단계)
        GameAnalytics.NewDesignEvent($"Event:IntensityChoice:Level_{intensity}");

        // 특정 리스크가 떴을 때 유저들이 선택한 평균 강도
        GameAnalytics.NewDesignEvent($"Event:Risk:{riskType}", intensity);

        // 특정 보상이 떴을 때 유저들이 선택한 평균 강도
        GameAnalytics.NewDesignEvent($"Event:Reward:{rewardType}", intensity);
    }

    // 7. 평균적으로 유저가 어떤 모듈(스테이지)를 골랐는지  
    public void LogMapSelection(string moduleType, int gainedProgress)
    {
        GameAnalytics.NewDesignEvent($"Map:SelectModule:{moduleType}", gainedProgress); // 선택한 모듈 타입과 상승한 진행도 전송
    }

    // 8. 보스를 잡았을 때 유저들의 플레이 타임과 남은 체력 퍼센트
    public void LogBossClear(string seasonName, int clearTimeSeconds, int remainingHpPercent)
    {
        GameAnalytics.NewDesignEvent($"Boss:ClearTime:{seasonName}", clearTimeSeconds); // 보스 클리어에 걸린 총 시간 전송
        GameAnalytics.NewDesignEvent($"Boss:RemainingHP:{seasonName}", remainingHpPercent); // 보스 클리어 직후 플레이어 생존 체력 전송
    }

    // 9. 모듈 강화 선택에서 어떤 스탯 강화를 가장 선호하는가?
    public void LogModuleRewardSelection(string effectType)
    {
        GameAnalytics.NewDesignEvent($"ModuleReward:Select:{effectType}");
    }
}