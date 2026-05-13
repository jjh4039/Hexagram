using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerInfoUI : MonoBehaviour
{
    public Slider healthSlider; 
    public TextMeshProUGUI healthText; 
    
    [Header("SafeZone UI Settings")]
    public TextMeshProUGUI safeZoneText; // GameObject에서 TextMeshProUGUI로 변경
    [SerializeField] private float fadeDuration = 0.6f; // 페이드 속도
    public bool isTutorial = false;
    
    private Coroutine fadeCoroutine;

    private void Start()
    {
        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.OnGamePhaseChanged += HandleGamePhaseChanged;
            
            // 튜토리얼 중이면 무조건 비표시 처리
            bool isSafe = (InputStateManager.Instance.CurrentPhase == GamePhase.SafeZone) && !isTutorial;
            if (safeZoneText != null)
            {
                safeZoneText.text = "비전투 상태 : 대시 소모 없음";
                Color c = safeZoneText.color;
                c.a = isSafe ? 1f : 0f;
                safeZoneText.color = c;
            }
        }
    }

    private void HandleGamePhaseChanged(GamePhase newPhase)
    {
        if (safeZoneText != null)
        {
            bool isSafe = (newPhase == GamePhase.SafeZone) && !isTutorial;
            
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(Co_FadeText(isSafe));
        }
    }

    private void OnDestroy()
    {
        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.OnGamePhaseChanged -= HandleGamePhaseChanged;
        }
    }
    
    private IEnumerator Co_FadeText(bool fadeIn)
    {
        float targetAlpha = fadeIn ? 1f : 0f;
        Color c = safeZoneText.color;
        float startAlpha = c.a;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            safeZoneText.color = c;
            yield return null;
        }

        c.a = targetAlpha;
        safeZoneText.color = c;
        fadeCoroutine = null;
    }

    public void Update()
    {
        if (GameManager.instance == null || GameManager.instance.stats == null) return;

        healthSlider.maxValue = GameManager.instance.stats.maxHealth;
        healthSlider.value = GameManager.instance.stats.currentHealth;
        healthText.text = $"{GameManager.instance.stats.currentHealth} / {GameManager.instance.stats.maxHealth}";
    }
}