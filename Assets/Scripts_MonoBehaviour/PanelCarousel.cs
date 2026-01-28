using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

public class PanelCarousel : MonoBehaviour
{
    // ... (기존 변수들 유지) ...
    [Header("패널 목록 (순서: 스탯 - 아티팩트 - 밸런스)")]
    public List<RectTransform> panels;

    [Header("UI 연결")]
    public TextMeshProUGUI titleText;
    public HangingUI hangingPhysics;

    [Header("Sound Effects")] // ★ [추가] 사운드 변수
    [SerializeField] private AudioClip sfxSwap; // 3. 패널 넘어갈 때 (휘익- 철컥)

    [Header("설정")]
    // ... (기존 설정 변수들 유지) ...
    public float xOffset = 800f;
    public float sideScale = 0.6f;
    public float sideAlpha = 0.3f;
    public float moveSpeed = 10f;

    private int currentIndex = 1;
    private PlayerInput inputActions;
    private struct PanelTarget { public Vector2 pos; public Vector3 scale; public float alpha; }
    private PanelTarget[] targets;

    // ... Awake, OnEnable, OnDisable, OnMoveInput 등 기존 함수 유지 ...

    private void Awake()
    {
        targets = new PanelTarget[panels.Count];
        inputActions = new PlayerInput();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Move.performed += OnMoveInput;

        UpdateTargets();
        UpdateTitle();
        SnapToTarget();
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

        // ★ [추가] 넘김 소리 재생
        // 타임스케일이 0일 때도 들려야 하므로 SoundManager가 멈추지 않는지 확인 필요하지만,
        // 보통 AudioSource는 TimeScale 영향 안 받음 (PlaySFX 그대로 사용)
        SoundManager.instance.PlaySFX(sfxSwap, 1f);
    }

    // ... Update, UpdateTargets, UpdateTitle, SnapToTarget 등 기존 함수 유지 ...
    private void UpdateTitle()
    {
        if (titleText == null) return;
        switch (currentIndex)
        {
            case 0: titleText.text = "Status"; break;
            case 1: titleText.text = "Artifact"; break;
            case 2: titleText.text = "Balance"; break;
        }
    }

    private void Update()
    {
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
            CanvasGroup cg = panels[i].GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = targets[i].alpha;
        }
    }
}