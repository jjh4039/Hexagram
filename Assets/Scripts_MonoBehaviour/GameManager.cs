using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public Player player;
    public PlayerStats stats;
    public WeaponUI weaponUI;
    public Dice dice;
    public MapManager mapManager;  // 맵 매니저 (맵 끄고 켤 때 필요)

    public GameObject currentStageObj; // 현재 소환된 스테이지 (지울 때 필요)

    public int currentProgress = 0;
    public int maxProgress = 100;

    void Awake()
    {
        instance = this;
    }

    public void LoadStage(StageData stageData)
    {
        // 1. 기존 스테이지 청소
        if (currentStageObj != null)
        {
            Destroy(currentStageObj);
        }

        // 2. 맵 UI 닫기 (MapManager가 있다면)
        if (mapManager != null) mapManager.mapVisualRoot.SetActive(false);

        // ★ [핵심 수정] 랜덤 뽑기 로직
        if (stageData.stagePrefabs != null && stageData.stagePrefabs.Length > 0)
        {
            // 0번부터 개수-1 사이의 랜덤한 번호를 하나 뽑음
            int randomIndex = Random.Range(0, stageData.stagePrefabs.Length);

            // 그 번호에 해당하는 프리펩을 선택
            GameObject selectedPrefab = stageData.stagePrefabs[randomIndex];

            // 선택된 프리펩 소환
            currentStageObj = Instantiate(selectedPrefab, Vector3.zero, Quaternion.identity);

            // 스테이지 초기화 (플레이어 이동 등)
            StageController controller = currentStageObj.GetComponent<StageController>();
            if (controller != null)
            {
                controller.InitStage();
            }
        }
        else
        {
            Debug.LogError($"오류: {stageData.stageName} 데이터에 연결된 프리펩이 하나도 없습니다!");
        }
    }
}
    