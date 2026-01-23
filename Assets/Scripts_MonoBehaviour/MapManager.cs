using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using ChocDino.UIFX;
using UnityEngine.InputSystem;

public class MapManager : MonoBehaviour
{
    [Header("--- Stage Data ---")]
    public StageData[] currentNodes;
    private int selectedIndex = 0;

    [Header("--- UI References ---")]
    public GameObject mapVisualRoot;
    public Image fadeOverlayImage;
    public RectTransform stageTextRect;
    public TextMeshProUGUI stageTitleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI stagePerText;
    public TextMeshProUGUI totalProgressText;
    private CanvasGroup stageTextCanvasGroup;

    [Header("--- Visual Elements (3 Each) ---")]
    public Image[] nodeVisuals;
    public Image[] lineVisuals;
    private CanvasGroup[] nodeCanvasGroups;

    [Header("--- Glow Filters ---")]
    public GlowFilter titleTextGlow;
    public GlowFilter perTextGlow;
    public GlowFilter[] nodeGlows;
    public GlowFilter[] lineGlows;

    [Header("--- Animation Settings ---")]
    [SerializeField] private float lerpSpeed = 12f;
    [SerializeField] private float floatAmount = 15f;
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float nodeFadeSpeed = 4.5f;
    [SerializeField] private float scanInterval = 0.12f;

    [Header("Sound")]
    [SerializeField] private AudioClip sfxSelect;
    [SerializeField] private AudioClip sfxScan;
    [SerializeField] private AudioClip sfxCount; // ★ [추가] 카운팅 사운드 (틱! 소리)

    private int[] currentRandomPers = new int[3];

    private PlayerInput inputActions;
    private readonly Color activeColor = Color.white;
    private readonly Color inactiveColor = new Color(70 / 255f, 70 / 255f, 70 / 255f);

    private Vector2[] nodeOriginPos;
    private Vector2[] lineOriginPos;
    private Coroutine fadeCoroutine;
    private bool isScanning = false;

    private void Awake()
    {
        inputActions = new PlayerInput();
        nodeOriginPos = new Vector2[nodeVisuals.Length];
        lineOriginPos = new Vector2[lineVisuals.Length];
        nodeCanvasGroups = new CanvasGroup[nodeVisuals.Length];

        if (stageTextRect != null)
        {
            stageTextCanvasGroup = stageTextRect.GetComponent<CanvasGroup>();
            if (stageTextCanvasGroup == null) stageTextCanvasGroup = stageTextRect.gameObject.AddComponent<CanvasGroup>();
        }

        for (int i = 0; i < nodeVisuals.Length; i++)
        {
            if (nodeVisuals[i] != null)
            {
                nodeOriginPos[i] = nodeVisuals[i].rectTransform.anchoredPosition;
                nodeCanvasGroups[i] = nodeVisuals[i].GetComponent<CanvasGroup>();
                if (nodeCanvasGroups[i] == null) nodeCanvasGroups[i] = nodeVisuals[i].gameObject.AddComponent<CanvasGroup>();
            }

            if (i < lineVisuals.Length && lineVisuals[i] != null)
            {
                lineOriginPos[i] = lineVisuals[i].rectTransform.anchoredPosition;
            }
        }
    }

    private void OnEnable() { inputActions.Enable(); inputActions.Player.Move.performed += ctx => OnNavigate(ctx.ReadValue<Vector2>()); inputActions.Player.Dash.performed += _ => OnEnterStage(); }
    private void OnDisable() => inputActions.Disable();

    private void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame) ToggleMap();
        if (mapVisualRoot == null || !mapVisualRoot.activeSelf) return;
        HandleSmoothVisuals();
    }

    public void ToggleMap()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        bool isOpening = !mapVisualRoot.activeSelf;
        fadeCoroutine = StartCoroutine(MapFadeRoutine(isOpening));
    }

    private IEnumerator MapFadeRoutine(bool isOpening)
    {
        float halfFade = fadeDuration * 0.5f;
        yield return StartCoroutine(FadeOverlay(0f, 1f, halfFade));

        if (isOpening)
        {
            ResetVisualsState();
            mapVisualRoot.SetActive(true);
            StartCoroutine(ScanSequence());
            yield return new WaitForSeconds(0.05f);
            yield return StartCoroutine(FadeOverlay(1f, 0f, halfFade));
        }
        else
        {
            isScanning = false;
            mapVisualRoot.SetActive(false);
            yield return StartCoroutine(FadeOverlay(1f, 0f, halfFade));
        }
        fadeCoroutine = null;
    }

    private void ResetVisualsState()
    {
        selectedIndex = 0;
        isScanning = true;

        if (titleTextGlow != null) titleTextGlow.enabled = true;
        if (perTextGlow != null) perTextGlow.enabled = true;
        if (stageTextCanvasGroup != null) stageTextCanvasGroup.alpha = 0f;

        for (int i = 0; i < 3; i++)
        {
            if (nodeGlows[i] != null) nodeGlows[i].enabled = false;
            if (lineGlows[i] != null) lineGlows[i].enabled = false;

            nodeCanvasGroups[i].alpha = 0f;
            nodeVisuals[i].rectTransform.anchoredPosition = nodeOriginPos[i];

            if (nodeVisuals[i] != null) nodeVisuals[i].color = inactiveColor;
            if (lineVisuals[i] != null)
            {
                lineVisuals[i].rectTransform.anchoredPosition = lineOriginPos[i];
                lineVisuals[i].color = new Color(inactiveColor.r, inactiveColor.g, inactiveColor.b, 0f);
            }

            if (currentNodes.Length > i && currentNodes[i] != null)
            {
                StageData data = currentNodes[i];
                currentRandomPers[i] = Random.Range(data.minRise, data.maxRise + 1);
            }
        }
        stageTextRect.anchoredPosition = new Vector2(-160f, 0);
    }

    private IEnumerator FadeOverlay(float start, float end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (fadeOverlayImage != null)
                fadeOverlayImage.color = new Color(0, 0, 0, Mathf.Lerp(start, end, elapsed / duration));
            yield return null;
        }
        if (fadeOverlayImage != null) fadeOverlayImage.color = new Color(0, 0, 0, end);
    }

    private IEnumerator ScanSequence()
    {
        for (int i = 0; i < nodeVisuals.Length; i++)
        {
            StageData data = currentNodes[i];
            if (stageTitleText != null) stageTitleText.text = $"모듈 : {data.stageName}";

            StartCoroutine(FadeInNode(i));
            if (sfxScan != null) SoundManager.instance.PlaySFX(sfxScan, 0.15f);
            yield return new WaitForSeconds(scanInterval);
        }

        isScanning = false;
        UpdateUI();
    }

    private IEnumerator FadeInNode(int index)
    {
        float alpha = 0f;
        while (alpha < 1f)
        {
            alpha += Time.deltaTime * nodeFadeSpeed;
            if (nodeCanvasGroups[index] != null) nodeCanvasGroups[index].alpha = alpha;
            if (lineVisuals[index] != null)
                lineVisuals[index].color = new Color(inactiveColor.r, inactiveColor.g, inactiveColor.b, alpha);
            yield return null;
        }
        if (nodeCanvasGroups[index] != null) nodeCanvasGroups[index].alpha = 1f;
    }

    private void HandleSmoothVisuals()
    {
        if (isScanning) return;

        if (stageTextCanvasGroup != null)
            stageTextCanvasGroup.alpha = Mathf.Lerp(stageTextCanvasGroup.alpha, 1f, Time.deltaTime * lerpSpeed);

        float targetX = (selectedIndex - 1) * 160f;
        stageTextRect.anchoredPosition = Vector2.Lerp(stageTextRect.anchoredPosition, new Vector2(targetX, 0), Time.deltaTime * lerpSpeed);

        for (int i = 0; i < 3; i++)
        {
            bool isSelected = (i == selectedIndex);
            Color targetColor = isSelected ? activeColor : inactiveColor;

            if (nodeVisuals[i] != null)
                nodeVisuals[i].color = Color.Lerp(nodeVisuals[i].color, targetColor, Time.deltaTime * lerpSpeed);
            if (lineVisuals[i] != null)
                lineVisuals[i].color = Color.Lerp(lineVisuals[i].color, targetColor, Time.deltaTime * lerpSpeed);

            Vector2 targetNodePos = isSelected ? nodeOriginPos[i] + Vector2.up * floatAmount : nodeOriginPos[i];
            nodeVisuals[i].rectTransform.anchoredPosition = Vector2.Lerp(nodeVisuals[i].rectTransform.anchoredPosition, targetNodePos, Time.deltaTime * lerpSpeed);

            if (lineVisuals[i] != null)
            {
                float currentYOffset = nodeVisuals[i].rectTransform.anchoredPosition.y - nodeOriginPos[i].y;
                float parentScaleY = nodeVisuals[i].rectTransform.localScale.y;
                if (parentScaleY == 0) parentScaleY = 1f;
                float compensatedOffset = currentYOffset / parentScaleY;
                lineVisuals[i].rectTransform.anchoredPosition = lineOriginPos[i] - new Vector2(0, compensatedOffset);
            }
        }
    }

    private void OnNavigate(Vector2 direction) { if (mapVisualRoot == null || !mapVisualRoot.activeSelf || isScanning) return; if (direction.x < -0.5f) ChangeSelection(-1); else if (direction.x > 0.5f) ChangeSelection(1); }

    private void ChangeSelection(int dir)
    {
        int prevIndex = selectedIndex;
        selectedIndex = Mathf.Clamp(selectedIndex + dir, 0, currentNodes.Length - 1);

        if (prevIndex != selectedIndex)
        {
            UpdateUI();
            if (sfxSelect != null) SoundManager.instance.PlaySFX(sfxSelect, 0.1f);
        }
    }

    private void UpdateUI()
    {
        if (currentNodes.Length == 0) return;
        StageData data = currentNodes[selectedIndex];

        if (stageTitleText != null) stageTitleText.text = $"모듈 : {data.stageName}";

        if (stagePerText != null)
        {
            stagePerText.text = $"+ {currentRandomPers[selectedIndex]}%";
        }

        if (descriptionText != null) descriptionText.text = data.description;

        if (titleTextGlow != null)
        {
            titleTextGlow.enabled = true;
            perTextGlow.enabled = true;
            titleTextGlow.Color = data.themeColor;
            perTextGlow.Color = data.themeColor;
        }

        if (!isScanning)
        {
            for (int i = 0; i < 3; i++)
            {
                bool isSelected = (i == selectedIndex);
                if (i < nodeGlows.Length && nodeGlows[i] != null)
                {
                    nodeGlows[i].enabled = isSelected;
                    if (isSelected) nodeGlows[i].Color = data.themeColor;
                }
                if (i < lineGlows.Length && lineGlows[i] != null) lineGlows[i].enabled = isSelected;
            }
        }
    }

    private void OnEnterStage()
    {
        if (mapVisualRoot == null || !mapVisualRoot.activeSelf || fadeCoroutine != null || isScanning) return;
        if (currentNodes.Length == 0) return;
        StartCoroutine(ProcessSelectSequence());
    }

    private IEnumerator ProcessSelectSequence()
    {
        StageData selectedData = currentNodes[selectedIndex];
        int gainPer = currentRandomPers[selectedIndex];
        int startTotalPer = GameManager.instance.currentProgress;
        int targetTotalPer = Mathf.Clamp(startTotalPer + gainPer, 0, 100);

        if (sfxSelect != null) SoundManager.instance.PlaySFX(sfxSelect, 0.1f);

        // 2. 숫자 카운팅 연출 (0.5초)
        float duration = 0.5f;
        float elapsed = 0f;

        // ★ [추가] 사운드 재생 타이머
        float soundTimer = 0f;
        float soundInterval = 0.07f; // 0.07초마다 틱 소리

        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            soundTimer += dt;

            // ★ 일정 간격마다 카운팅 소리 재생
            if (soundTimer >= soundInterval)
            {
                soundTimer = 0f;
                if (sfxCount != null) SoundManager.instance.PlaySFX(sfxCount, 0.9f, 0.1f);
            }

            float t = elapsed / duration;

            int currentNodeVal = (int)Mathf.Lerp(gainPer, 0, t);
            if (stagePerText != null) stagePerText.text = $"+ {currentNodeVal}%";

            int currentTotalVal = (int)Mathf.Lerp(startTotalPer, targetTotalPer, t);
            if (totalProgressText != null) totalProgressText.text = $"진행도 : <color=#80FF80>봄_{currentTotalVal}%</color>";

            yield return null;
        }

        if (stagePerText != null) stagePerText.text = "";
        GameManager.instance.currentProgress = targetTotalPer;

        yield return new WaitForSeconds(0.2f);

        yield return StartCoroutine(FadeOverlay(0f, 1f, 0.5f));

        mapVisualRoot.SetActive(false);
        isScanning = false;

        if (GameManager.instance != null)
        {
            GameManager.instance.LoadStage(selectedData);
        }

        yield return new WaitForSeconds(0.1f);

        yield return StartCoroutine(FadeOverlay(1f, 0f, 0.5f));
    }
}