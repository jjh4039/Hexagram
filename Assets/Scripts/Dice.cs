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
    [SerializeField] private float rollCooldown = 5f;

    [Header("--- Visual Settings ---")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutStartSeconds = 3.0f;

    [Header("--- UI Group (Left Top) ---")]
    [SerializeField] private CanvasGroup uiCanvasGroup;

    [Header("--- UI Elements (Left Top) ---")]
    [SerializeField] private Image moduleIcon;
    [SerializeField] private Image moduleCooldownOverlay;
    [SerializeField] private TextMeshProUGUI desText;

    [Header("--- Head Object (World) ---")]
    [SerializeField] private SpriteRenderer headDiceRenderer;
    [SerializeField] private Sprite[] greyDiceSprites;
    [SerializeField] private float rollAnimSpeed = 0.08f;

    [Header("--- Head Scale & Effect Settings ---")]
    [SerializeField] private float rollingScale = 1.0f;     // 굴러갈 때 크기
    [SerializeField] private float resultDiceScale = 0.05f; // 결과 크기
    [SerializeField] private float ghostDuration = 0.4f;    // 잔상 지속 시간
    [SerializeField] private float ghostScaleMultiplier = 2.0f; // 잔상 크기 배율
    [SerializeField] private float resultVisibleTime = 1.5f; // ★ 결과가 선명하게 보이는 시간
    [SerializeField] private float resultFadeDuration = 0.5f; // ★ 결과가 흐려지며 사라지는 시간

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
        UpdateUI_Ready();
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
            if (isBuffActive) EndBuff();
            else RollDice();
        }
    }

    private void HandleVisualEffects()
    {
        float currentAlpha = 1f;

        if (timeSinceStateStart < fadeInDuration)
        {
            currentAlpha = timeSinceStateStart / fadeInDuration;
        }
        else if (isBuffActive && currentTimer <= fadeOutStartSeconds)
        {
            currentAlpha = currentTimer / fadeOutStartSeconds;
        }
        else
        {
            currentAlpha = 1f;
        }

        if (uiCanvasGroup != null) uiCanvasGroup.alpha = currentAlpha;

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
            StopAllCoroutines();
            StartCoroutine(ShowResultRoutine(diceVal));
        }
    }

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
            StartCoroutine(RollingRoutine());
        }
    }

    // --- Coroutines for Head Dice ---

    // [충전 중] 회색 주사위 굴리기
    private IEnumerator RollingRoutine()
    {
        headDiceRenderer.gameObject.SetActive(true);
        // ★ 중요: 다시 굴릴 때 투명도를 1(불투명)로 복구해야 함
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

    // [발동!] 결과 보여주고 잔상 -> 서서히 사라짐
    private IEnumerator ShowResultRoutine(int diceVal)
    {
        // 1. 초기화 (투명도 1)
        headDiceRenderer.color = Color.white;
        headDiceRenderer.sprite = diceSprites[diceVal];
        headDiceRenderer.transform.localScale = Vector3.one * resultDiceScale;

        // 2. 잔상 이펙트
        StartCoroutine(PlayGhostEffect(diceSprites[diceVal], headDiceRenderer.transform.position, headDiceRenderer.transform.localScale));

        // 3. 선명하게 유지하는 시간 (예: 1.5초)
        yield return new WaitForSeconds(resultVisibleTime);

        // 4. ★ 서서히 투명해지기 (Fade Out) (예: 0.5초)
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

        // 5. 완전히 끄기
        headDiceRenderer.gameObject.SetActive(false);
    }

    // 잔상 효과 코루틴
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