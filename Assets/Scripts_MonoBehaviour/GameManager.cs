using UnityEngine;
using TMPro;
using System.Collections;

public enum Season { Spring, Summer, Autumn, Winter }

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public Player player;
    public PlayerStats stats;
    public WeaponManager weaponManager;
    public Dice dice;
    public MapManager mapManager;
    public BitManager bitManager;
    public BalanceManager balanceManager;
    public VirtualCursor cursor;
    public ShopUIController shopUIController;

    public GameObject currentStageObj;

    [Header("--- Global Resources ---")]
    public GameObject commonScrapPrefab;

    [Header("--- Scrap Data & UI ---")]
    public int currentScrap = 0;

    private float _hitStopTimer = 0f;
    private bool _isHitStopping = false;

    private Coroutine _scrapPunchRoutine;
    private Vector3 _scrapTextOriginScale;
    
    [Header("--- Season System ---")]
    // 1. 현재 계절 (레벨 역할)
    public Season currentSeason = Season.Spring; 

    // 3. 진행도 (0~100)
    public int currentProgress = 0;
    public int maxProgress = 100;
    
    [Header("--- Hit Stop ---")]
    private Coroutine _hitStopCoroutine;
    private float _originalFixedDeltaTime;
    private StageController _controller;

    private void Start()
    {
        if (currentStageObj != null)
        {
            // 2. 컴포넌트를 가져와서 변수에 할당
            _controller = currentStageObj.GetComponent<StageController>();

            // 3. 컴포넌트가 제대로 있다면 초기화 실행
            if (_controller != null)
            {
                _controller.InitStage();
            }
            else
            {
                Debug.LogWarning("현재 스테이지 오브젝트에 StageController가 없습니다!");
            }
        }
    }

    void Awake()
    {
        instance = this;
        _originalFixedDeltaTime = Time.fixedDeltaTime;
    }

    public void AddScrap(int amount)
    {
        currentScrap += amount;
    }

    // =========================================================
    // Stage Load
    // =========================================================
    public void LoadStage(StageData stageData)
    {
        if (currentStageObj)
            Destroy(currentStageObj);

        if (mapManager)
            mapManager.mapVisualRoot.SetActive(false);

        // [수정] 현재 계절에 맞는 프리팹 리스트를 가져옴
        GameObject[] seasonPrefabs = stageData.GetCurrentSeasonPrefabs(currentSeason);

        if (seasonPrefabs != null && seasonPrefabs.Length > 0)
        {
            int randomIndex = Random.Range(0, seasonPrefabs.Length);
            GameObject selectedPrefab = seasonPrefabs[randomIndex];

            currentStageObj = Instantiate(selectedPrefab, Vector3.zero, Quaternion.identity);
        
            // 인스턴스화 후 컨포넌트 재할당 및 초기화
            _controller = currentStageObj.GetComponent<StageController>();
            if (_controller != null)
                _controller.InitStage();

            if (CameraFollow.instance)
                CameraFollow.instance.SnapToTarget();

            // [수정] stageData.moduleName 사용 (GetModuleName 자동 변환 결과)
            if (StageMessageUI.instance)
                StageMessageUI.instance.ShowEntryMessage(stageData.moduleName, stageData.description);
        }
        else
        {
            Debug.LogError($"오류: {currentSeason} 계절에 {stageData.moduleName} 프리펩 데이터가 없습니다!");
        }
    }
    
    public void HitStop(float duration)
    {
        // 더 긴 히트스탑이 들어오면 연장
        _hitStopTimer = Mathf.Max(_hitStopTimer, duration);

        if (!_isHitStopping)
            StartCoroutine(hitStopRoutine());
        return;

        IEnumerator hitStopRoutine()
        {
            _isHitStopping = true;

            // ★ [수정됨] 0.2f(느려짐) -> 0.05f(거의 멈춤)으로 변경! 
            // 이제 렉이 아니라 진짜 타격감(역경직)으로 느껴집니다.
            Time.timeScale = 0.05f;
            Time.fixedDeltaTime = _originalFixedDeltaTime * Time.timeScale;

            while (_hitStopTimer > 0f)
            {
                _hitStopTimer -= Time.unscaledDeltaTime;
                yield return null;
            }

            Time.timeScale = 1f;
            Time.fixedDeltaTime = _originalFixedDeltaTime;

            _isHitStopping = false;
        }
    }
}
