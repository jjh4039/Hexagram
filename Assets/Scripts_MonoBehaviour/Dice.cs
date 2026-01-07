using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChocDino.UIFX;

public class Dice : MonoBehaviour
{
    [Header("--- Data Settings ---")]
    [SerializeField] public DiceData[] diceList; // ★ 인스펙터에서 주사위 데이터 6개 꼭 연결하세요!
    [SerializeField] public DiceData defaultData; // 대기 상태용 데이터 (회색)

    [Header("--- Settings ---")]
    [SerializeField] private float buffDuration = 10f;
    [SerializeField] private float rollCooldown = 3f;

    [Header("--- Visual Settings ---")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float countdownStartSeconds = 3.0f;
    [SerializeField] private float preResultDelay = 0.2f;

    [Header("--- UI Elements ---")]
    [SerializeField] private CanvasGroup uiCanvasGroup;
    [SerializeField] private Image moduleIcon;
    [SerializeField] private Image moduleCooldownOverlay;
    [SerializeField] private TextMeshProUGUI desText;

    [Header("--- Head Object ---")]
    [SerializeField] private SpriteRenderer headDiceRenderer;
    [SerializeField] private Sprite[] countdownSprites;
    [SerializeField] private Sprite[] greyDiceSprites;
    [SerializeField] private float rollAnimSpeed = 0.08f;

    [Header("--- Head Scale & Effect ---")]
    [SerializeField] private float rollingScale = 1.0f;
    [SerializeField] private float resultDiceScale = 0.05f;
    [SerializeField] private float ghostDuration = 0.4f;
    [SerializeField] private float ghostScaleMultiplier = 2.0f;
    [SerializeField] private float resultVisibleTime = 1.5f;
    [SerializeField] private float resultFadeDuration = 0.5f;

    [Header("--- FX ---")]
    [SerializeField] private GlowFilter diceGlowFilter;
    [SerializeField] private GlowFilter textGlowFilter;
    [SerializeField] private ParticleSystem resultParticle;

    private float currentTimer;
    private bool isBuffActive = false;
    private Color currentTargetColor;
    private float timeSinceStateStart;

    private void Start() => InitializeState();
    private void Update()
    {
        HandleTimer();
        HandleVisualEffects();
    }

    private void InitializeState()
    {
        isBuffActive = false;
        currentTimer = rollCooldown;
        UpdateUI_Ready();
    }

    private void HandleTimer()
    {
        currentTimer -= Time.deltaTime;
        timeSinceStateStart += Time.deltaTime;
        float maxTime = isBuffActive ? buffDuration : rollCooldown;
        if (moduleCooldownOverlay != null) moduleCooldownOverlay.fillAmount = 1f - (currentTimer / maxTime);

        if (currentTimer <= 0)
        {
            if (isBuffActive) EndBuff();
            else if (timeSinceStateStart > 0.1f) StartCoroutine(RollDiceSequence());
        }
    }

    private IEnumerator RollDiceSequence()
    {
        // 충전 상태 해제
        if (GameManager.instance?.player != null) GameManager.instance.player.SetChargingState(false);

        yield return new WaitForSeconds(preResultDelay);

        // 랜덤 주사위 굴리기
        int diceValue = Random.Range(0, diceList.Length);

        isBuffActive = true;
        currentTimer = buffDuration;
        timeSinceStateStart = 0f;

        // 결과 적용
        UpdateUI_BuffActive(diceValue);
    }

    private void EndBuff()
    {
        isBuffActive = false;
        currentTimer = rollCooldown;
        timeSinceStateStart = 0f;

        // ★ [중요] 버프 끝났다고 플레이어에게 알림
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            GameManager.instance.player.RemoveDiceBuff();
            Debug.Log("주사위 버프 종료");
        }

        UpdateUI_Ready();
    }

    private void UpdateUI_BuffActive(int diceVal)
    {
        // 데이터 가져오기 (범위 체크)
        if (diceVal < 0 || diceVal >= diceList.Length) return;
        DiceData data = diceList[diceVal];

        // ★ [핵심] 플레이어에게 데이터 전달해서 버프 발동!
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            GameManager.instance.player.ApplyDiceBuff(data);
        }

        if (resultParticle != null)
        {
            // 1. 색상 적용 (기존 코드)
            var main = resultParticle.main;

            // 2. [추가] 텍스처 시트 애니메이션 모듈 켜기
            var texSheet = resultParticle.textureSheetAnimation;
            texSheet.enabled = true;
            texSheet.mode = ParticleSystemAnimationMode.Sprites; // 스프라이트 모드로 변경

            // 3. 스프라이트 교체 (주사위 데이터의 아이콘 사용)
            // (SetSprite 함수는 인덱스와 스프라이트를 받음. 0번에 넣으면 됨)
            texSheet.SetSprite(0, data.icon);

            resultParticle.Stop();
            resultParticle.Play();
        }

        // UI 및 효과 적용
        currentTargetColor = data.uiGlowColor;
        moduleIcon.sprite = data.icon;
        desText.text = data.description;

        // Glow
        if (diceGlowFilter != null) { diceGlowFilter.enabled = true; diceGlowFilter.Color = currentTargetColor; }
        if (textGlowFilter != null) { textGlowFilter.enabled = true; textGlowFilter.Color = currentTargetColor; }
        if (uiCanvasGroup != null) uiCanvasGroup.alpha = 0f;

        // 머리 위 주사위 연출
        if (headDiceRenderer != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowResultRoutine(data));
        }
    }

    private void UpdateUI_Ready()
    {
        // Default Data 사용
        if (defaultData != null)
        {
            currentTargetColor = defaultData.uiGlowColor;
            moduleIcon.sprite = defaultData.icon;
        }
        else
        {
            currentTargetColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        desText.text = "주사위 충전 중...";

        if (diceGlowFilter != null) { diceGlowFilter.enabled = true; diceGlowFilter.Color = currentTargetColor; }
        if (textGlowFilter != null) { textGlowFilter.enabled = true; textGlowFilter.Color = currentTargetColor; }
        if (uiCanvasGroup != null) uiCanvasGroup.alpha = 0f;

        if (headDiceRenderer != null)
        {
            StopAllCoroutines();
            StartCoroutine(RollingRoutine());
        }

        // 충전 상태 시작 알림
        if (GameManager.instance?.player != null) GameManager.instance.player.SetChargingState(true);
    }

    // --- (아래 비주얼 코드는 기존과 동일) ---

    private void HandleVisualEffects()
    {
        float currentAlpha = 1f;
        if (timeSinceStateStart < fadeInDuration) currentAlpha = timeSinceStateStart / fadeInDuration;
        else currentAlpha = 1f;
        if (uiCanvasGroup != null) uiCanvasGroup.alpha = currentAlpha;

        if (isBuffActive && currentTimer <= countdownStartSeconds)
        {
            if (!headDiceRenderer.gameObject.activeSelf) headDiceRenderer.gameObject.SetActive(true);
            headDiceRenderer.color = Color.white;
            headDiceRenderer.transform.localScale = Vector3.one * rollingScale;

            if (countdownSprites != null && countdownSprites.Length >= 3)
            {
                int index = 3 - Mathf.CeilToInt(currentTimer);
                index = Mathf.Clamp(index, 0, countdownSprites.Length - 1);
                headDiceRenderer.sprite = countdownSprites[index];
            }
        }

        Color finalGlowColor = currentTargetColor;
        finalGlowColor.a = currentAlpha;
        if (diceGlowFilter != null) diceGlowFilter.Color = finalGlowColor;
        if (textGlowFilter != null) textGlowFilter.Color = finalGlowColor;
    }

    private IEnumerator RollingRoutine()
    {
        headDiceRenderer.gameObject.SetActive(true);
        headDiceRenderer.color = Color.white;
        headDiceRenderer.transform.rotation = Quaternion.identity;
        headDiceRenderer.transform.localScale = Vector3.one * rollingScale;
        while (true)
        {
            if (greyDiceSprites.Length > 0)
            {
                int randomIndex = Random.Range(0, greyDiceSprites.Length);
                headDiceRenderer.sprite = greyDiceSprites[randomIndex];
            }
            yield return new WaitForSeconds(rollAnimSpeed);
        }
    }

    private IEnumerator ShowResultRoutine(DiceData data)
    {
        headDiceRenderer.gameObject.SetActive(true);
        headDiceRenderer.color = Color.white;
        headDiceRenderer.sprite = data.icon;
        headDiceRenderer.transform.localScale = Vector3.one * resultDiceScale;

        StartCoroutine(PlayGhostEffect(data.icon, headDiceRenderer.transform.position, headDiceRenderer.transform.localScale));

        // 애니메이션 Sync
        int index = System.Array.IndexOf(diceList, data);
        if (GameManager.instance.player != null) GameManager.instance.player.SetDiceAnimation(index);

        yield return new WaitForSeconds(resultVisibleTime);

        float elapsed = 0f;
        while (elapsed < resultFadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / resultFadeDuration);
            Color c = headDiceRenderer.color;
            c.a = alpha;
            headDiceRenderer.color = c;
            yield return null;
        }
        headDiceRenderer.gameObject.SetActive(false);
    }

    private IEnumerator PlayGhostEffect(Sprite sprite, Vector3 position, Vector3 startScale)
    {
        GameObject ghostObj = new GameObject("DiceGhost");
        ghostObj.transform.position = position;
        SpriteRenderer sr = ghostObj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(1, 1, 1, 0.8f);
        sr.sortingLayerID = headDiceRenderer.sortingLayerID;
        sr.sortingOrder = headDiceRenderer.sortingOrder - 1;
        float elapsed = 0f;
        Vector3 targetScale = startScale * ghostScaleMultiplier;
        while (elapsed < ghostDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / ghostDuration;
            ghostObj.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            Color c = sr.color;
            c.a = Mathf.Lerp(0.8f, 0f, t);
            sr.color = c;
            yield return null;
        }
        Destroy(ghostObj);
    }
}