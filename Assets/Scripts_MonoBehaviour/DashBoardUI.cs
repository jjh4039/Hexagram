using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections; // 코루틴 사용을 위해 필수

public class DashboardUI : MonoBehaviour
{
    public static DashboardUI instance;

    [Header("Main Objects")]
    public GameObject dashboardPanel;   // 전체 팝업 (검은 배경 + 판)
    public CanvasGroup dashboardCG;     // ★ [필수] 투명도 조절용 (인스펙터에서 연결!)
    public Transform artifactGrid;      // 아티팩트가 생성될 그리드
    public GameObject slotPrefab;       // 슬롯 프리팹

    [Header("Animation Settings")]
    public float fadeDuration = 0.2f;   // 페이드 시간 (0.2초 추천)
    public Vector3 startScale = new Vector3(0.9f, 0.9f, 1f); // 시작/종료 크기

    [Header("Tooltip")]
    public GameObject tooltipGroup;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public Vector2 tooltipOffset = new Vector2(15f, -15f);

    [Header("Sound Effects")]
    [SerializeField] private AudioClip sfxOpen;   // 1. 창 열릴 때
    [SerializeField] private AudioClip sfxClose;  // 1. 창 닫힐 때
    [SerializeField] private AudioClip sfxHover;  // 2. 툴팁 뜰 때

    private PlayerInput inputActions;
    public bool isOpen = false;
    private bool isTooltipActive = false;
    private Coroutine fadeRoutine; // 실행 중인 애니메이션 관리용

    private void Awake()
    {
        instance = this;

        // 시작할 때 꺼두기 및 초기화
        if (dashboardCG == null) dashboardCG = dashboardPanel.GetComponent<CanvasGroup>();
        if (dashboardCG != null)
        {
            dashboardCG.alpha = 0f;
            dashboardCG.blocksRaycasts = false; // 클릭 방지
        }

        dashboardPanel.SetActive(false);
        if (tooltipGroup != null) tooltipGroup.SetActive(false);

        inputActions = new PlayerInput();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Inventory.performed += OnToggle;
    }

    private void OnDisable()
    {
        inputActions.Player.Inventory.performed -= OnToggle;
        inputActions.Disable();
    }

    private void Update()
    {
        // 툴팁이 켜져있으면 마우스 따라다니기
        if (isOpen && isTooltipActive && tooltipGroup != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            tooltipGroup.transform.position = mousePos + tooltipOffset;
        }
    }

    private void OnToggle(InputAction.CallbackContext context)
    {
        // 켜져있으면 끄고, 꺼져있으면 켠다
        if (isOpen) CloseDashboard();
        else OpenDashboard();
    }

    public void OpenDashboard()
    {
        isOpen = true;
        dashboardPanel.SetActive(true);
        Time.timeScale = 0f; // 시간 정지 (게임 멈춤)

        RefreshArtifacts(); // 슬롯 갱신
        SoundManager.instance.PlaySFX(sfxOpen, 1.0f);

        // ★ 열기 애니메이션 시작 (이전 애니메이션 취소)
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(true));
    }

    public void CloseDashboard()
    {
        isOpen = false;
        HideTooltip();

        SoundManager.instance.PlaySFX(sfxClose, 1.0f);

        // ★ 닫기 애니메이션 시작
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(false));
    }

    // ★ 페이드 인/아웃 + 스케일 업/다운 코루틴
    private IEnumerator FadeRoutine(bool show)
    {
        float timer = 0f;

        // Alpha 설정 (0 <-> 1)
        float startAlpha = dashboardCG.alpha;
        float targetAlpha = show ? 1f : 0f;

        // Scale 설정 (0.9 <-> 1.0)
        // 켤 때: 0.9 -> 1.0 (커짐)
        // 끌 때: 1.0 -> 0.9 (작아짐)
        Vector3 fromScale = show ? startScale : Vector3.one;
        Vector3 toScale = show ? Vector3.one : startScale;

        // 켜질 때는 바로 클릭 허용, 꺼질 때는 차단
        if (show) dashboardCG.blocksRaycasts = true;
        else dashboardCG.blocksRaycasts = false;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime; // TimeScale 0이어도 작동하도록 unscaled 사용
            float t = timer / fadeDuration;

            // 부드러운 움직임 (Ease Out Sine)
            t = Mathf.Sin(t * Mathf.PI * 0.5f);

            // 투명도 조절
            dashboardCG.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            // 크기 조절 (열 때도, 닫을 때도 작동)
            dashboardPanel.transform.localScale = Vector3.Lerp(fromScale, toScale, t);

            yield return null;
        }

        // 애니메이션 종료 후 값 확정
        dashboardCG.alpha = targetAlpha;
        dashboardPanel.transform.localScale = toScale;

        if (!show)
        {
            dashboardPanel.SetActive(false);
            Time.timeScale = 1f; // ★ 완전히 닫힌 후에 시간 다시 흐르게 함 (안정성 UP)
        }
    }

    // 아티팩트 목록 갱신
    public void RefreshArtifacts()
    {
        foreach (Transform child in artifactGrid) Destroy(child.gameObject);
        foreach (ArtifactData data in ArtifactManager.instance.myArtifacts)
        {
            GameObject newSlot = Instantiate(slotPrefab, artifactGrid);
            newSlot.GetComponent<ArtifactSlot>().Setup(data);
        }
    }

    // 아티팩트용 툴팁 표시
    public void ShowTooltip(ArtifactData data)
    {
        if (tooltipGroup == null) return;

        // 내용이 바뀔 때 소리 재생 (이미 켜져있어도 다른 아이템이면 소리 남)
        // 너무 시끄러우면 if(!isTooltipActive) 조건 추가하세요.
        SoundManager.instance.PlaySFX(sfxHover, 0.3f, 0.1f);

        isTooltipActive = true;
        tooltipGroup.SetActive(true);
        nameText.text = data.artifactName;

        string colorHex = (data.grade == ArtifactGrade.Legendary) ? "#FFD000" :
                          (data.grade == ArtifactGrade.Epic) ? "#B591D1" :
                          (data.grade == ArtifactGrade.Rare) ? "#4AA8D8" : "#FFFFFF";
        descText.text = $"<color={colorHex}>[ {data.grade} ]</color>\n\n{data.description}";
    }

    // 범용(밸런스/스탯 등) 툴팁 표시
    public void ShowTooltipCommon(string title, string content)
    {
        if (tooltipGroup == null) return;

        SoundManager.instance.PlaySFX(sfxHover, 0.3f, 0.1f);

        isTooltipActive = true;
        tooltipGroup.SetActive(true);
        nameText.text = title;
        descText.text = content;
    }

    // 툴팁 숨기기
    public void HideTooltip()
    {
        if (tooltipGroup == null) return;
        isTooltipActive = false;
        tooltipGroup.SetActive(false);
    }
}