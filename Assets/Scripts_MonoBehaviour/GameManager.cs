using UnityEngine;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public Player player;
    public PlayerStats stats;
    public WeaponUI weaponUI;
    public Dice dice;
    public MapManager mapManager;

    public GameObject currentStageObj;

    [Header("--- Global Resources ---")]
    public GameObject commonScrapPrefab;

    [Header("--- Scrap Data & UI ---")]
    public int currentScrap = 0;
    public TextMeshProUGUI scrapText;

    private Coroutine scrapPunchRoutine;
    private Vector3 scrapTextOriginScale;

    public int currentProgress = 0;
    public int maxProgress = 100;

    // ★ [추가] 힛스탑 중인지 체크하는 변수
    private bool isHitStopping = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (scrapText != null)
        {
            scrapTextOriginScale = scrapText.transform.localScale;
            UpdateScrapUI();
        }
    }

    public void LoadStage(StageData stageData)
    {
        if (currentStageObj != null) Destroy(currentStageObj);
        if (mapManager != null) mapManager.mapVisualRoot.SetActive(false);

        if (stageData.stagePrefabs != null && stageData.stagePrefabs.Length > 0)
        {
            int randomIndex = Random.Range(0, stageData.stagePrefabs.Length);
            GameObject selectedPrefab = stageData.stagePrefabs[randomIndex];
            currentStageObj = Instantiate(selectedPrefab, Vector3.zero, Quaternion.identity);

            StageController controller = currentStageObj.GetComponent<StageController>();
            if (controller != null) controller.InitStage();

            if (CameraFollow.instance != null) CameraFollow.instance.SnapToTarget();

            if (StageMessageUI.instance != null)
            {
                StageMessageUI.instance.ShowEntryMessage(stageData.stageName, stageData.description);
            }
        }
        else
        {
            Debug.LogError($"오류: {stageData.stageName} 데이터 프리펩 없음!");
        }
    }

    public void AddScrap(int amount)
    {
        currentScrap += amount;
        UpdateScrapUI();

        if (scrapText != null)
        {
            if (scrapPunchRoutine != null) StopCoroutine(scrapPunchRoutine);
            scrapPunchRoutine = StartCoroutine(ScrapTextPunch());
        }
    }

    private void UpdateScrapUI()
    {
        if (scrapText != null)
        {
            scrapText.text = $"{currentScrap}";
        }
    }

    private IEnumerator ScrapTextPunch()
    {
        float duration = 0.12f;
        float elapsed = 0f;
        Vector3 targetScale = scrapTextOriginScale * 1.1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float scale = Mathf.Sin(t * Mathf.PI);
            scrapText.transform.localScale = Vector3.Lerp(scrapTextOriginScale, targetScale, scale);
            yield return null;
        }
        scrapText.transform.localScale = scrapTextOriginScale;
    }

    // =========================================================
    // ★ [추가] 타격 정지 (Hit Stop) 기능
    // duration: 멈출 시간 (보통 0.05 ~ 0.1초 사용)
    // =========================================================
    public void HitStop(float duration)
    {
        // 이미 멈춰있다면 중복 실행 방지 (여러 마리 때릴 때 렉 걸리는 느낌 방지)
        if (isHitStopping) return;

        StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        isHitStopping = true;

        // 1. 시간을 멈춤
        Time.timeScale = 0.0f;

        // 2. 실제 시간(Realtime)으로 대기 (timeScale이 0이라 WaitForSeconds는 안 먹힘)
        yield return new WaitForSecondsRealtime(duration);

        // 3. 시간 원상복구
        Time.timeScale = 1.0f;

        isHitStopping = false;
    }
}