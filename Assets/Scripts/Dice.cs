using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChocDino.UIFX;

public class Dice : MonoBehaviour
{
    [Header("--- Settings ---")]
    [SerializeField] private Color32[] diceColors;      // ★ 파티클 전용 (진한 색)
    [SerializeField] private Color32[] lightDiceColors; // ★ UI FX 전용 (연한 색)
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

    [Header("--- Resources ---")]
    [SerializeField] private Sprite[] diceSprites;

    private float currentTimer;
    private bool isBuffActive = false;
    private Color currentTargetColor; // 현재 적용중인 lightDiceColor 저장용
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
        if (GameManager.instance?.player != null) GameManager.instance.player.SetChargingState(false);
        yield return new WaitForSeconds(preResultDelay);

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

    // --- UI 및 상태 업데이트 ---

    private void UpdateUI_BuffActive(int diceVal)
    {
        // 1. 주사위 결과 색상 가져오기 (진한 색 사용)
        Color resultColor = diceColors[diceVal]; // Color32는 Color로 자동 형변환됩니다.

        // 2. 파티클 색상 설정 및 실행
        if (resultParticle != null)
        {
            var main = resultParticle.main;

            // ★ [수정] MinMaxGradient 형식으로 변환하여 대입
            main.startColor = new ParticleSystem.MinMaxGradient(resultColor);

            resultParticle.Stop();
            resultParticle.Play();
        }

        // 3. UI 및 내부 타겟 색상 갱신 (연한 색 사용)
        currentTargetColor = lightDiceColors[diceVal];
        moduleIcon.sprite = diceSprites[diceVal];
        desText.text = GetShortDescription(diceVal);

        if (diceGlowFilter != null) { diceGlowFilter.enabled = true; diceGlowFilter.Color = currentTargetColor; }
        if (textGlowFilter != null) { textGlowFilter.enabled = true; textGlowFilter.Color = currentTargetColor; }
        if (uiCanvasGroup != null) uiCanvasGroup.alpha = 0f;

        if (headDiceRenderer != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowResultRoutine(diceVal));
        }
    }

    private void UpdateUI_Ready()
    {
        // 충전 중 UI 색상 설정 (연한 대기색 사용)
        currentTargetColor = lightDiceColors[6]; // ★ lightDiceColors 사용

        if (diceSprites.Length > 6) moduleIcon.sprite = diceSprites[6];
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

    private void HandleVisualEffects()
    {
        float currentAlpha = 1f;
        if (timeSinceStateStart < fadeInDuration) currentAlpha = timeSinceStateStart / fadeInDuration;
        else currentAlpha = 1f;
        if (uiCanvasGroup != null) uiCanvasGroup.alpha = currentAlpha;

        // 카운트다운 연출 (머리 위)
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

        // Glow 효과 업데이트 (연한 색 + 알파값)
        Color finalGlowColor = currentTargetColor;
        finalGlowColor.a = currentAlpha;
        if (diceGlowFilter != null) diceGlowFilter.Color = finalGlowColor;
        if (textGlowFilter != null) textGlowFilter.Color = finalGlowColor;
    }

    // ... (RollingRoutine, ShowResultRoutine, PlayGhostEffect, GetShortDescription 등 기존 코루틴 로직 동일)
    // (이하 중복 코드 생략)
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

    private IEnumerator ShowResultRoutine(int diceVal)
    {
        headDiceRenderer.gameObject.SetActive(true);
        headDiceRenderer.color = Color.white;
        headDiceRenderer.sprite = diceSprites[diceVal];
        headDiceRenderer.transform.localScale = Vector3.one * resultDiceScale;
        StartCoroutine(PlayGhostEffect(diceSprites[diceVal], headDiceRenderer.transform.position, headDiceRenderer.transform.localScale));
        if (GameManager.instance.player != null) GameManager.instance.player.SetDiceAnimation(diceVal);
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