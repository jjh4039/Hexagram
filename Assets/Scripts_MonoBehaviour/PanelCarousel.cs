using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PanelCarousel : MonoBehaviour
{
    [Header("패널 목록 (순서: 스탯 - 아티팩트 - 밸런스)")]
    public List<RectTransform> panels;

    [Header("설정")]
    public float xOffset = 800f;   // 패널 간 간격
    public float sideScale = 0.6f; // 옆 패널 크기
    public float sideAlpha = 0.3f; // 옆 패널 투명도
    public float moveSpeed = 10f;  // 움직임 속도

    [Header("연결")]
    public HangingUI hangingPhysics; // 철커덩 효과용

    private int currentIndex = 1; // 1번(Artifacts)이 기본 중앙
    private PlayerInput inputActions; // ★ 최신 인풋

    // 목표 상태 저장용
    private struct PanelTarget { public Vector2 pos; public Vector3 scale; public float alpha; }
    private PanelTarget[] targets;

    private void Awake()
    {
        targets = new PanelTarget[panels.Count];
        inputActions = new PlayerInput(); // Input Action 클래스 생성
    }

    private void OnEnable()
    {
        inputActions.Enable();
        // ★ Player 맵의 Move 액션(A/D)을 구독
        inputActions.Player.Move.performed += OnMoveInput;

        UpdateTargets();
        SnapToTarget(); // 켜질 땐 애니메이션 없이 즉시 배치
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMoveInput;
        inputActions.Disable();
    }

    private void OnMoveInput(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();

        if (input.x > 0.5f) MoveIndex(1);       // D키 (오른쪽)
        else if (input.x < -0.5f) MoveIndex(-1); // A키 (왼쪽)
    }

    private void MoveIndex(int direction)
    {
        currentIndex += direction;

        // 순환 구조 (0 -> 1 -> 2 -> 0)
        if (currentIndex >= panels.Count) currentIndex = 0;
        else if (currentIndex < 0) currentIndex = panels.Count - 1;

        UpdateTargets();

        // ★ 물리 효과: 판 넘길 때 철커덩! (방향 반대로 밀기)
        if (hangingPhysics != null) hangingPhysics.Push(direction * -20f);
    }

    private void Update()
    {
        // 매 프레임 부드럽게 이동 (TimeScale 무시)
        float dt = Time.unscaledDeltaTime * moveSpeed;

        for (int i = 0; i < panels.Count; i++)
        {
            panels[i].anchoredPosition = Vector2.Lerp(panels[i].anchoredPosition, targets[i].pos, dt);
            panels[i].localScale = Vector3.Lerp(panels[i].localScale, targets[i].scale, dt);

            CanvasGroup cg = panels[i].GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = Mathf.Lerp(cg.alpha, targets[i].alpha, dt);
        }
    }

    private void UpdateTargets()
    {
        for (int i = 0; i < panels.Count; i++)
        {
            int diff = i - currentIndex;
            // 순환 거리 계산 (3개 기준)
            if (diff == -2) diff = 1;
            if (diff == 2) diff = -1;

            if (diff == 0) // 중앙
            {
                targets[i].pos = Vector2.zero;
                targets[i].scale = Vector3.one;
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
            CanvasGroup cg = panels[i].GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = targets[i].alpha;
        }
    }
}