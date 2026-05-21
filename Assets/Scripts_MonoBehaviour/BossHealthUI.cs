using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BossHealthUI : MonoBehaviour
{
    public static BossHealthUI Instance;

    [Header("UI Components")]
    [SerializeField] private CanvasGroup bossCanvasGroup;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("Sliders (Layered)")]
    [SerializeField] private Slider mainSlider;     
    [SerializeField] private Slider flashSlider;    
    [SerializeField] private Slider bufferSlider1;  
    [SerializeField] private Slider bufferSlider2;  

    private float _maxHp;
    private Coroutine _bufferCoroutine;
    private Coroutine _flashCoroutine; // ★ 추가: 연타 시 플래시 중첩 방지

    private float _currentTargetFill = 1f;
    private bool _isIntroFilling = false;

    private void Awake() => Instance = this;

    public void SetupBoss(string name, float maxHealth)
    {
        this._maxHp = maxHealth;
        if (nameText) nameText.text = name;

        mainSlider.value = 0;
        flashSlider.value = 0;
        bufferSlider1.value = 0;
        bufferSlider2.value = 0;

        if (hpText) hpText.text = $"0 / {_maxHp:N0}";

        bossCanvasGroup.alpha = 0f;

        _currentTargetFill = 1f;
        _isIntroFilling = true;
        StartCoroutine(Co_IntroFill());
    }

    private IEnumerator Co_IntroFill()
    {
        float elapsed = 0f;
        float duration = 1.5f; 

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            bossCanvasGroup.alpha = Mathf.Clamp01(elapsed / 0.5f);

            float currentFill = Mathf.Lerp(0, _currentTargetFill, t);

            bufferSlider2.value = Mathf.Lerp(0, _currentTargetFill, t * 1.5f);
            bufferSlider1.value = Mathf.Lerp(0, _currentTargetFill, t * 1.25f);
            flashSlider.value = currentFill;
            mainSlider.value = currentFill;

            if (hpText)
            {
                float currentDisplayHp = _maxHp * mainSlider.value;
                hpText.text = $"{currentDisplayHp:N0} / {_maxHp:N0}";
            }

            yield return null;
        }

        _isIntroFilling = false;
        bossCanvasGroup.alpha = 1f;

        UpdateBossHealth(_currentTargetFill * _maxHp);
    }

    public void UpdateBossHealth(float currentHp)
    {
        float targetFill = currentHp / _maxHp;
        _currentTargetFill = targetFill; 

        if (hpText)
            hpText.text = $"{Mathf.Max(0, currentHp):N0} / {_maxHp:N0}";

        if (_isIntroFilling) return;

        mainSlider.value = targetFill;

        // ★ 수정: 중첩 실행 방어
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(Co_FlashEffect(targetFill));

        if (_bufferCoroutine != null) StopCoroutine(_bufferCoroutine);
        _bufferCoroutine = StartCoroutine(Co_BufferFollow(targetFill));
    }

    private IEnumerator Co_FlashEffect(float target)
    {
        flashSlider.value = target + 0.005f; 
        yield return new WaitForSeconds(0.1f);
        flashSlider.value = target;
    }

    private IEnumerator Co_BufferFollow(float target)
    {
        while (Mathf.Abs(bufferSlider2.value - target) > 0.001f)
        {
            bufferSlider1.value = Mathf.Lerp(bufferSlider1.value, target, Time.deltaTime * 5f);
            bufferSlider2.value = Mathf.Lerp(bufferSlider2.value, target, Time.deltaTime * 2f);
            yield return null;
        }
        bufferSlider1.value = target;
        bufferSlider2.value = target;
    }

    public void HideUI() => StartCoroutine(Co_FadeOut());

    private IEnumerator Co_FadeOut()
    {
        yield return new WaitForSeconds(2f);
        float timer = 0;
        while (timer < 1f)
        {
            timer += Time.deltaTime;
            bossCanvasGroup.alpha = 1 - timer;
            yield return null;
        }
    }
}