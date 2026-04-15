using ChocDino.UIFX; 
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

    public GlowFilter titleTextGlow;
    public GlowFilter titleDesTextGlow;

    public HangingUI hangingPhysics;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip sfxSwap;

    [Header("설정")]
    public float xOffset = 300f;
    public float sideScale = 0.7f;
    public float sideAlpha = 0.1f;
    
    [Range(0.01f, 1f)] public float smoothTime = 0.13f;
    
    private bool _isIdle = false;
    private readonly float _snapThreshold = 0.1f;

    private int _currentIndex = 1;
    private CanvasGroup[] _panelCanvasGroups; 

    private Vector2[] velPositions;
    private Vector3[] velScales;
    private float[] velAlphas;

    private struct PanelTarget { public Vector2 pos; public Vector3 scale; public float alpha; }
    private PanelTarget[] targets;

    private readonly Color statusColor = new Color(0 / 255f, 20 / 255f, 20 / 255f);
    private readonly Color artifactColor = new Color(40 / 255f, 15 / 255f, 0 / 255f);
    private readonly Color balanceColor = new Color(30 / 255f, 0 / 255f, 25 / 255f);

    private void Awake()
    {
        int count = panels.Count;
        targets = new PanelTarget[count];

        _panelCanvasGroups = new CanvasGroup[count];
        velPositions = new Vector2[count];
        velScales = new Vector3[count];
        velAlphas = new float[count];

        for (int i = 0; i < count; i++)
        {
            _panelCanvasGroups[i] = panels[i].GetComponent<CanvasGroup>();
            if (_panelCanvasGroups[i] == null)
                _panelCanvasGroups[i] = panels[i].gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.Actions.UI.MoveUI.performed += OnMoveInput;
        }

        UpdateTargets();
        UpdateTitle();
        SnapToTarget(); 
    }

    private void OnDisable()
    {
        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.Actions.UI.MoveUI.performed -= OnMoveInput;
        }
    }

    private void OnMoveInput(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        if (input.x > 0.5f) MoveIndex(1);
        else if (input.x < -0.5f) MoveIndex(-1);
    }

    private void MoveIndex(int direction)
    {
        _currentIndex += direction;

        if (_currentIndex >= panels.Count) _currentIndex = 0;
        else if (_currentIndex < 0) _currentIndex = panels.Count - 1;

        UpdateTargets();
        UpdateTitle();

        if (hangingPhysics != null) hangingPhysics.Push(direction * -20f);
        SoundManager.instance.PlaySFX(sfxSwap, 1f);

        _isIdle = false;
    }

    private void UpdateTitle()
    {
        if (titleText == null) return;

        switch (_currentIndex)
        {
            case 0: 
                titleText.text = "Status";
                if (titleDesText) titleDesText.text = "캐릭터의 스탯을 확인할 수 있습니다.";

                if (titleTextGlow) titleTextGlow.Color = statusColor;
                if (titleDesTextGlow) titleDesTextGlow.Color = statusColor;
                break;

            case 1: 
                titleText.text = "Artifact";
                if (titleDesText) titleDesText.text = "보유한 아티팩트들의 효과를 확인할 수 있습니다.";

                if (titleTextGlow) titleTextGlow.Color = artifactColor;
                if (titleDesTextGlow) titleDesTextGlow.Color = artifactColor;
                break;

            case 2: 
                titleText.text = "Balance";
                if (titleDesText) titleDesText.text = "주사위 모듈에 관련된 정보들을 확인할 수 있습니다.";

                if (titleTextGlow) titleTextGlow.Color = balanceColor;
                if (titleDesTextGlow) titleDesTextGlow.Color = balanceColor;
                break;
        }
    }

    private void Update()
    {
        if (_isIdle) return;

        bool allSettled = true;

        for (int i = 0; i < panels.Count; i++)
        {
            panels[i].anchoredPosition = Vector2.SmoothDamp(
                panels[i].anchoredPosition, targets[i].pos, ref velPositions[i], smoothTime, Mathf.Infinity, Time.unscaledDeltaTime
            );

            panels[i].localScale = Vector3.SmoothDamp(
                panels[i].localScale, targets[i].scale, ref velScales[i], smoothTime, Mathf.Infinity, Time.unscaledDeltaTime
            );

            if (_panelCanvasGroups[i])
            {
                _panelCanvasGroups[i].alpha = Mathf.SmoothDamp(
                    _panelCanvasGroups[i].alpha, targets[i].alpha, ref velAlphas[i], smoothTime, Mathf.Infinity, Time.unscaledDeltaTime
                );
            }

            if (Vector2.Distance(panels[i].anchoredPosition, targets[i].pos) > _snapThreshold)
            {
                allSettled = false;
            }
        }

        if (allSettled)
        {
            _isIdle = true;
            SnapToTarget(); 
        }
    }

    private void UpdateTargets()
    {
        for (int i = 0; i < panels.Count; i++)
        {
            int diff = i - _currentIndex;
            if (diff == -2) diff = 1;
            if (diff == 2) diff = -1;

            if (diff == 0) 
            {
                targets[i].pos = Vector2.zero;
                targets[i].scale = Vector3.one * 1.2f;
                targets[i].alpha = 1f;
                
                if (_panelCanvasGroups[i]) _panelCanvasGroups[i].blocksRaycasts = true; 
            }
            else 
            {
                targets[i].pos = new Vector2(diff * xOffset, 0);
                targets[i].scale = Vector3.one * sideScale;
                targets[i].alpha = sideAlpha;
                
                if (_panelCanvasGroups[i]) _panelCanvasGroups[i].blocksRaycasts = false;
            }
        }
    }

    private void SnapToTarget()
    {
        for (int i = 0; i < panels.Count; i++)
        {
            panels[i].anchoredPosition = targets[i].pos;
            panels[i].localScale = targets[i].scale;
            if (_panelCanvasGroups[i]) _panelCanvasGroups[i].alpha = targets[i].alpha;

            velPositions[i] = Vector2.zero;
            velScales[i] = Vector3.zero;
            velAlphas[i] = 0f;
        }
    }
}