using System.Collections;
using UnityEngine;
using TMPro;

public class PlayerFeedbackUI : MonoBehaviour
{
    public static PlayerFeedbackUI Instance { get; private set; } // 전역 인스턴스

    [Header("UI References")]
    [SerializeField] private TMP_Text feedbackText; // 출력 텍스트

    [Header("Messages")]
    [SerializeField]
    private string[] warningMessages = new string[]
    {
        "전투 중에는 열 수 없습니다.",
        "전투 중에는 획득할 수 없습니다.",
        "아티팩트를 더 이상 획득할 수 없습니다. (최대 10)",
        "수정구가 힘을 잃어 사용할 수 없습니다",
        "보상을 먼저 획득해야 합니다.",
        "모듈 정화 완료 시 사라집니다."
    };

    [Header("Animation Settings")]
    [SerializeField] private float fadeInTime = 0.1f;  // 페이드인 속도
    [SerializeField] private float displayTime = 1.5f; // 표시 시간
    [SerializeField] private float fadeOutTime = 0.5f; // 페이드아웃 속도
    [SerializeField] private float floatSpeed = 0.5f;  // 상승 속도

    [Header("Sound Settings")]
    [SerializeField] private AudioClip warningSound;   // 출력 사운드

    private Coroutine currentRoutine; // 실행중인 코루틴
    private float remainTime;         // 남은 표시 시간
    private Vector3 startLocalPos;    // 초기 위치

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (feedbackText != null)
        {
            startLocalPos = feedbackText.transform.localPosition;

            Color color = feedbackText.color;
            color.a = 0f;
            feedbackText.color = color;
        }
    }

    public void ShowWarning(int messageIndex)
    {
        if (feedbackText == null) return;

        if (messageIndex >= 0 && messageIndex < warningMessages.Length)
        {
            feedbackText.text = warningMessages[messageIndex];
        }
        else
        {
            feedbackText.text = "알 수 없는 오류입니다.";
        }

        // 사운드 재생 로직 추가
        if (warningSound != null && SoundManager.instance != null)
        {
            SoundManager.instance.PlaySFX(warningSound);
        }

        remainTime = displayTime;

        if (currentRoutine == null)
        {
            currentRoutine = StartCoroutine(FadeAndFloatRoutine());
        }
    }

    private IEnumerator FadeAndFloatRoutine()
    {
        feedbackText.transform.localPosition = startLocalPos;
        Color color = feedbackText.color;

        while (color.a < 1f)
        {
            color.a += Time.deltaTime / fadeInTime;
            feedbackText.color = color;
            yield return null;
        }
        color.a = 1f;
        feedbackText.color = color;

        while (remainTime > 0f)
        {
            remainTime -= Time.deltaTime;
            feedbackText.transform.localPosition += Vector3.up * (floatSpeed * Time.deltaTime);
            yield return null;
        }

        while (color.a > 0f)
        {
            color.a -= Time.deltaTime / fadeOutTime;
            feedbackText.color = color;
            feedbackText.transform.localPosition += Vector3.up * (floatSpeed * Time.deltaTime);
            yield return null;
        }

        color.a = 0f;
        feedbackText.color = color;
        currentRoutine = null;
    }
}