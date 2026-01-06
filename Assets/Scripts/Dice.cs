using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChocDino.UIFX;

public class Dice : MonoBehaviour
{
    [Header("--- Settings ---")]
    [SerializeField] private Color32[] diceColors;
    [SerializeField] private float buffDuration = 10f;
    [SerializeField] private float rollCooldown = 3f; // 회색 주사위 굴러가는 시간

    [Header("--- Visual Settings ---")]
    [SerializeField] private float fadeInDuration = 0.5f;

    // ★ [수정] 깜빡임 대신 카운트다운 시작 시간으로 사용
    [SerializeField] private float countdownStartSeconds = 3.0f;

    [Header("--- UI Group (Left Top) ---")]
    [SerializeField] private CanvasGroup uiCanvasGroup;

    [Header("--- UI Elements (Left Top) ---")]
    [SerializeField] private Image moduleIcon;
    [SerializeField] private Image moduleCooldownOverlay;
    [SerializeField] private TextMeshProUGUI desText;

    [Header("--- Head Object (World) ---")]
    [SerializeField] private SpriteRenderer headDiceRenderer;

    // ★ [추가] 3, 2, 1 카운트다운 스프라이트 (인스펙터에서 할당: 0=숫자3, 1=숫자2, 2=숫자1)
    [SerializeField] private Sprite[] countdownSprites;

    [SerializeField] private Sprite[] greyDiceSprites; // 회색 주사위 (굴러갈 때)
    [SerializeField] private float rollAnimSpeed = 0.08f;

    [Header("--- Head Scale & Effect Settings ---")]
    [SerializeField] private float rollingScale = 1.0f;
    [SerializeField] private float resultDiceScale = 0.05f;
    [SerializeField] private float ghostDuration = 0.4f;
    [SerializeField] private float ghostScaleMultiplier = 2.0f;
    [SerializeField] private float resultVisibleTime = 1.5f;
    [SerializeField] private float resultFadeDuration = 0.5f;

    [Header("--- FX ---")]
    [SerializeField] private GlowFilter diceGlowFilter;
    [SerializeField] private GlowFilter textGlowFilter;

    [Header("--- Resources ---")]
    [SerializeField] private Sprite[] diceSprites;

    // 내부 변수
    private float currentTimer;
    private bool isBuffActive = false;
    private Color currentTargetColor;
    private float timeSinceStateStart;

    private void Start()
    {
        InitializeState();
    }

    private void Update()
    {
        HandleTimer();
        HandleVisualEffects();
    }

    private void InitializeState()
    {
        isBuffActive = false;
        currentTimer = rollCooldown;
        UpdateUI_Ready(); // 시작하면 회색 주사위 굴리기
    }

    private void HandleTimer()
    {
        currentTimer -= Time.deltaTime;
        timeSinceStateStart += Time.deltaTime;

        float maxTime = isBuffActive ? buffDuration : rollCooldown;

        if (moduleCooldownOverlay != null)
        {
            moduleCooldownOverlay.fillAmount = 1f - (currentTimer / maxTime);
        }

        if (currentTimer <= 0)
        {
            if (isBuffActive) EndBuff(); // 버프 끝 -> 회색 주사위 시작
            else RollDice(); // 회색 주사위 끝 -> 결과 뽑기
        }
    }

    // ★ [핵심 수정] 3초 남았을 때 카운트다운 표시 로직 추가
    private void HandleVisualEffects()
    {
        // 1. UI 투명도 관리 (기존 유지)
        float currentAlpha = 1f;
        if (timeSinceStateStart < fadeInDuration)
        {
            currentAlpha = timeSinceStateStart / fadeInDuration;
        }
        else
        {
            currentAlpha = 1f;
        }
        if (uiCanvasGroup != null) uiCanvasGroup.alpha = currentAlpha;


        // 2. 머리 위 카운트다운 (버프 상태이고, 시간이 3초 이하로 남았을 때)
        if (isBuffActive)
        {
            if (currentTimer <= countdownStartSeconds)
            {
                // 코루틴(결과 보여주기) 등과 겹치지 않게 켜줌
                if (!headDiceRenderer.gameObject.activeSelf)
                    headDiceRenderer.gameObject.SetActive(true);

                // 색상 초기화 (결과창에서 투명해졌을 수 있으므로)
                headDiceRenderer.color = Color.white;
                headDiceRenderer.transform.localScale = Vector3.one * rollingScale;

                // 3초 -> 인덱스0(3), 2초 -> 인덱스1(2), 1초 -> 인덱스2(1)
                if (countdownSprites != null && countdownSprites.Length >= 3)
                {
                    int index = 3 - Mathf.CeilToInt(currentTimer);
                    index = Mathf.Clamp(index, 0, countdownSprites.Length - 1);
                    headDiceRenderer.sprite = countdownSprites[index];
                }
            }
            // 3초보다 많이 남았을 때는? 
            // ShowResultRoutine이 알아서 끄게 둠 (여기서 끄면 결과 나오자마자 꺼질 수 있음)
        }

        // 3. 글로우 효과 (기존 유지)
        Color finalGlowColor = currentTargetColor;
        finalGlowColor.a = currentAlpha;
        if (diceGlowFilter != null) diceGlowFilter.Color = finalGlowColor;
        if (textGlowFilter != null) textGlowFilter.Color = finalGlowColor;
    }

    private void RollDice()
    {
        int diceValue = Random.Range(0, 6);

        isBuffActive = true;
        currentTimer = buffDuration;
        timeSinceStateStart = 0f;

        UpdateUI_BuffActive(diceValue);
    }

    private void EndBuff()
    {
        isBuffActive = false;
        currentTimer = rollCooldown;
        timeSinceStateStart = 0f;

        UpdateUI_Ready();
    }

    // --- UI & Head Dice State Change ---

    // [결과 나옴 & 버프 시작]
    private void UpdateUI_BuffActive(int diceVal)
    {
        currentTargetColor = diceColors[diceVal];
        moduleIcon.sprite = diceSprites[diceVal];
        desText.text = GetShortDescription(diceVal);

        if (diceGlowFilter != null) { diceGlowFilter.enabled = true; diceGlowFilter.Color = currentTargetColor; }
        if (textGlowFilter != null) { textGlowFilter.enabled = true; textGlowFilter.Color = currentTargetColor; }
        if (uiCanvasGroup != null) uiCanvasGroup.alpha = 0f;

        if (headDiceRenderer != null)
        {
            StopAllCoroutines(); // 돌던거 멈추고
            StartCoroutine(ShowResultRoutine(diceVal)); // 결과 보여주기
        }

        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            GameManager.instance.player.SetChargingState(false);
        }
    }

    // [버프 끝 & 회색 주사위 굴리기(충전)]
    private void UpdateUI_Ready()
    {
        currentTargetColor = diceColors[6];
        if (diceSprites.Length > 6) moduleIcon.sprite = diceSprites[6];
        desText.text = "주사위 충전 중...";

        if (diceGlowFilter != null) { diceGlowFilter.enabled = true; diceGlowFilter.Color = currentTargetColor; }
        if (textGlowFilter != null) { textGlowFilter.enabled = true; textGlowFilter.Color = currentTargetColor; }
        if (uiCanvasGroup != null) uiCanvasGroup.alpha = 0f;

        if (headDiceRenderer != null)
        {
            StopAllCoroutines();
            StartCoroutine(RollingRoutine()); // ★ 회색 주사위 굴리기 시작
        }

        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            GameManager.instance.player.SetChargingState(true);
        }
    }

    // --- Coroutines ---

    // 회색 주사위 굴러가는 연출 (기존 유지)
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

    // 결과 보여주고 페이드아웃 (기존 유지)
    private IEnumerator ShowResultRoutine(int diceVal)
    {
        headDiceRenderer.gameObject.SetActive(true); // 확실하게 켜주기
        headDiceRenderer.color = Color.white;
        headDiceRenderer.sprite = diceSprites[diceVal];
        headDiceRenderer.transform.localScale = Vector3.one * resultDiceScale;

        StartCoroutine(PlayGhostEffect(diceSprites[diceVal], headDiceRenderer.transform.position, headDiceRenderer.transform.localScale));

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
        // 여기서 꺼지지만, 나중에 3초 남았을 때 HandleVisualEffects가 다시 켭니다.
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

    private string GetShortDescription(int diceVal)
    {
        return diceVal switch
        {
            0 => "체력이 1 감소하고, 공격력이 10% 상승합니다.",
            1 => "다음 2회의 공격이 강한 피해를 입힙니다.",
            2 => "영구적으로 공격력 3% 상승합니다.",
            3 => "체력을 4 회복합니다. (초과 시 방어막)",
            4 => "이동속도, 공격속도가 40% 상승합니다.",
            5 => "원거리 무기의 충전 속도가 6배로 적용됩니다.",
            _ => "Error"
        };
    }
}