using UnityEngine;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public Player player;
    public PlayerStats stats;
    public WeaponManager weaponManager;
    public Dice dice;
    public MapManager mapManager;
    public BitManager bitManager;
    public VirtualCursor cursor;

    public GameObject currentStageObj;

    [Header("--- Global Resources ---")]
    public GameObject commonScrapPrefab;

    [Header("--- Scrap Data & UI ---")]
    public int currentScrap = 0;

    private float hitStopTimer = 0f;
    private bool isHitStopping = false;

    private Coroutine scrapPunchRoutine;
    private Vector3 scrapTextOriginScale;

    public int currentProgress = 0;
    public int maxProgress = 100;

    // =========================
    // ★ HitStop System
    // =========================
    private Coroutine hitStopCoroutine;
    private float originalFixedDeltaTime;

    void Awake()
    {
        instance = this;
        originalFixedDeltaTime = Time.fixedDeltaTime;
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
        if (currentStageObj != null)
            Destroy(currentStageObj);

        if (mapManager != null)
            mapManager.mapVisualRoot.SetActive(false);

        if (stageData.stagePrefabs != null && stageData.stagePrefabs.Length > 0)
        {
            int randomIndex = Random.Range(0, stageData.stagePrefabs.Length);
            GameObject selectedPrefab = stageData.stagePrefabs[randomIndex];

            currentStageObj = Instantiate(selectedPrefab, Vector3.zero, Quaternion.identity);

            StageController controller = currentStageObj.GetComponent<StageController>();
            if (controller != null)
                controller.InitStage();

            if (CameraFollow.instance != null)
                CameraFollow.instance.SnapToTarget();

            if (StageMessageUI.instance != null)
                StageMessageUI.instance.ShowEntryMessage(stageData.stageName, stageData.description);
        }
        else
        {
            Debug.LogError($"오류: {stageData.stageName} 데이터 프리펩 없음!");
        }
    }

    // =========================================================
    // ★ Hit Stop (안정 버전)
    // =========================================================
    public void HitStop(float duration)
    {
        // 더 긴 히트스탑이 들어오면 연장
        hitStopTimer = Mathf.Max(hitStopTimer, duration);

        if (!isHitStopping)
            StartCoroutine(HitStopRoutine());
    }

    private IEnumerator HitStopRoutine()
    {
        isHitStopping = true;

        // ★ [수정됨] 0.2f(느려짐) -> 0.05f(거의 멈춤)으로 변경! 
        // 이제 렉이 아니라 진짜 타격감(역경직)으로 느껴집니다.
        Time.timeScale = 0.05f;
        Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale;

        while (hitStopTimer > 0f)
        {
            hitStopTimer -= Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDeltaTime;

        isHitStopping = false;
    }
}
