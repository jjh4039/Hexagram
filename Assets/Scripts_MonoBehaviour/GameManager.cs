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

    [Header("Global Resources")]
    public GameObject commonScrapPrefab;

    [Header("Scrap Data & UI")]
    public int currentScrap = 0;

    private float _hitStopTimer = 0f;
    private bool _isHitStopping = false;

    private Coroutine _scrapPunchRoutine;
    private Vector3 _scrapTextOriginScale;

    [Header("Season System")]
    public Season currentSeason = Season.Spring;
    public int currentProgress = 0;
    public int maxProgress = 100;

    [Header("Play Time")]
    public float currentPlayTime = 0f;                       // 누적 플레이 타임

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
                Debug.LogWarning("현재 스테이지 오브젝트에 StageController가 없습니다!");
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
        currentScrap += amount;
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

            currentStageObj = Instantiate(selectedPrefab, Vector3.zero, Quaternion.identity);

            _controller = currentStageObj.GetComponent<StageController>();
            if (_controller)
                _controller.InitStage();

            if (CameraFollow.instance)
                CameraFollow.instance.SnapToTarget();

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
                _hitStopTimer -= Time.unscaledDeltaTime;
                yield return null;
            }

            Time.timeScale = 1f;
            Time.fixedDeltaTime = _originalFixedDeltaTime;

            _isHitStopping = false;
        }
    }
}