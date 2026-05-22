using UnityEngine;
using TMPro;
using System.Collections;

public enum Season { Spring, Summer, Autumn, Winter }

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Auto Assigned References")]
    public Player player;
    public PlayerStats stats;
    public WeaponManager weaponManager;
    public Dice dice;
    public MapManager mapManager;
    public BitManager bitManager;
    public BalanceManager balanceManager;
    public VirtualCursor cursor;
    public ShopUIController shopUIController;

    [Header("Stage Settings")]
    public GameObject currentStageObj;
    public Transform stageParent;                          

    [Header("Global Resources")]
    public GameObject commonScrapPrefab;

    [Header("Scrap Data & UI")]
    public int currentScrap = 0;
    public float scrapPercentage = 1f;                     

    private float _hitStopTimer = 0f;
    private bool _isHitStopping = false;

    private Coroutine _scrapPunchRoutine;
    private Vector3 _scrapTextOriginScale;
    private int _lastStageIndex = -1;                      

    [Header("Season System")]
    public Season currentSeason = Season.Spring;
    public int currentProgress = 0;
    public int maxProgress = 100;

    [Header("Play Time")]
    public float currentPlayTime = 0f;                       
    public int totalDamageDealt;                      

    [Header("Event System")]
    public float eventBossHealthMultiplier = 1.0f;

    [Header("Hit Stop")]
    private Coroutine _hitStopCoroutine;
    private float _originalFixedDeltaTime;
    private StageController _controller;

    private void Awake()
    {
        instance = this;
        _originalFixedDeltaTime = Time.fixedDeltaTime;

        FindSceneComponents();
    }

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

    private void Update()
    {
        currentPlayTime += Time.deltaTime;                   
    }

    private void FindSceneComponents()
    {
        if (player == null) player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            if (stats == null) stats = player.GetComponent<PlayerStats>();
            if (weaponManager == null) weaponManager = player.GetComponentInChildren<WeaponManager>();
        }
        
        if (mapManager == null) mapManager = FindFirstObjectByType<MapManager>();
        if (bitManager == null) bitManager = FindFirstObjectByType<BitManager>();
        if (balanceManager == null) balanceManager = FindFirstObjectByType<BalanceManager>();
        if (cursor == null) cursor = FindFirstObjectByType<VirtualCursor>();
        if (shopUIController == null) shopUIController = FindFirstObjectByType<ShopUIController>();
        if (dice == null) dice = FindFirstObjectByType<Dice>();
    }

    public void AddScrap(int amount)
    {
        float multiplier = scrapPercentage;            
        int finalAmount = Mathf.CeilToInt(amount * multiplier); 

        currentScrap += finalAmount;                            
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
            int randomIndex = 0;

            if (seasonPrefabs.Length > 1)
            {
                do
                {
                    randomIndex = Random.Range(0, seasonPrefabs.Length);
                } while (randomIndex == _lastStageIndex);
            }

            _lastStageIndex = randomIndex;
            GameObject selectedPrefab = seasonPrefabs[randomIndex];

            currentStageObj = Instantiate(selectedPrefab, Vector3.zero, Quaternion.identity, stageParent);

            if (currentStageObj.TryGetComponent(out _controller))
            {
                _controller.InitStage();
            }

            if (CameraFollow.Instance)
                CameraFollow.Instance.SnapToTarget();

            if (currentProgress < 100)
            {
                if (StageMessageUI.instance)
                {
                    StageMessageUI.instance.ShowEntryMessage(stageData.moduleName, stageData.description, stageData.themeColor);
                }
            }
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
                    yield return null;                       
                    continue;
                }

                _hitStopTimer -= Time.unscaledDeltaTime;     
                yield return null;
            }

            if (!InputStateManager.Instance || InputStateManager.Instance.CurrentInputState != InputState.UI)
            {
                Time.timeScale = 1f;                         
                Time.fixedDeltaTime = _originalFixedDeltaTime;
            }

            _isHitStopping = false;
        }
    }
}