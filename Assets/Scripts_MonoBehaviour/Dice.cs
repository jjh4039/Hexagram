using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChocDino.UIFX;

public class Dice : MonoBehaviour
{
    [Header("--- Data Settings ---")]
    [SerializeField] public DiceData[] diceList;
    [SerializeField] public DiceData defaultData;

    [Header("--- Settings ---")]
    [SerializeField] private float buffDuration = 10f; // 버프 지속 시간
    [SerializeField] private float rollCooldown = 3f;  // 준비(충전) 시간

    [Header("--- Visual Settings ---")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float countdownStartSeconds = 3.0f;
    [SerializeField] private float preAnimTime = 0.5f; // 0.2초 전 애니메이션 발동

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

    // 내부 변수
    private float currentTimer;
    private bool isBuffActive = false;
    private Color currentTargetColor;
    private float timeSinceStateStart;

    // 로직 제어용 변수
    private bool hasPreAnimTriggered = false;
    private int nextDiceIndex = -1;

    private void Start() => InitializeState();
    private void Update()
    {
        HandleTimerAndLogic();
        HandleVisualEffects();
    }

    private void InitializeState()
    {
        EnterCooldownPhase();
    }

    private void HandleTimerAndLogic()
    {
        currentTimer -= Time.deltaTime;
        timeSinceStateStart += Time.deltaTime;

        float maxTime = isBuffActive ? buffDuration : rollCooldown;

        // ★ [수정 1] 원 회전 방향 반대로 변경 (덮이는 연출)
        // 1f - 비율 : 시간이 지날수록 0 -> 1로 증가 (회색이 차오름)
        if (moduleCooldownOverlay != null)
            moduleCooldownOverlay.fillAmount = 1f - (currentTimer / maxTime);

        // -------------------------------------------------------------
        // 상태별 로직 처리
        // -------------------------------------------------------------

        if (isBuffActive)
        {
            // [버프 상태] 시간이 다 되면 준비 상태로 전환
            if (currentTimer <= 0f)
            {
                EnterCooldownPhase();
            }
        }
        else
        {
            // [준비 상태] 3초 -> 0.2초 -> 0초

            // 1. 남은 시간이 0.2초 이하 -> 애니메이션만 먼저 실행
            if (currentTimer <= preAnimTime && !hasPreAnimTriggered)
            {
                TriggerPreAnimation();
                hasPreAnimTriggered = true;
            }

            // 2. 남은 시간이 0초 이하 -> 실제 버프 발동 및 무기 해제
            if (currentTimer <= 0f)
            {
                EnterBuffPhase();
            }
        }
    }

    // -----------------------------------------------------------------------
    // [0.2초 전] 애니메이션만 실행
    // -----------------------------------------------------------------------
    private void TriggerPreAnimation()
    {
        nextDiceIndex = Random.Range(0, diceList.Length);

        if (GameManager.instance?.player != null)
        {
            GameManager.instance.player.SetDiceAnimation(nextDiceIndex);
            GameManager.instance.player.PlayWakeUpAnimation();
        }
    }

    // -----------------------------------------------------------------------
    // [0.0초 땡] 실제 버프 적용
    // -----------------------------------------------------------------------
    private void EnterBuffPhase()
    {
        isBuffActive = true;
        currentTimer = buffDuration;
        timeSinceStateStart = 0f;

        if (nextDiceIndex == -1) nextDiceIndex = Random.Range(0, diceList.Length);
        DiceData data = diceList[nextDiceIndex];

        if (GameManager.instance?.player != null)
        {
            GameManager.instance.player.SetChargingState(false);
            GameManager.instance.player.ApplyDiceBuff(data);
        }

        UpdateUI_BuffActive(data);
    }

    // -----------------------------------------------------------------------
    // [버프 끝] 준비 상태로 복귀
    // -----------------------------------------------------------------------
    private void EnterCooldownPhase()
    {
        isBuffActive = false;
        currentTimer = rollCooldown;
        timeSinceStateStart = 0f;

        hasPreAnimTriggered = false;
        nextDiceIndex = -1;

        if (GameManager.instance?.player != null)
        {
            GameManager.instance.player.RemoveDiceBuff();
            GameManager.instance.player.SetChargingState(true);
        }

        UpdateUI_Ready();
    }

    // -----------------------------------------------------------------------
    // 비주얼 효과
    // -----------------------------------------------------------------------
    private void HandleVisualEffects()
    {
        float currentAlpha = 1f;
        if (timeSinceStateStart < fadeInDuration) currentAlpha = timeSinceStateStart / fadeInDuration;
        else currentAlpha = 1f;
        if (uiCanvasGroup != null) uiCanvasGroup.alpha = currentAlpha;

        // 버프 상태이고, 남은 시간이 3초 이하일 때 카운트다운 표시 (버프 종료 임박 알림)
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
        else if (isBuffActive)
        {
            // 버프 중이지만 카운트다운 시간은 아닐 때 (머리 위 주사위 끄기)
            // (ShowResultRoutine 코루틴이 알아서 끄므로 여기선 건드리지 않음)
        }
        else
        {
            // 준비 시간에는 머리 위 주사위 롤링 애니메이션이 코루틴으로 돔
        }

        Color finalGlowColor = currentTargetColor;
        finalGlowColor.a = currentAlpha;
        if (diceGlowFilter != null) diceGlowFilter.Color = finalGlowColor;
        if (textGlowFilter != null) textGlowFilter.Color = finalGlowColor;
    }

    // ... (나머지 UI 업데이트 및 코루틴 함수들은 기존과 동일) ...

    private void UpdateUI_BuffActive(DiceData data)
    {
        currentTargetColor = data.uiGlowColor;
        moduleIcon.sprite = data.icon;
        desText.text = data.description;

        if (resultParticle != null)
        {
            var texSheet = resultParticle.textureSheetAnimation;
            texSheet.enabled = true;
            texSheet.mode = ParticleSystemAnimationMode.Sprites;
            texSheet.SetSprite(0, data.icon);
            resultParticle.Stop();
            resultParticle.Play();
        }

        if (diceGlowFilter != null) { diceGlowFilter.enabled = true; diceGlowFilter.Color = currentTargetColor; }
        if (textGlowFilter != null) { textGlowFilter.enabled = true; textGlowFilter.Color = currentTargetColor; }
        if (uiCanvasGroup != null) uiCanvasGroup.alpha = 0f;

        if (headDiceRenderer != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowResultRoutine(data));
        }
    }

    private void UpdateUI_Ready()
    {
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

        if (GameManager.instance?.player != null) GameManager.instance.player.SetChargingState(true);
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