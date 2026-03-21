using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public List<StageProbability> stagePool; 

    public StageData bossStageData; 
    private StageData _lastSelectedStage; 

    private StageData[] currentNodes = new StageData[3];
    private int _selectedIndex = 1;
    private bool _isBossStageMode = false;

    [Header("--- UI References ---")] 
    public GameObject mapVisualRoot;
    public Image fadeOverlayImage;
    public RectTransform stageTextRect;
    public TextMeshProUGUI stageTitleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI stagePerText;
    public TextMeshProUGUI totalProgressText;
    private CanvasGroup _stageTextCanvasGroup;

    [Header("--- Text Settings (Inspector) ---")]
    [Tooltip("일반 모드일 때 텍스트(StageText)의 위치 (0:왼쪽, 1:중앙, 2:오른쪽)")]
    public Vector2[] normalTextPositions = new Vector2[3] { new Vector2(-160, 0), new Vector2(0, 0), new Vector2(160, 0) };
    [Tooltip("보스 모드일 때 텍스트(StageText)의 위치")]
    public Vector2 bossTextPosition = Vector2.zero;
    // [추가] 텍스트 스케일 설정
    [Tooltip("일반 모드 텍스트 스케일")]
    public float normalTextScale = 1.0f;
    [Tooltip("보스 모드 텍스트 스케일")]
    public float bossTextScale = 1.2f;

    [Header("--- Visual Elements (3 Each) ---")]
    public Image[] nodeVisuals; 
    public Image[] lineVisuals; 
    private CanvasGroup[] _nodeCanvasGroups;

    [Header("--- Boss Visual Elements ---")]
    public Image bossNodeVisual;
    public GlowFilter bossNodeGlow;
    public Image bossLineVisual;
    public GlowFilter bossLineGlow;

    private CanvasGroup _bossNodeCanvasGroup;
    private Vector2 _bossNodeOriginPos;
    private Vector2 _bossLineOriginPos; 

    [Header("--- Glow Filters ---")] 
    public GlowFilter titleTextGlow;
    public GlowFilter perTextGlow;
    public GlowFilter[] nodeGlows;
    public GlowFilter[] lineGlows;

    [Header("--- Animation Settings ---")] 
    [SerializeField] private float lerpSpeed = 18f;
    [SerializeField] private float floatAmount = 9f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float nodeFadeSpeed = 3f;
    [SerializeField] private float scanInterval = 0.15f;

    [Header("Sound")] 
    [SerializeField] private AudioClip sfxSelect;
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

        if (bossNodeVisual != null)
        {
            _bossNodeOriginPos = bossNodeVisual.rectTransform.anchoredPosition;
            _bossNodeCanvasGroup = bossNodeVisual.GetComponent<CanvasGroup>();
            if (_bossNodeCanvasGroup == null)
                _bossNodeCanvasGroup = bossNodeVisual.gameObject.AddComponent<CanvasGroup>();
        }

        if (bossLineVisual != null)
        {
            _bossLineOriginPos = bossLineVisual.rectTransform.anchoredPosition;
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

        _isBossStageMode = GameManager.instance.currentProgress >= 100;
        
        string seasonName = GameManager.instance.currentSeason switch
        {
            Season.Spring => "봄", Season.Summer => "여름",
            Season.Autumn => "가을", Season.Winter => "겨울", _ => ""
        };

        if (totalProgressText)
            totalProgressText.text = $"진행도 : <color=#80FF80>{seasonName}_{GameManager.instance.currentProgress}%</color>";

        if (_isBossStageMode) SetBossNode();
        else SetRandomNodes();

        if (titleTextGlow) titleTextGlow.enabled = false;
        if (perTextGlow) perTextGlow.enabled = false;
        if (bossNodeGlow) bossNodeGlow.enabled = false;
        if (bossLineGlow) bossLineGlow.enabled = false; 

        // 텍스트 위치 및 스케일 초기화
        if (_stageTextCanvasGroup) _stageTextCanvasGroup.alpha = 0f;
        
        stageTextRect.anchoredPosition = _isBossStageMode ? bossTextPosition : normalTextPositions[1];
        
        // [추가] 맵을 열 때 스케일도 즉시 설정 (부드러운 전환 전 초기값)
        float initialScale = _isBossStageMode ? bossTextScale : normalTextScale;
        stageTextRect.localScale = new Vector3(initialScale, initialScale, 1f);

        if (stageTitleText) stageTitleText.text = "";
        if (descriptionText) descriptionText.text = "";
        if (stagePerText) stagePerText.text = "";

        if (_isBossStageMode)
        {
            for (int i = 0; i < 3; i++)
            {
                if (nodeVisuals[i]) nodeVisuals[i].gameObject.SetActive(false);
                if (lineVisuals[i]) lineVisuals[i].gameObject.SetActive(false);
                if (nodeGlows[i]) nodeGlows[i].enabled = false;
                if (lineGlows[i]) lineGlows[i].enabled = false;
            }

            if (bossNodeVisual)
            {
                bossNodeVisual.gameObject.SetActive(true);
                if (bossStageData.moduleIcon) bossNodeVisual.sprite = bossStageData.moduleIcon;
                
                if (_bossNodeCanvasGroup) _bossNodeCanvasGroup.alpha = 0f;
                bossNodeVisual.rectTransform.anchoredPosition = _bossNodeOriginPos;
                bossNodeVisual.color = _inactiveColor;
            }

            if (bossLineVisual)
            {
                bossLineVisual.gameObject.SetActive(true);
                bossLineVisual.rectTransform.anchoredPosition = _bossLineOriginPos;
            }
        }
        else
        {
            if (bossNodeVisual) bossNodeVisual.gameObject.SetActive(false);
            if (bossLineVisual) bossLineVisual.gameObject.SetActive(false);

            for (int i = 0; i < 3; i++)
            {
                if (nodeGlows[i]) nodeGlows[i].enabled = false;
                if (lineGlows[i]) lineGlows[i].enabled = false;

                bool hasNode = currentNodes[i] != null;
                if (nodeVisuals[i]) nodeVisuals[i].gameObject.SetActive(hasNode);

                if (i < lineVisuals.Length && lineVisuals[i] != null)
                {
                    lineVisuals[i].gameObject.SetActive(hasNode);
                    lineVisuals[i].rectTransform.anchoredPosition = _lineOriginPos[i];
                }

                if (hasNode)
                {
                    if (currentNodes[i].moduleIcon) nodeVisuals[i].sprite = currentNodes[i].moduleIcon;
                    if (_nodeCanvasGroups[i]) _nodeCanvasGroups[i].alpha = 0f;
                    
                    nodeVisuals[i].rectTransform.anchoredPosition = _nodeOriginPos[i];
                    nodeVisuals[i].color = _inactiveColor;
                    _currentRandomPers[i] = Random.Range(currentNodes[i].minRise, currentNodes[i].maxRise + 1);
                }
            }
        }
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

        List<StageProbability> tempPool = stagePool
            .Where(p => p.stageData != _lastSelectedStage)
            .ToList();

        for (int i = 0; i < 3; i++)
        {
            if (tempPool.Count == 0) break;

            float totalWeight = tempPool.Sum(p => p.weight);
            float randomVal = Random.Range(0, totalWeight);
            float currentWeightSum = 0;

            for (int j = 0; j < tempPool.Count; j++)
            {
                currentWeightSum += tempPool[j].weight;
                if (randomVal <= currentWeightSum)
                {
                    selectedStages.Add(tempPool[j].stageData);
                    tempPool.RemoveAt(j);
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
        if (_isBossStageMode)
        {
            if (stageTitleText) stageTitleText.text = $"모듈 : {bossStageData.moduleName}";

            float alpha = 0f;
            while (alpha < 1f)
            {
                alpha += Time.deltaTime * nodeFadeSpeed;
                if (_bossNodeCanvasGroup) _bossNodeCanvasGroup.alpha = alpha;
                
                if (bossLineVisual) 
                    bossLineVisual.color = new Color(_inactiveColor.r, _inactiveColor.g, _inactiveColor.b, alpha);
                    
                yield return null;
            }
            if (_bossNodeCanvasGroup) _bossNodeCanvasGroup.alpha = 1f;

            if (sfxScan) SoundManager.instance.PlaySFX(sfxScan, 0.15f);
            yield return new WaitForSeconds(scanInterval);
        }
        else
        {
            for (int i = 0; i < nodeVisuals.Length; i++)
            {
                StageData data = currentNodes[i];
                if (data == null) continue;

                if (stageTitleText) stageTitleText.text = $"모듈 : {data.moduleName}";
                StartCoroutine(FadeInNode(i));
                
                if (sfxScan) SoundManager.instance.PlaySFX(sfxScan, 0.15f);
                yield return new WaitForSeconds(scanInterval);
            }
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
        if (!_isScanning)
        {
            if (_stageTextCanvasGroup)
                _stageTextCanvasGroup.alpha = Mathf.Lerp(_stageTextCanvasGroup.alpha, 1f, Time.deltaTime * lerpSpeed);

            Vector2 targetTextPos = _isBossStageMode ? bossTextPosition : normalTextPositions[_selectedIndex];
            stageTextRect.anchoredPosition = Vector2.Lerp(stageTextRect.anchoredPosition, targetTextPos, Time.deltaTime * lerpSpeed);
            
            // [추가] 텍스트 부모 오브젝트의 스케일 보간 (부드럽게 커지거나 작아짐)
            float targetScale = _isBossStageMode ? bossTextScale : normalTextScale;
            Vector3 targetScaleVector = new Vector3(targetScale, targetScale, 1f);
            stageTextRect.localScale = Vector3.Lerp(stageTextRect.localScale, targetScaleVector, Time.deltaTime * lerpSpeed);
        }

        if (_isBossStageMode)
        {
            if (bossNodeVisual)
            {
                Color targetColor = _isScanning ? _inactiveColor : _activeColor;
                bossNodeVisual.color = Color.Lerp(bossNodeVisual.color, targetColor, Time.deltaTime * lerpSpeed);

                if (bossLineVisual && !_isScanning)
                    bossLineVisual.color = Color.Lerp(bossLineVisual.color, targetColor, Time.deltaTime * lerpSpeed);

                Vector2 targetNodePos = (!_isScanning) ? _bossNodeOriginPos + Vector2.up * floatAmount : _bossNodeOriginPos;
                bossNodeVisual.rectTransform.anchoredPosition = Vector2.Lerp(bossNodeVisual.rectTransform.anchoredPosition, targetNodePos, Time.deltaTime * lerpSpeed);

                if (bossLineVisual)
                {
                    float currentYOffset = bossNodeVisual.rectTransform.anchoredPosition.y - _bossNodeOriginPos.y;
                    float parentScaleY = bossNodeVisual.rectTransform.localScale.y;
                    if (parentScaleY == 0) parentScaleY = 1f;
                    float compensatedOffset = currentYOffset / parentScaleY;

                    bossLineVisual.rectTransform.anchoredPosition = _bossLineOriginPos - new Vector2(0, compensatedOffset);
                }
            }
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                if (currentNodes[i] == null) continue;

                bool isSelected = (!_isScanning && i == _selectedIndex); 
                Color targetColor = isSelected ? _activeColor : _inactiveColor;

                if (nodeVisuals[i])
                    nodeVisuals[i].color = Color.Lerp(nodeVisuals[i].color, targetColor, Time.deltaTime * lerpSpeed);

                if (lineVisuals[i] && !_isScanning)
                    lineVisuals[i].color = Color.Lerp(lineVisuals[i].color, targetColor, Time.deltaTime * lerpSpeed);

                Vector2 targetNodePos = isSelected ? _nodeOriginPos[i] + Vector2.up * floatAmount : _nodeOriginPos[i];
                nodeVisuals[i].rectTransform.anchoredPosition = Vector2.Lerp(nodeVisuals[i].rectTransform.anchoredPosition, targetNodePos, Time.deltaTime * lerpSpeed);

                if (lineVisuals[i])
                {
                    float currentYOffset = nodeVisuals[i].rectTransform.anchoredPosition.y - _nodeOriginPos[i].y;
                    float parentScaleY = nodeVisuals[i].rectTransform.localScale.y;
                    if (parentScaleY == 0) parentScaleY = 1f;
                    float compensatedOffset = currentYOffset / parentScaleY;

                    lineVisuals[i].rectTransform.anchoredPosition = _lineOriginPos[i] - new Vector2(0, compensatedOffset);
                }
            }
        }
    }

    private void OnNavigate(Vector2 direction)
    {
        if (mapVisualRoot == null || !mapVisualRoot.activeSelf || _isScanning || _isBossStageMode) return;
        
        if (direction.x < -0.5f) ChangeSelection(-1);
        else if (direction.x > 0.5f) ChangeSelection(1);
    }

    private void ChangeSelection(int dir)
    {
        int prevIndex = _selectedIndex;
        _selectedIndex = Mathf.Clamp(_selectedIndex + dir, 0, currentNodes.Length - 1);

        if (currentNodes[_selectedIndex] == null)
        {
            _selectedIndex = prevIndex;
        }

        if (prevIndex != _selectedIndex)
        {
            UpdateUI();
            if (sfxSelect != null) SoundManager.instance.PlaySFX(sfxSelect, 0.1f);
        }
    }

    private void UpdateUI()
    {
        if (currentNodes == null || currentNodes.Length <= _selectedIndex || currentNodes[_selectedIndex] == null)
        {
            if (titleTextGlow) titleTextGlow.enabled = false;
            if (perTextGlow) perTextGlow.enabled = false;
            return;
        }

        StageData data = currentNodes[_selectedIndex];

        if (stageTitleText) stageTitleText.text = $"모듈 : {data.moduleName}";
        if (stagePerText) stagePerText.text = _isBossStageMode ? "" : $"+ {_currentRandomPers[_selectedIndex]}%";
        if (descriptionText) descriptionText.text = data.description;

        if (titleTextGlow)
        {
            titleTextGlow.enabled = true;
            titleTextGlow.Color = data.themeColor;
        }

        if (perTextGlow)
        {
            perTextGlow.enabled = !_isBossStageMode;
            if (perTextGlow.enabled) perTextGlow.Color = data.themeColor;
        }

        if (!_isScanning)
        {
            if (_isBossStageMode)
            {
                if (bossNodeGlow)
                {
                    bossNodeGlow.enabled = true;
                    bossNodeGlow.Color = data.themeColor;
                }
                
                if (bossLineGlow)
                {
                    bossLineGlow.enabled = true;
                    bossLineGlow.Color = data.themeColor;
                }
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    if (currentNodes[i] == null) continue;

                    bool isSelected = (i == _selectedIndex);

                    if (i < nodeGlows.Length && nodeGlows[i])
                    {
                        nodeGlows[i].enabled = isSelected;
                        if (nodeGlows[i].enabled) nodeGlows[i].Color = data.themeColor;
                    }

                    if (i < lineGlows.Length && lineGlows[i])
                    {
                        lineGlows[i].enabled = isSelected;
                        if (lineGlows[i].enabled) lineGlows[i].Color = data.themeColor;
                    }
                }
            }
        }
    }

    private void OnEnterStage()
    {
        if (mapVisualRoot == null || !mapVisualRoot.activeSelf || _fadeCoroutine != null || _isScanning) return;
        if (currentNodes.Length == 0 || currentNodes[_selectedIndex] == null) return;
        
        StartCoroutine(ProcessSelectSequence());
    }

    private IEnumerator ProcessSelectSequence()
    {
        StageData selectedData = currentNodes[_selectedIndex];
        _lastSelectedStage = selectedData;

        int gainPer = _currentRandomPers[_selectedIndex];
        int startTotalPer = GameManager.instance.currentProgress;
        int targetTotalPer = Mathf.Clamp(startTotalPer + gainPer, 0, 100);

        string seasonName = GameManager.instance.currentSeason switch
        {
            Season.Spring => "봄", Season.Summer => "여름",
            Season.Autumn => "가을", Season.Winter => "겨울", _ => ""
        };

        if (sfxSelect) SoundManager.instance.PlaySFX(sfxSelect, 0.1f);

        if (!_isBossStageMode)
        {
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

                int currentNodeVal = (int)Mathf.Lerp(gainPer, 0, t);
                if (stagePerText) stagePerText.text = $"+ {currentNodeVal}%";

                int currentTotalVal = (int)Mathf.Lerp(startTotalPer, targetTotalPer, t);
                if (totalProgressText)
                    totalProgressText.text = $"진행도 : <color=#80FF80>{seasonName}_{currentTotalVal}%</color>";

                yield return null;
            }
        }

        if (stagePerText) stagePerText.text = "";
        if (totalProgressText)
            totalProgressText.text = $"진행도 : <color=#80FF80>{seasonName}_{targetTotalPer}%</color>";

        GameManager.instance.currentProgress = targetTotalPer;

        yield return new WaitForSeconds(0.2f);

        yield return StartCoroutine(FadeOverlay(0f, 1f, 0.5f));

        mapVisualRoot.SetActive(false);
        _isScanning = false;

        if (GameManager.instance)
        {
            GameManager.instance.LoadStage(selectedData);
        }

        yield return new WaitForSeconds(0.1f);

        yield return StartCoroutine(FadeOverlay(1f, 0f, 0.5f));
    }
}