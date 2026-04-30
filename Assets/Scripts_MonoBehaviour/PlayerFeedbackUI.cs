using System.Collections;
using UnityEngine;
using TMPro;

// 플레이어에게 시각적인 경고 텍스트를 띄워주는 UI 전담 매니저
public class PlayerFeedbackUI : MonoBehaviour
{
    public static PlayerFeedbackUI Instance { get; private set; } // 전역 접근용 인스턴스

    [Header("UI References")]
    [SerializeField] private TMP_Text feedbackText; // 에디터에서 연결할 텍스트 컴포넌트

    [Header("Messages")]
    [SerializeField]
    private string[] warningMessages = new string[]
    {
        "전투 중에는 열 수 없습니다.",            // 0번
        "전투 중에는 획득할 수 없습니다.",          // 1번
        "아티팩트를 더 이상 획득할 수 없습니다. (최대 10)"     // 2번 (신규)
    };

    [Header("Animation Settings")]
    [SerializeField] private float fadeInTime = 0.1f;  // 글자가 선명해지는 속도
    [SerializeField] private float displayTime = 1.5f; // 글자가 화면에 머무는 시간
    [SerializeField] private float fadeOutTime = 0.5f; // 글자가 투명해지는 속도
    [SerializeField] private float floatSpeed = 0.5f;  // 글자가 위로 떠오르는 속도

    private Coroutine currentRoutine; // 현재 실행 중인 연출 코루틴
    private float remainTime; // 남은 표시 시간
    private Vector3 startLocalPos; // 텍스트의 초기 위치

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (feedbackText != null)
        {
            startLocalPos = feedbackText.transform.localPosition; // 원래 위치 기억

            Color color = feedbackText.color;
            color.a = 0f;
            feedbackText.color = color; // 시작할 때 투명하게 숨김
        }
    }

    // 지정된 번호에 맞는 경고 문구를 띄웁니다
    public void ShowWarning(int messageIndex)
    {
        if (feedbackText == null) return;

        // 1. 인덱스 검사 후 인스펙터에 설정된 텍스트 출력
        if (messageIndex >= 0 && messageIndex < warningMessages.Length)
        {
            feedbackText.text = warningMessages[messageIndex];
        }
        else
        {
            feedbackText.text = "알 수 없는 오류입니다.";
        }

        // 2. 이미 떠있는 상태에서 다시 호출되면 남은 시간만 최대치로 연장
        remainTime = displayTime;

        // 3. 코루틴이 안 돌고 있다면 새로 시작
        if (currentRoutine == null)
        {
            currentRoutine = StartCoroutine(FadeAndFloatRoutine());
        }
    }

    // 텍스트 페이드 및 부유 연출 코루틴
    private IEnumerator FadeAndFloatRoutine()
    {
        feedbackText.transform.localPosition = startLocalPos; // 위치 초기화
        Color color = feedbackText.color;

        // 빠르게 나타나기 (Fade In)
        while (color.a < 1f)
        {
            color.a += Time.deltaTime / fadeInTime;
            feedbackText.color = color;
            yield return null;
        }
        color.a = 1f;
        feedbackText.color = color;

        // 설정된 시간만큼 대기하며 위로 살짝씩 떠오르기
        while (remainTime > 0f)
        {
            remainTime -= Time.deltaTime; // 시간 차감
            feedbackText.transform.localPosition += Vector3.up * (floatSpeed * Time.deltaTime);
            yield return null;
        }

        // 부드럽게 사라지기 (Fade Out)
        while (color.a > 0f)
        {
            color.a -= Time.deltaTime / fadeOutTime;
            feedbackText.color = color;
            feedbackText.transform.localPosition += Vector3.up * (floatSpeed * Time.deltaTime); // 사라질 때도 계속 상승
            yield return null;
        }

        color.a = 0f;
        feedbackText.color = color;
        currentRoutine = null; // 코루틴 완전 종료 처리
    }
}