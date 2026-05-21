using System.Collections;
using UnityEngine;
using TMPro;

public class PlayerFeedbackUI : MonoBehaviour
{
    public static PlayerFeedbackUI Instance { get; private set; } 

    [Header("UI References")]
    [SerializeField] private TMP_Text feedbackText; 

    [Header("Messages")]
    [SerializeField]
    private string[] warningMessages = new string[]
    {
        "전투 중에는 열 수 없습니다.",
        "전투 중에는 획득할 수 없습니다.",
        "아티팩트를 더 이상 획득할 수 없습니다. (최대 10)",
        "수정구가 힘을 잃어 사용할 수 없습니다.",
        "보상을 먼저 획득해야 합니다.",
        "정화 작업 완료까지 경로가 차단됩니다.",
        "스크랩이 부족합니다." 
    };

    [Header("Animation Settings")]
    [SerializeField] private float fadeInTime = 0.1f;  
    [SerializeField] private float displayTime = 1.5f; 
    [SerializeField] private float fadeOutTime = 0.5f; 
    [SerializeField] private float floatSpeed = 0.5f;  

    [Header("Sound Settings")]
    [SerializeField] private AudioClip warningSound;   

    private Coroutine currentRoutine; 
    private float remainTime;         
    private Vector3 startLocalPos;    

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

        if (warningSound != null && SoundManager.instance != null)
        {
            SoundManager.instance.PlaySFX(warningSound);
        }

        remainTime = displayTime;

        // ★ 수정: 경고가 연달아 들어왔을 때 깜빡이거나 투명해지는 버그 방지
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            feedbackText.transform.localPosition = startLocalPos;
            Color c = feedbackText.color;
            c.a = 1f; 
            feedbackText.color = c;
        }
        
        currentRoutine = StartCoroutine(FadeAndFloatRoutine());
    }

    private IEnumerator FadeAndFloatRoutine()
    {
        Color color = feedbackText.color;

        // 투명할 경우 페이드 인
        if (color.a < 1f)
        {
            while (color.a < 1f)
            {
                color.a += Time.deltaTime / fadeInTime;
                feedbackText.color = color;
                yield return null;
            }
            color.a = 1f;
            feedbackText.color = color;
        }

        // 표시 유지 (새 경고가 오면 remainTime이 리셋됨)
        while (remainTime > 0f)
        {
            remainTime -= Time.deltaTime;
            feedbackText.transform.localPosition += Vector3.up * (floatSpeed * Time.deltaTime);
            yield return null;
        }

        // 페이드 아웃
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