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
    public EventManager eventManager;
    public VirtualCursor cursor;
    public ShopUIController shopUIController;

    public GameObject currentStageObj;
    public Transform stageParent;                          // 생성될 스테이지의 부모 트랜스폼

    [Header("Global Resources")]
    public GameObject commonScrapPrefab;

    [Header("Scrap Data & UI")]
    public int currentScrap = 0;
    public float scrapPercentage = 1f;                     // 스크랩 획득 보너스 비율 (기본 1 = +100%)

    private float _hitStopTimer = 0f;
    private bool _isHitStopping = false;

    private Coroutine _scrapPunchRoutine;
    private Vector3 _scrapTextOriginScale;

    [Header("Season System")]
    public Season currentSeason = Season.Spring;
    public int currentProgress = 0;
    public int maxProgress = 100;

    [Header("Play Time")]
    public float currentPlayTime = 0f;                       // 플레이 타임
    public int totalDamageDealt = 2133;                          // 누적 피해량

    [Header("Hit Stop")]
    private Coroutine _hitStopCoroutine;
    private float _originalFixedDeltaTime;
    private StageController _controller;

    private void Start()
    {
        if (currentStageObj != null)
        {
            _controller = currentStageObj.GetComponent<StageController>();

            if (_controller != null)
            {
                _controller.InitStage();
            }
            else
            {
                Debug.LogWarning("StageController 컴포넌트 누락");
            }
        }
    }

    void Awake()
    {
        instance = this;
        _originalFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void Update()
    {
        currentPlayTime += Time.deltaTime;                   // 매 프레임 플레이 타임 누적
    }

    public void AddScrap(int amount)
    {
        float multiplier = scrapPercentage;           // 기본 배율(1) + 보너스 비율 계산
        int finalAmount = Mathf.CeilToInt(amount * multiplier); // 소수점 올림으로 최종 획득량 산출
    
        currentScrap += finalAmount;                            // 최종 계산된 스크랩 적용
    }

    public void LoadStage(StageData stageData)
    {
        if (currentStageObj)
            Destroy(currentStageObj);

        if (mapManager)
            mapManager.mapVisualRoot.SetActive(false);

        GameObject[] seasonPrefabs = stageData.GetCurrentSeasonPrefabs(currentSeason);

        if (seasonPrefabs != null && seasonPrefabs.Length > 0)
        {
            int randomIndex = Random.Range(0, seasonPrefabs.Length);
            GameObject selectedPrefab = seasonPrefabs[randomIndex];

            // 변경된 부분: 4번째 매개변수로 부모 트랜스폼 지정
            currentStageObj = Instantiate(selectedPrefab, Vector3.zero, Quaternion.identity, stageParent);

            if (currentStageObj.TryGetComponent(out _controller))
            {
                _controller.InitStage();
            }
            
            if (_controller)
                _controller.InitStage();

            if (CameraFollow.instance)
                CameraFollow.instance.SnapToTarget();

            if (StageMessageUI.instance)
                StageMessageUI.instance.ShowEntryMessage(stageData.moduleName, stageData.description);
        }
        else
        {
            Debug.LogError("해당 계절 프리팹 데이터 누락");
        }
    }

    public void HitStop(float duration)
    {
        _hitStopTimer = Mathf.Max(_hitStopTimer, duration);

        if (!_isHitStopping)
            StartCoroutine(hitStopRoutine());
        return;

        IEnumerator hitStopRoutine()
        {
            _isHitStopping = true;

            Time.timeScale = 0.05f;
            Time.fixedDeltaTime = _originalFixedDeltaTime * Time.timeScale;

            while (_hitStopTimer > 0f)
            {
                if (InputStateManager.Instance && InputStateManager.Instance.CurrentInputState == InputState.UI)
                {
                    yield return null;                       // 일시정지 중에는 타이머 대기
                    continue;
                }

                _hitStopTimer -= Time.unscaledDeltaTime;     // 일시정지가 아닐 때만 현실 시간 기준으로 감소
                yield return null;
            }

            if (!InputStateManager.Instance || InputStateManager.Instance.CurrentInputState != InputState.UI)
            {
                Time.timeScale = 1f;                         // 일시정지가 아닐 때만 시간 배율 복구
                Time.fixedDeltaTime = _originalFixedDeltaTime;
            }

            _isHitStopping = false;
        }
    }
}