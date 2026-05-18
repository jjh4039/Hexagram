using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BossHealthUI : MonoBehaviour
{
    public static BossHealthUI instance;

    [Header("UI Components")]
    [SerializeField] private CanvasGroup bossCanvasGroup;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("Sliders (Layered)")]
    [SerializeField] private Slider mainSlider;     // 메인 바 (인스펙터에서 #FF3131 설정)
    [SerializeField] private Slider flashSlider;    // 플래시 바 (인스펙터에서 #FFFFFF 설정)
    [SerializeField] private Slider bufferSlider1;  // 빠른 잔상 (인스펙터에서 #FFD700 설정)
    [SerializeField] private Slider bufferSlider2;  // 느린 잔상 (인스펙터에서 #8B0000 설정)

    private float maxHP;
    private Coroutine bufferCoroutine;

    // ★ 추가: 인트로 연출 중에도 깎인 체력을 기억하기 위한 변수
    private float currentTargetFill = 1f;
    private bool isIntroFilling = false;

    private void Awake() => instance = this;

    public void SetupBoss(string name, float maxHealth)
    {
        this.maxHP = maxHealth;
        if (nameText != null) nameText.text = name;

        mainSlider.value = 0;
        flashSlider.value = 0;
        bufferSlider1.value = 0;
        bufferSlider2.value = 0;

        if (hpText != null) hpText.text = $"0 / {maxHP:N0}";

        bossCanvasGroup.alpha = 0f;

        currentTargetFill = 1f;
        isIntroFilling = true;
        StartCoroutine(Co_IntroFill());
    }

    private IEnumerator Co_IntroFill()
    {
        float elapsed = 0f;
        float duration = 1.5f; // 전체 연출 시간

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 초반 0.5초 동안 체력바 UI 자체가 스르륵 나타남
            bossCanvasGroup.alpha = Mathf.Clamp01(elapsed / 0.5f);

            // ★ 1이 아니라 '목표 체력(currentTargetFill)'에 비례해서 차오르게 변경
            float currentFill = Mathf.Lerp(0, currentTargetFill, t);

            bufferSlider2.value = Mathf.Lerp(0, currentTargetFill, t * 1.5f);
            bufferSlider1.value = Mathf.Lerp(0, currentTargetFill, t * 1.25f);
            flashSlider.value = currentFill;
            mainSlider.value = currentFill;

            if (hpText != null)
            {
                float currentDisplayHP = maxHP * mainSlider.value;
                hpText.text = $"{currentDisplayHP:N0} / {maxHP:N0}";
            }

            yield return null;
        }

        isIntroFilling = false;
        bossCanvasGroup.alpha = 1f;

        // 인트로 종료 후 정확한 최종 수치로 동기화
        UpdateBossHealth(currentTargetFill * maxHP);
    }

    public void UpdateBossHealth(float currentHP)
    {
        float targetFill = currentHP / maxHP;
        currentTargetFill = targetFill; // ★ 인트로 중에도 깎인 체력 비율 갱신

        // 텍스트 업데이트
        if (hpText != null)
            hpText.text = $"{Mathf.Max(0, currentHP):N0} / {maxHP:N0}";

        // ★ 인트로가 진행 중일 때는 UI가 차오르는 연출만 하도록 아래 슬라이더 로직은 스킵
        if (isIntroFilling) return;

        // 1. 메인 바는 즉각 반응
        mainSlider.value = targetFill;

        // 2. 흰색 플래시 연출
        StartCoroutine(Co_FlashEffect(targetFill));

        // 3. 잔상 코루틴 (중첩 방지)
        if (bufferCoroutine != null) StopCoroutine(bufferCoroutine);
        bufferCoroutine = StartCoroutine(Co_BufferFollow(targetFill));
    }

    private IEnumerator Co_FlashEffect(float target)
    {
        flashSlider.value = target + 0.005f; // 아주 미세하게 높게 잡아 겹침 방지
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