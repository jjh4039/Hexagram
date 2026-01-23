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

            // ★ [수정] 이름 변경 (StageEntryUI -> StageMessageUI)
            if (StageMessageUI.instance != null)
            {
                StageMessageUI.instance.ShowEntryMessage("모듈 : " + stageData.stageName, stageData.description);
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

    // ★ [수정] 텍스트 펀치 연출 (약하게!)
    private IEnumerator ScrapTextPunch()
    {
        // 시간도 아주 살짝 짧게 (0.15 -> 0.12)
        float duration = 0.12f;
        float elapsed = 0f;

        // ★ [핵심 수정] 1.3배 -> 1.1배 (아주 살짝만 커짐)
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
}