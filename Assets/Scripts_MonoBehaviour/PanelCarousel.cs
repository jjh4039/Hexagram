using ChocDino.UIFX; // ★ GlowFilter 사용을 위해 필수
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PanelCarousel : MonoBehaviour
{
    [Header("패널 목록 (순서: 스탯 - 아티팩트 - 밸런스)")]
    public List<RectTransform> panels;

    [Header("UI 연결")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI titleDesText;

    // ★ [복구 완료] GlowFilter 연결 변수
    public GlowFilter titleTextGlow;
    public GlowFilter titleDesTextGlow;

    public HangingUI hangingPhysics;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip sfxSwap;

    [Header("설정")]
    public float xOffset = 800f;
    public float sideScale = 0.6f;
    public float sideAlpha = 0.3f;

    // ★ [최적화 1] Lerp 대신 SmoothDamp 시간 (0.15f 추천)
    [Range(0.01f, 1f)] public float smoothTime = 0.15f;

    // ★ [최적화 2] Sleep 모드 변수 (도착하면 연산 중지)
    private bool isIdle = false;
    private float snapThreshold = 0.1f;

    private int currentIndex = 1;
    private PlayerInput inputActions;
    private CanvasGroup[] panelCanvasGroups; // 캐싱용

    // SmoothDamp용 속도 변수들
    private Vector2[] velPositions;
    private Vector3[] velScales;
    private float[] velAlphas;

    private struct PanelTarget { public Vector2 pos; public Vector3 scale; public float alpha; }
    private PanelTarget[] targets;

    // ★ 메모리 최적화를 위한 컬러 캐싱 (매번 new Color 하지 않음)
    private readonly Color statusColor = new Color(0 / 255f, 20 / 255f, 20 / 255f);
    private readonly Color artifactColor = new Color(40 / 255f, 15 / 255f, 0 / 255f);
    private readonly Color balanceColor = new Color(30 / 255f, 0 / 255f, 25 / 255f);

    private void Awake()
    {
        int count = panels.Count;
        targets = new PanelTarget[count];
        inputActions = new PlayerInput();

        // 배열 초기화
        panelCanvasGroups = new CanvasGroup[count];
        velPositions = new Vector2[count];
        velScales = new Vector3[count];
        velAlphas = new float[count];

        // 컴포넌트 미리 찾아두기 (Update 성능 향상)
        for (int i = 0; i < count; i++)
        {
            panelCanvasGroups[i] = panels[i].GetComponent<CanvasGroup>();
            if (panelCanvasGroups[i] == null)
                panelCanvasGroups[i] = panels[i].gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Move.performed += OnMoveInput;

        UpdateTargets();
        UpdateTitle();
        SnapToTarget(); // 켜질 땐 즉시 이동
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMoveInput;
        inputActions.Disable();
    }

    private void OnMoveInput(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        if (input.x > 0.5f) MoveIndex(1);
        else if (input.x < -0.5f) MoveIndex(-1);
    }

    private void MoveIndex(int direction)
    {
        currentIndex += direction;

        if (currentIndex >= panels.Count) currentIndex = 0;
        else if (currentIndex < 0) currentIndex = panels.Count - 1;

        UpdateTargets();
        UpdateTitle();

        if (hangingPhysics != null) hangingPhysics.Push(direction * -20f);
        SoundManager.instance.PlaySFX(sfxSwap, 1f);

        // 움직임 시작! (계산 재개)
        isIdle = false;
    }

    private void UpdateTitle()
    {
        if (titleText == null) return;

        switch (currentIndex)
        {
            case 0: // Status
                titleText.text = "Status";
                if (titleDesText) titleDesText.text = "캐릭터의 스탯을 확인할 수 있습니다.";

                // ★ [복구 완료] Glow 색상 변경
                if (titleTextGlow) titleTextGlow.Color = statusColor;
                if (titleDesTextGlow) titleDesTextGlow.Color = statusColor;
                break;

            case 1: // Artifact
                titleText.text = "Artifact";
                if (titleDesText) titleDesText.text = "보유한 아티팩트들의 효과를 확인할 수 있습니다.";

                // ★ [복구 완료] Glow 색상 변경
                if (titleTextGlow) titleTextGlow.Color = artifactColor;
                if (titleDesTextGlow) titleDesTextGlow.Color = artifactColor;
                break;

            case 2: // Balance
                titleText.text = "Balance";
                if (titleDesText) titleDesText.text = "주사위 모듈에 관련된 정보들을 확인할 수 있습니다.";

                // ★ [복구 완료] Glow 색상 변경
                if (titleTextGlow) titleTextGlow.Color = balanceColor;
                if (titleDesTextGlow) titleDesTextGlow.Color = balanceColor;
                break;
        }
    }

    private void Update()
    {
        // ★ [최적화] 이미 다 도착했으면 연산 중지
        if (isIdle) return;

        bool allSettled = true;

        for (int i = 0; i < panels.Count; i++)
        {
            // 1. 위치 이동 (SmoothDamp)
            panels[i].anchoredPosition = Vector2.SmoothDamp(
                panels[i].anchoredPosition, targets[i].pos, ref velPositions[i], smoothTime, Mathf.Infinity, Time.unscaledDeltaTime
            );

            // 2. 크기 변경 (SmoothDamp)
            panels[i].localScale = Vector3.SmoothDamp(
                panels[i].localScale, targets[i].scale, ref velScales[i], smoothTime, Mathf.Infinity, Time.unscaledDeltaTime
            );

            // 3. 투명도 변경 (SmoothDamp + 캐싱된 컴포넌트 사용)
            if (panelCanvasGroups[i] != null)
            {
                panelCanvasGroups[i].alpha = Mathf.SmoothDamp(
                    panelCanvasGroups[i].alpha, targets[i].alpha, ref velAlphas[i], smoothTime, Mathf.Infinity, Time.unscaledDeltaTime
                );
            }

            // 도착 체크
            if (Vector2.Distance(panels[i].anchoredPosition, targets[i].pos) > snapThreshold)
            {
                allSettled = false;
            }
        }

        // 모두 도착했으면 Sleep 모드 전환
        if (allSettled)
        {
            isIdle = true;
            SnapToTarget(); // 위치 딱 맞추기
        }
    }

    private void UpdateTargets()
    {
        for (int i = 0; i < panels.Count; i++)
        {
            int diff = i - currentIndex;
            if (diff == -2) diff = 1;
            if (diff == 2) diff = -1;

            if (diff == 0) // 중앙
            {
                targets[i].pos = Vector2.zero;
                targets[i].scale = Vector3.one * 1.2f;
                targets[i].alpha = 1f;
            }
            else // 사이드
            {
                targets[i].pos = new Vector2(diff * xOffset, 0);
                targets[i].scale = Vector3.one * sideScale;
                targets[i].alpha = sideAlpha;
            }
        }
    }

    private void SnapToTarget()
    {
        for (int i = 0; i < panels.Count; i++)
        {
            panels[i].anchoredPosition = targets[i].pos;
            panels[i].localScale = targets[i].scale;
            if (panelCanvasGroups[i] != null) panelCanvasGroups[i].alpha = targets[i].alpha;

            // 속도 초기화 (안 하면 다음에 튐)
            velPositions[i] = Vector2.zero;
            velScales[i] = Vector3.zero;
                velAlphas[i] = 0f;
        }
    }
}