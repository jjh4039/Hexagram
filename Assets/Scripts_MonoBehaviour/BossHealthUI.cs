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

    private void Awake() => instance = this;

    public void SetupBoss(string name, float maxHealth)
    {
        this.maxHP = maxHealth;
        if (nameText != null) nameText.text = name;

        // 초기화: 모두 0에서 시작
        mainSlider.value = 0;
        flashSlider.value = 0;
        bufferSlider1.value = 0;
        bufferSlider2.value = 0;

        if (hpText != null) hpText.text = $"0 / {maxHP:N0}";

        bossCanvasGroup.alpha = 1f;

        // ★ [추가] 등장 인트로 연출 시작
        StartCoroutine(Co_IntroFill());
    }

    private IEnumerator Co_IntroFill()
    {
        float elapsed = 0f;
        float duration = 3f; // 전체 연출 시간

        // 시작 시 모든 바는 0
        mainSlider.value = 0;
        flashSlider.value = 0;
        bufferSlider1.value = 0;
        bufferSlider2.value = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 1. 부드러운 진행도를 위해 에니메이션 커브와 유사한 t값 계산 (선택사항)
            // t = Mathf.Sin(t * Mathf.PI * 0.5f); 

            // 2. 각 레이어별 차등 속도 적용 (비율에 따라 다르게 차오름)
            // 잔상들이 먼저 빠르게 차오르고, 메인이 가장 느리게 따라감
            bufferSlider2.value = Mathf.Lerp(0, 1f, t * 1.5f); // 가장 빠름
            bufferSlider1.value = Mathf.Lerp(0, 1f, t * 1.25f);
            flashSlider.value = Mathf.Lerp(0, 1f, t);
            mainSlider.value = Mathf.Lerp(0, 1f, t);          // 가장 느림

            // 3. 텍스트 카운팅 (메인 바 기준으로 표시)
            if (hpText != null)
            {
                float currentDisplayHP = maxHP * mainSlider.value;
                hpText.text = $"{currentDisplayHP:N0} / {maxHP:N0}";
            }

            yield return null;
        }

        // 최종 값 보정
        mainSlider.value = 1f;
        flashSlider.value = 1f;
        bufferSlider1.value = 1f;
        bufferSlider2.value = 1f;
        if (hpText != null) hpText.text = $"{maxHP:N0} / {maxHP:N0}";
    }

    public void UpdateBossHealth(float currentHP)
    {
        float targetFill = currentHP / maxHP;

        // 텍스트 업데이트
        if (hpText != null)
            hpText.text = $"{Mathf.Max(0, currentHP):N0} / {maxHP:N0}";

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
        // Lerp 속도는 기호에 따라 인스펙터 변수로 빼셔도 좋습니다.
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