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
        "전투 중에는 열 수 없습니다.",            // 0
        "전투 중에는 획득할 수 없습니다.",          // 1
        "아티팩트를 더 이상 획득할 수 없습니다. (최대 10)", // 2
        "수정구가 힘을 잃어 사용할 수 없습니다.",       // 3
        "보상을 먼저 획득해야 합니다.",            // 4
        "정화 작업 완료까지 경로가 차단됩니다.",        // 5
        "고철이 부족합니다.",                  // 6
        "회복 불가 상태이므로 구매할 수 없습니다."      // ★ 7: 새로 추가됨
    };

    [Header("Animation Settings")]
    [SerializeField] private float fadeInTime = 0.105f;  
    [SerializeField] private float displayTime = 2f; 
    [SerializeField] private float fadeOutTime = 0.2f; 
    [SerializeField] private float floatSpeed = 0f;  

    [Header("Sound Settings")]
    [SerializeField] private AudioClip warningSound;   

    private Coroutine _currentRoutine; 
    private float _remainTime;         
    private Vector3 _startLocalPos;    

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (feedbackText != null)
        {
            _startLocalPos = feedbackText.transform.localPosition;

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

        _remainTime = displayTime;

        if (_currentRoutine != null)
        {
            StopCoroutine(_currentRoutine);
            feedbackText.transform.localPosition = _startLocalPos;
            Color c = feedbackText.color;
            c.a = 1f; 
            feedbackText.color = c;
        }
        
        _currentRoutine = StartCoroutine(FadeAndFloatRoutine());
    }

    private IEnumerator FadeAndFloatRoutine()
    {
        Color color = feedbackText.color;

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

        while (_remainTime > 0f)
        {
            _remainTime -= Time.deltaTime;
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
        _currentRoutine = null;
    }
}