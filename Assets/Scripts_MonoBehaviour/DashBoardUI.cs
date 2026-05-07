using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class DashboardUI : MonoBehaviour
{
    public static DashboardUI instance;

    [Header("Main Objects")]
    public GameObject dashboardPanel;   // 전체 팝업 창
    public CanvasGroup dashboardCG;     // 투명도 조절용 컴포넌트
    public Transform artifactGrid;      // 아티팩트가 생성될 그리드
    public GameObject slotPrefab;       // 슬롯 프리팹

    [Header("Animation Settings")]
    public float fadeDuration = 0.2f;   // 애니메이션 재생 시간
    public Vector3 startScale = new Vector3(0.9f, 0.9f, 1f); // 시작과 종료 시점의 크기

    [Header("Tooltip")]
    public GameObject tooltipGroup;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public Vector2 tooltipOffset = new Vector2(15f, -15f);

    [Header("Tooltip Colors")]
    public string hexLegendary = "#FFD000"; // 전설 등급 헥스코드
    public string hexEpic = "#B591D1";      // 에픽 등급 헥스코드
    public string hexRare = "#4AA8D8";      // 레어 등급 헥스코드
    public string hexNormal = "#FFFFFF";    // 일반 등급 헥스코드

    [Header("Sound Effects")]
    [SerializeField] private AudioClip sfxOpen;   // 창이 열릴 때 재생할 소리
    [SerializeField] private AudioClip sfxClose;  // 창이 닫힐 때 재생할 소리
    [SerializeField] private AudioClip sfxHover;  // 마우스를 올렸을 때 재생할 소리

    public bool isOpen = false;
    private bool isTooltipActive = false;
    private Coroutine fadeRoutine; // 실행 중인 애니메이션 관리 객체

    private void Awake()
    {
        instance = this;

        if (dashboardCG == null) dashboardCG = dashboardPanel.GetComponent<CanvasGroup>();
        if (dashboardCG != null)
        {
            dashboardCG.alpha = 0f;
            dashboardCG.blocksRaycasts = false;
        }

        dashboardPanel.SetActive(false);
        if (tooltipGroup != null) tooltipGroup.SetActive(false);

        // 기존의 독자적인 입력 시스템 생성 코드 삭제
    }

    // 싱글톤 초기화를 보장하기 위해 OnEnable 대신 Start 사용
    private void Start()
    {
        if (InputStateManager.Instance == null) return;

        var actions = InputStateManager.Instance.Actions;

        // 평상시와 전투 상태일 때 인벤토리 키를 누르면 열기 시도
        actions.Normal.Inventory.performed += OnInventoryPressed;
        actions.Combat.Inventory.performed += OnInventoryPressed;

        // UI 상태일 때 닫기 키를 누르면 창 닫기
        actions.UI.CloseInventory.performed += OnCloseUIPressed;
    }

    // OnDisable 대신 OnDestroy에서 메모리 해제
    private void OnDestroy()
    {
        if (InputStateManager.Instance == null) return;

        var actions = InputStateManager.Instance.Actions;

        actions.Normal.Inventory.performed -= OnInventoryPressed;
        actions.Combat.Inventory.performed -= OnInventoryPressed;
        actions.UI.CloseInventory.performed -= OnCloseUIPressed;
    }

    private void Update()
    {
        if (isOpen && isTooltipActive && tooltipGroup != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            tooltipGroup.transform.position = mousePos + tooltipOffset;
        }
    }

    // 인벤토리 열기 시도 콜백
    private void OnInventoryPressed(InputAction.CallbackContext context)
    {
        if (isOpen) return;

        if (InputStateManager.Instance.TryOpenUI())
        {
            OpenDashboard();
        }
        else
        {
            // [교체됨] 디버그 로그 대신 화면에 플로팅 텍스트 띄우기 (0번: 전투 중 불가)
            if (PlayerFeedbackUI.Instance != null)
                PlayerFeedbackUI.Instance.ShowWarning(0);
        }
    }

    // 인벤토리 닫기 시도 콜백
    private void OnCloseUIPressed(InputAction.CallbackContext context)
    {
        if (isOpen)
        {
            CloseDashboard();
            InputStateManager.Instance.CloseUI(); // 매니저에게 이전 상태로 복귀 요청
        }
    }

    public void OpenDashboard()
    {
        isOpen = true;
        dashboardPanel.SetActive(true);
        Time.timeScale = 0f;

        RefreshArtifacts();
        if (SoundManager.instance) SoundManager.instance.PlaySFX(sfxOpen, 1.0f);

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(true));
    }

    public void CloseDashboard()
    {
        isOpen = false;
        HideTooltip();

        if (SoundManager.instance) SoundManager.instance.PlaySFX(sfxClose, 1.0f);

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(false));
    }

    private IEnumerator FadeRoutine(bool show)
    {
        float timer = 0f;

        float startAlpha = dashboardCG.alpha;
        float targetAlpha = show ? 1f : 0f;

        Vector3 fromScale = show ? startScale : Vector3.one;
        Vector3 toScale = show ? Vector3.one : startScale;

        dashboardCG.blocksRaycasts = show;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / fadeDuration;

            t = Mathf.Sin(t * Mathf.PI * 0.5f);

            dashboardCG.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            dashboardPanel.transform.localScale = Vector3.Lerp(fromScale, toScale, t);

            yield return null;
        }

        dashboardCG.alpha = targetAlpha;
        dashboardPanel.transform.localScale = toScale;

        if (!show)
        {
            dashboardPanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    public void RefreshArtifacts()
    {
        if (artifactGrid == null) return;

        for (int i = artifactGrid.childCount - 1; i >= 0; i--)
        {
            Transform child = artifactGrid.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        if (ArtifactManager.instance == null) return;

        foreach (ArtifactData data in ArtifactManager.instance.myArtifacts)
        {
            GameObject newSlot = Instantiate(slotPrefab, artifactGrid);

            newSlot.transform.localScale = Vector3.one;

            newSlot.GetComponent<ArtifactSlot>().Setup(data);
        }
    }

    public void ShowTooltip(ArtifactData data)
    {
        if (tooltipGroup == null) return;

        if (SoundManager.instance) SoundManager.instance.PlaySFX(sfxHover, 0.3f, 0.1f);

        isTooltipActive = true;
        tooltipGroup.SetActive(true);
        nameText.text = data.artifactName;

        string colorHex = (data.grade == ArtifactGrade.Legendary) ? hexLegendary :
                          (data.grade == ArtifactGrade.Epic) ? hexEpic :
                          (data.grade == ArtifactGrade.Rare) ? hexRare : hexNormal;

        descText.text = $"<color={colorHex}>[ {data.grade} ]</color>\n\n{data.description}";
    }

    public void ShowTooltipCommon(string title, string content)
    {
        if (tooltipGroup == null) return;

        if (SoundManager.instance) SoundManager.instance.PlaySFX(sfxHover, 0.3f, 0.1f);

        isTooltipActive = true;
        tooltipGroup.SetActive(true);
        nameText.text = title;
        descText.text = content;
    }

    public void HideTooltip()
    {
        if (tooltipGroup == null) return;
        isTooltipActive = false;
        tooltipGroup.SetActive(false);
    }
}