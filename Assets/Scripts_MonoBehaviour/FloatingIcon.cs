using System.Collections;
using UnityEngine;

public class FloatingIcon : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float floatSpeed = 0.3f;           // 위로 떠오르는 속도
    [SerializeField] private float fadeDuration = 0.8f;         // 전체 지속 시간

    [Header("Animation")]
    [SerializeField] private float baseScale = 0.25f;           // 기본 스케일
    [SerializeField] private float popScale = 0.35f;            // 처음 튀어오를 때 최대 스케일
    [SerializeField] private float popDuration = 0.2f;          // 튀어오르는(팝) 연출에 쓰이는 시간

    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        // 초기 스케일을 0으로 설정하여 안 보이게 시작
        transform.localScale = Vector3.zero;
    }

    public void Setup(Sprite icon)
    {
        if (_spriteRenderer != null) _spriteRenderer.sprite = icon;
        StartCoroutine(FloatAndFadeRoutine());
    }

    private IEnumerator FloatAndFadeRoutine()
    {
        float elapsed = 0f;
        Color startColor = _spriteRenderer.color;

        while (elapsed < fadeDuration)
        {
            // 1. 위로 이동 (일정하게)
            transform.position += Vector3.up * (floatSpeed * Time.unscaledDeltaTime);

            // 2. 스케일 애니메이션 (팝 & 안착)
            if (elapsed < popDuration)
            {
                // 0 -> popScale(0.35) 로 빠르게 커짐
                float t = elapsed / popDuration;
                // Ease Out 효과를 위해 t 연산 (부드럽게 커지다가 감속)
                t = Mathf.Sin(t * Mathf.PI * 0.5f);
                float currentScale = Mathf.Lerp(0f, popScale, t);
                transform.localScale = new Vector3(currentScale, currentScale, 1f);
            }
            else if (elapsed < popDuration * 2f)
            {
                // popScale(0.35) -> baseScale(0.25) 로 살짝 줄어들며 안착
                float t = (elapsed - popDuration) / popDuration;
                float currentScale = Mathf.Lerp(popScale, baseScale, t);
                transform.localScale = new Vector3(currentScale, currentScale, 1f);
            }
            else
            {
                // 이후에는 baseScale 유지
                transform.localScale = new Vector3(baseScale, baseScale, 1f);
            }

            // 3. 투명도 애니메이션 (페이드 아웃)
            // 전체 시간의 절반(0.4초)이 지난 후부터 투명해지기 시작
            float fadeStartTime = fadeDuration * 0.5f;
            if (elapsed > fadeStartTime)
            {
                float fadeProgress = (elapsed - fadeStartTime) / (fadeDuration - fadeStartTime);
                float alpha = Mathf.Lerp(1f, 0f, fadeProgress);
                _spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}