using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    [Header("Sequence Delays")]
    [SerializeField] private float delayA = 1.0f; // 초기 대기 시간
    [SerializeField] private float delayB = 0.5f; // 좌측 텍스트 간격
    [SerializeField] private float delayD = 0.3f; // 좌측 완료 후 우측 시작 전 대기
    [SerializeField] private float delayC = 0.5f; // 우측 텍스트 간격
    [SerializeField] private float delayE = 0.8f; // 우측 완료 후 정산 시작 전 대기
    [SerializeField] private float countDelay = 0.05f; // 보석 정산 속도 및 정산 사이 대기

    [Header("UI Objects (Left Texts)")]
    [SerializeField] private GameObject text0Obj; // 최상단 타이틀
    [SerializeField] private GameObject text1Obj; // 타임 라벨
    [SerializeField] private GameObject text2Obj; // 데미지 라벨
    [SerializeField] private GameObject text3Obj; // 보석 라벨

    [Header("UI Objects (Right Texts)")]
    [SerializeField] private GameObject textAObj; // 타임 보상치
    [SerializeField] private GameObject textBObj; // 데미지 보상치
    [SerializeField] private GameObject textCObj; // 합계 보상치
    [SerializeField] private GameObject spacePromptObj; // 안내 문구

    [Header("Text Components For Counting")]
    [SerializeField] private TextMeshProUGUI timeRewardText; // 타임 보석 숫자
    [SerializeField] private TextMeshProUGUI damageRewardText; // 데미지 보석 숫자
    [SerializeField] private TextMeshProUGUI totalGemText; // 합계 보석 숫자
    [SerializeField] private TextMeshProUGUI spacePromptText; // 안내 문구 컴포넌트

    [Header("Settlement Variables")]
    public int rewardFromTime = 3; // 타임 보상 목표치
    public int rewardFromDamage = 2; // 데미지 보상 목표치
    public int totalGainedReward = 0; // 누적되는 획득량
    public int currentOwnedGems = 3; // 보유 중인 보석량

    private bool isCalculationDone = false; // 입력 활성화 플래그

    private void OnEnable()
    {
        InitializeUI();
        StartCoroutine(Co_GameOverSequence());
    }

    private void Update()
    {
        if (!isCalculationDone) return;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            GoToTitle();
        }
    }

    private void InitializeUI()
    {
        isCalculationDone = false;
        totalGainedReward = 0;

        GameObject[] allObjs = { text0Obj, text1Obj, text2Obj, text3Obj, textAObj, textBObj, textCObj, spacePromptObj };
        foreach (var obj in allObjs) if (obj != null) obj.SetActive(false);

        UpdateRewardUI();
    }

    private IEnumerator Co_GameOverSequence()
    {
        yield return new WaitForSecondsRealtime(delayA);

        if (text0Obj != null) text0Obj.SetActive(true);
        yield return new WaitForSecondsRealtime(delayB);

        if (text1Obj != null) text1Obj.SetActive(true);
        yield return new WaitForSecondsRealtime(delayB);

        if (text2Obj != null) text2Obj.SetActive(true);
        yield return new WaitForSecondsRealtime(delayB);

        if (text3Obj != null) text3Obj.SetActive(true);

        yield return new WaitForSecondsRealtime(delayD); // 좌측 종료 후 대기

        if (textAObj != null) textAObj.SetActive(true);
        yield return new WaitForSecondsRealtime(delayC);

        if (textBObj != null) textBObj.SetActive(true);
        yield return new WaitForSecondsRealtime(delayC);

        if (textCObj != null) textCObj.SetActive(true);

        yield return new WaitForSecondsRealtime(delayE); // 우측 종료 후 대기

        yield return StartCoroutine(Co_CalculateRewards());

        if (spacePromptObj != null) spacePromptObj.SetActive(true);
        if (spacePromptText != null) StartCoroutine(Co_BlinkPromptText());

        isCalculationDone = true;
    }

    private IEnumerator Co_CalculateRewards()
    {
        while (rewardFromTime > 0)
        {
            rewardFromTime--;
            totalGainedReward++;
            currentOwnedGems++;
            UpdateRewardUI();
            yield return new WaitForSecondsRealtime(countDelay);
        }

        yield return new WaitForSecondsRealtime(countDelay); // 정산 사이 대기

        while (rewardFromDamage > 0)
        {
            rewardFromDamage--;
            totalGainedReward++;
            currentOwnedGems++;
            UpdateRewardUI();
            yield return new WaitForSecondsRealtime(countDelay);
        }
    }

    private void UpdateRewardUI()
    {
        if (timeRewardText != null) timeRewardText.text = $"+{rewardFromTime}";
        if (damageRewardText != null) damageRewardText.text = $"+{rewardFromDamage}";
        if (totalGemText != null) totalGemText.text = $"(+{totalGainedReward})";
    }

    private IEnumerator Co_BlinkPromptText()
    {
        // 불가피한 수정: 0과 1 사이를 완전 왕복하는 부드러운 숨쉬기 연출 적용
        while (true)
        {
            // 시간 기반 Cos 공식으로 0에서 1 사이를 부드럽게 오가는 T값 계산 (속도 2.5)
            float t = (Mathf.Cos(Time.unscaledTime * 2.5f) + 1f) * 0.5f;

            // SmoothStep을 적용하여 T값의 양 끝단 변화를 더 부드럽게 처리
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // 최종 알파값을 0(완전 투명)에서 1(완전 불투명) 사이로 보간
            float alpha = Mathf.Lerp(0f, 1f, smoothT);

            if (spacePromptText != null) spacePromptText.alpha = alpha;
            yield return null;
        }
    }

    private void GoToTitle()
    {
        Debug.Log("타이틀로 이동");
        // Time.timeScale = 1f; // 시간 정지 해제
        // SceneManager.LoadScene("TitleScene");
    }
}