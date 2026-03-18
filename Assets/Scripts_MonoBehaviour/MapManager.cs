using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic; // List 사용을 위해 추가
using System.Linq; // 리스트 처리를 위해 추가
using ChocDino.UIFX;
using UnityEngine.InputSystem;

public class MapManager : MonoBehaviour
{
    [System.Serializable]
    public class StageProbability
    {
        public StageData stageData;
        [Range(0, 100)] public float weight = 10f; // 등장 확률 가중치
    }

    [Header("--- Stage Pool Settings ---")]
    public List<StageProbability> stagePool; // 6가지 일반 모듈과 가중치

    public StageData bossStageData; // 임계(보스) 모듈
    private StageData _lastSelectedStage; // 직전에 선택한 스테이지 저장

    private StageData[] currentNodes = new StageData[3];
    private int _selectedIndex = 1;

    [Header("--- UI References ---")] public GameObject mapVisualRoot;
    public Image fadeOverlayImage;
    public RectTransform stageTextRect;
    public TextMeshProUGUI stageTitleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI stagePerText;
    public TextMeshProUGUI totalProgressText;
    private CanvasGroup _stageTextCanvasGroup;

    [Header("--- Visual Elements (3 Each) ---")]
    public Image[] nodeVisuals;

    public Image[] lineVisuals;
    private CanvasGroup[] _nodeCanvasGroups;

    [Header("--- Glow Filters ---")] public GlowFilter titleTextGlow;
    public GlowFilter perTextGlow;
    public GlowFilter[] nodeGlows;
    public GlowFilter[] lineGlows;

    [Header("--- Animation Settings ---")] [SerializeField]
    private float lerpSpeed = 18f;

    [SerializeField] private float floatAmount = 9f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float nodeFadeSpeed = 3f;
    [SerializeField] private float scanInterval = 0.15f;

    [Header("Sound")] [SerializeField] private AudioClip sfxSelect;
    [SerializeField] private AudioClip sfxScan;
    [SerializeField] private AudioClip sfxCount;

    private readonly int[] _currentRandomPers = new int[3];

    private PlayerInput _inputActions;
    private readonly Color _activeColor = Color.white;
    private readonly Color _inactiveColor = new Color(70 / 255f, 70 / 255f, 70 / 255f);

    private Vector2[] _nodeOriginPos;
    private Vector2[] _lineOriginPos;
    private Coroutine _fadeCoroutine;
    private bool _isScanning = false;

    private void Awake()
    {
        _inputActions = new PlayerInput();
        _nodeOriginPos = new Vector2[nodeVisuals.Length];
        _lineOriginPos = new Vector2[lineVisuals.Length];
        _nodeCanvasGroups = new CanvasGroup[nodeVisuals.Length];

        if (stageTextRect != null)
        {
            _stageTextCanvasGroup = stageTextRect.GetComponent<CanvasGroup>();
            if (_stageTextCanvasGroup == null)
                _stageTextCanvasGroup = stageTextRect.gameObject.AddComponent<CanvasGroup>();
        }

        for (int i = 0; i < nodeVisuals.Length; i++)
        {
            if (nodeVisuals[i] != null)
            {
                _nodeOriginPos[i] = nodeVisuals[i].rectTransform.anchoredPosition;
                _nodeCanvasGroups[i] = nodeVisuals[i].GetComponent<CanvasGroup>();
                if (_nodeCanvasGroups[i] == null)
                    _nodeCanvasGroups[i] = nodeVisuals[i].gameObject.AddComponent<CanvasGroup>();
            }

            if (i < lineVisuals.Length && lineVisuals[i] != null)
            {
                _lineOriginPos[i] = lineVisuals[i].rectTransform.anchoredPosition;
            }
        }
    }

    private void OnEnable()
    {
        _inputActions.Enable();
        _inputActions.Player.Move.performed += ctx => OnNavigate(ctx.ReadValue<Vector2>());
        _inputActions.Player.Dash.performed += _ => OnEnterStage();
    }

    private void OnDisable() => _inputActions.Disable();

    private void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame) ToggleMap();
        if (!mapVisualRoot || !mapVisualRoot.activeSelf) return;
        HandleSmoothVisuals();
    }

    // ReSharper disable Unity.PerformanceAnalysis
    public void ToggleMap()
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        bool isOpening = !mapVisualRoot.activeSelf;
        _fadeCoroutine = StartCoroutine(mapFadeRoutine(isOpening));
        return;

        IEnumerator mapFadeRoutine(bool isOpen)
        {
            float halfFade = fadeDuration * 0.5f;
            yield return StartCoroutine(FadeOverlay(0f, 1f, halfFade));

            if (isOpen)
            {
                ResetVisualsState();
                mapVisualRoot.SetActive(true);
                StartCoroutine(ScanSequence());
                yield return new WaitForSeconds(0.05f);
                yield return StartCoroutine(FadeOverlay(1f, 0f, halfFade));
            }
            else
            {
                _isScanning = false;
                mapVisualRoot.SetActive(false);
                yield return StartCoroutine(FadeOverlay(1f, 0f, halfFade));
            }

            _fadeCoroutine = null;
        }
    }

    private void ResetVisualsState()
    {
        _isScanning = true;

        // 1. 진행도 및 계절 이름 설정
        bool isBossStage = GameManager.instance.currentProgress >= 100;
        string seasonName = GameManager.instance.currentSeason switch
        {
            Season.Spring => "봄", Season.Summer => "여름",
            Season.Autumn => "가을", Season.Winter => "겨울", _ => ""
        };

        if (totalProgressText)
            totalProgressText.text =
                $"진행도 : <color=#80FF80>{seasonName}_{GameManager.instance.currentProgress}%</color>";

        // 2. 스테이지 노드 결정 (보스 vs 일반)
        if (isBossStage) SetBossNode();
        else SetRandomNodes();

        // 3. Glow 필터 강제 초기화 (이전 잔상 제거)
        if (titleTextGlow) titleTextGlow.enabled = false;
        if (perTextGlow) perTextGlow.enabled = false;

        for (int i = 0; i < 3; i++)
        {
            // Glow 초기화
            if (i < nodeGlows.Length && nodeGlows[i]) nodeGlows[i].enabled = false;
            if (i < lineGlows.Length && lineGlows[i]) lineGlows[i].enabled = false;

            bool hasNode = currentNodes[i] != null;
            nodeVisuals[i].gameObject.SetActive(hasNode);

            if (i < lineVisuals.Length && lineVisuals[i] != null)
            {
                lineVisuals[i].gameObject.SetActive(hasNode);
                // [핵심] 맵이 열릴 때 라인 위치를 저장된 원본 위치로 강제 초기화
                lineVisuals[i].rectTransform.anchoredPosition = _lineOriginPos[i];
            }

            if (hasNode)
            {
                if (currentNodes[i].moduleIcon) nodeVisuals[i].sprite = currentNodes[i].moduleIcon;

                _nodeCanvasGroups[i].alpha = 0f;
                // 노드도 원본 위치로
                nodeVisuals[i].rectTransform.anchoredPosition = _nodeOriginPos[i];
                nodeVisuals[i].color = _inactiveColor;
                _currentRandomPers[i] = Random.Range(currentNodes[i].minRise, currentNodes[i].maxRise + 1);
            }
        }

        // 5. 텍스트 레이아웃 초기화
        if (_stageTextCanvasGroup) _stageTextCanvasGroup.alpha = 0f;
        // 보스전이면 중앙(0), 아니면 0(사용자 설정에 따름)
        stageTextRect.anchoredPosition = isBossStage ? Vector2.zero : new Vector2(0f, 0f);

        if (stageTitleText) stageTitleText.text = "";
        if (descriptionText) descriptionText.text = "";
        if (stagePerText) stagePerText.text = "";
    }

    private void SetBossNode()
    {
        currentNodes[0] = null;
        currentNodes[1] = bossStageData;
        currentNodes[2] = null;
        _selectedIndex = 1;
    }

    private void SetRandomNodes()
    {
        _selectedIndex = 1;
        List<StageData> selectedStages = new List<StageData>();

        // 직전 스테이지를 제외한 임시 풀 생성
        List<StageProbability> tempPool = stagePool
            .Where(p => p.stageData != _lastSelectedStage)
            .ToList();

        for (int i = 0; i < 3; i++)
        {
            if (tempPool.Count == 0) break;

            // 가중치 기반 랜덤 선택
            float totalWeight = tempPool.Sum(p => p.weight);
            float randomVal = Random.Range(0, totalWeight);
            float currentWeightSum = 0;

            for (int j = 0; j < tempPool.Count; j++)
            {
                currentWeightSum += tempPool[j].weight;
                if (randomVal <= currentWeightSum)
                {
                    selectedStages.Add(tempPool[j].stageData);
                    tempPool.RemoveAt(j); // 중복 방지
                    break;
                }
            }
        }

        for (int i = 0; i < 3; i++)
        {
            currentNodes[i] = i < selectedStages.Count ? selectedStages[i] : null;
        }
    }

    private IEnumerator FadeOverlay(float start, float end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (fadeOverlayImage)
                fadeOverlayImage.color = new Color(0, 0, 0, Mathf.Lerp(start, end, elapsed / duration));
            yield return null;
        }

        if (fadeOverlayImage) fadeOverlayImage.color = new Color(0, 0, 0, end);
    }

    private IEnumerator ScanSequence()
    {
        for (int i = 0; i < nodeVisuals.Length; i++)
        {
            StageData data = currentNodes[i];
            // [수정] stageName -> moduleName
            if (stageTitleText) stageTitleText.text = $"모듈 : {data.moduleName}";

            StartCoroutine(FadeInNode(i));
            if (sfxScan) SoundManager.instance.PlaySFX(sfxScan, 0.15f);
            yield return new WaitForSeconds(scanInterval);
        }

        _isScanning = false;
        UpdateUI();
    }

    private IEnumerator FadeInNode(int index)
    {
        float alpha = 0f;
        while (alpha < 1f)
        {
            alpha += Time.deltaTime * nodeFadeSpeed;
            if (_nodeCanvasGroups[index]) _nodeCanvasGroups[index].alpha = alpha;
            if (lineVisuals[index])
                lineVisuals[index].color = new Color(_inactiveColor.r, _inactiveColor.g, _inactiveColor.b, alpha);
            yield return null;
        }

        if (_nodeCanvasGroups[index]) _nodeCanvasGroups[index].alpha = 1f;
    }

    private void HandleSmoothVisuals()
    {
        // [수정] 스캔 중에는 텍스트와 알파 연출만 스킵하고, 위치 계산은 계속 돌아가야 라인이 고정됨
        if (!_isScanning)
        {
            if (_stageTextCanvasGroup)
                _stageTextCanvasGroup.alpha = Mathf.Lerp(_stageTextCanvasGroup.alpha, 1f, Time.deltaTime * lerpSpeed);

            float targetX = (_selectedIndex - 1) * 160f;
            stageTextRect.anchoredPosition = Vector2.Lerp(stageTextRect.anchoredPosition, new Vector2(targetX, 0),
                Time.deltaTime * lerpSpeed);
        }

        for (int i = 0; i < 3; i++)
        {
            bool isSelected = (!_isScanning && i == _selectedIndex); // 스캔 중엔 선택 효과 끔
            Color targetColor = isSelected ? _activeColor : _inactiveColor;

            // 색상 보간
            if (nodeVisuals[i])
                nodeVisuals[i].color = Color.Lerp(nodeVisuals[i].color, targetColor, Time.deltaTime * lerpSpeed);

            // 라인 색상 (스캔 중엔 FadeInNode에서 알파를 제어하므로 Lerp는 스캔 후에만)
            if (lineVisuals[i] && !_isScanning)
                lineVisuals[i].color = Color.Lerp(lineVisuals[i].color, targetColor, Time.deltaTime * lerpSpeed);

            // 노드 위치 결정
            Vector2 targetNodePos = isSelected ? _nodeOriginPos[i] + Vector2.up * floatAmount : _nodeOriginPos[i];
            nodeVisuals[i].rectTransform.anchoredPosition = Vector2.Lerp(nodeVisuals[i].rectTransform.anchoredPosition,
                targetNodePos, Time.deltaTime * lerpSpeed);

            // [중요] 라인 위치 보정 로직 - 스캔 중이든 아니든 항상 실행되어야 자식인 라인이 고정됨
            if (lineVisuals[i])
            {
                float currentYOffset = nodeVisuals[i].rectTransform.anchoredPosition.y - _nodeOriginPos[i].y;
                float parentScaleY = nodeVisuals[i].rectTransform.localScale.y;
                if (parentScaleY == 0) parentScaleY = 1f;
                float compensatedOffset = currentYOffset / parentScaleY;

                // 노드가 움직이는 만큼 정확히 반대로 찍어 누름
                lineVisuals[i].rectTransform.anchoredPosition = _lineOriginPos[i] - new Vector2(0, compensatedOffset);
            }
        }
    }

    private void OnNavigate(Vector2 direction)
    {
        if (mapVisualRoot == null || !mapVisualRoot.activeSelf || _isScanning) return;
        if (direction.x < -0.5f) ChangeSelection(-1);
        else if (direction.x > 0.5f) ChangeSelection(1);
    }

    private void ChangeSelection(int dir)
    {
        int prevIndex = _selectedIndex;
        _selectedIndex = Mathf.Clamp(_selectedIndex + dir, 0, currentNodes.Length - 1);

        if (prevIndex != _selectedIndex)
        {
            UpdateUI();
            if (sfxSelect != null) SoundManager.instance.PlaySFX(sfxSelect, 0.1f);
        }
    }

    private void UpdateUI()
    {
        // 현재 선택된 인덱스에 데이터가 없으면 실행 안 함 (방어 코드)
        if (currentNodes == null || currentNodes.Length <= _selectedIndex || currentNodes[_selectedIndex] == null)
        {
            if (titleTextGlow) titleTextGlow.enabled = false;
            if (perTextGlow) perTextGlow.enabled = false;
            return;
        }

        StageData data = currentNodes[_selectedIndex];

        // 1. 텍스트 내용 갱신
        if (stageTitleText) stageTitleText.text = $"모듈 : {data.moduleName}";
        if (stagePerText) stagePerText.text = $"+ {_currentRandomPers[_selectedIndex]}%";
        if (descriptionText) descriptionText.text = data.description;

        // 2. 공통 Glow 설정 (타이틀 & 진행도 숫자)
        if (titleTextGlow)
        {
            titleTextGlow.enabled = true;
            titleTextGlow.Color = data.themeColor;
        }

        if (perTextGlow)
        {
            perTextGlow.enabled = true;
            perTextGlow.Color = data.themeColor;
        }

        // 3. 노드 및 라인별 개별 Glow 설정 (스캔이 끝난 후에만 작동)
        if (!_isScanning)
        {
            for (int i = 0; i < 3; i++)
            {
                bool isSelected = (i == _selectedIndex);
                bool hasNode = (currentNodes[i] != null);

                // 선택된 노드에만 테마 색상 Glow 켜기
                if (i < nodeGlows.Length && nodeGlows[i])
                {
                    nodeGlows[i].enabled = isSelected && hasNode;
                    if (nodeGlows[i].enabled) nodeGlows[i].Color = data.themeColor;
                }

                // 라인 Glow 처리
                if (i < lineGlows.Length && lineGlows[i])
                {
                    lineGlows[i].enabled = isSelected && hasNode;
                    if (lineGlows[i].enabled) lineGlows[i].Color = data.themeColor;
                }
            }
        }
    }

    private void OnEnterStage()
    {
        if (mapVisualRoot == null || !mapVisualRoot.activeSelf || _fadeCoroutine != null || _isScanning) return;
        if (currentNodes.Length == 0) return;
        StartCoroutine(ProcessSelectSequence());
    }

    private IEnumerator ProcessSelectSequence()
    {
        // 1. 선택된 데이터 확보 및 마지막 선택 기록
        StageData selectedData = currentNodes[_selectedIndex];
        _lastSelectedStage = selectedData; // 다음 맵 생성 시 중복 방지를 위해 저장

        int gainPer = _currentRandomPers[_selectedIndex];
        int startTotalPer = GameManager.instance.currentProgress;
        int targetTotalPer = Mathf.Clamp(startTotalPer + gainPer, 0, 100);

        // 현재 계절 이름 가져오기
        string seasonName = GameManager.instance.currentSeason switch
        {
            Season.Spring => "봄",
            Season.Summer => "여름",
            Season.Autumn => "가을",
            Season.Winter => "겨울",
            _ => ""
        };

        if (sfxSelect) SoundManager.instance.PlaySFX(sfxSelect, 0.1f);

        // 2. 진행도 카운트업 연출 (숫자가 올라가는 시각 효과)
        float duration = 0.5f;
        float elapsed = 0f;
        float soundTimer = 0f;
        float soundInterval = 0.07f;

        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            soundTimer += dt;

            if (soundTimer >= soundInterval)
            {
                soundTimer = 0f;
                if (sfxCount) SoundManager.instance.PlaySFX(sfxCount, 0.9f);
            }

            float t = elapsed / duration;

            // 노드 위쪽의 "+ N%" 텍스트가 줄어드는 연출
            int currentNodeVal = (int)Mathf.Lerp(gainPer, 0, t);
            if (stagePerText) stagePerText.text = $"+ {currentNodeVal}%";

            // 전체 진행도 텍스트가 올라가는 연출
            int currentTotalVal = (int)Mathf.Lerp(startTotalPer, targetTotalPer, t);
            if (totalProgressText)
                totalProgressText.text = $"진행도 : <color=#80FF80>{seasonName}_{currentTotalVal}%</color>";

            yield return null;
        }

        // 최종 값 확정
        if (stagePerText) stagePerText.text = "";
        if (totalProgressText)
            totalProgressText.text = $"진행도 : <color=#80FF80>{seasonName}_{targetTotalPer}%</color>";

        // 실제 데이터 갱신
        GameManager.instance.currentProgress = targetTotalPer;

        yield return new WaitForSeconds(0.2f);

        // 3. 페이드 아웃 및 스테이지 로드
        yield return StartCoroutine(FadeOverlay(0f, 1f, 0.5f));

        mapVisualRoot.SetActive(false);
        _isScanning = false;

        // GameManager에게 실제 맵 생성을 요청
        if (GameManager.instance)
        {
            GameManager.instance.LoadStage(selectedData);
        }

        yield return new WaitForSeconds(0.1f);

        // 4. 다시 화면 밝아짐
        yield return StartCoroutine(FadeOverlay(1f, 0f, 0.5f));
    }
}