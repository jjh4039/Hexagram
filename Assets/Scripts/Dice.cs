using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChocDino.UIFX.Editor;

public class Dice : MonoBehaviour
{
    [Header("Color Palette")]
    [SerializeField] private Color32[] diceColors; // 0~5: 각 주사위 색상, 6: 대기 색상

    [Header("Timers")]
    [SerializeField] private float diceDuration = 10f; // 주사위 효과 유지 시간
    [SerializeField] private float rollWaitTime = 5f;  // 다음 굴림까지 대기 시간
    private float currentTimer;
    private bool isEffectActive;

    [Header("UI - Dice Module (Left-Top)")]
    [SerializeField] private Image diceIcon;          // 주사위 눈금 이미지 (1~6)
    [SerializeField] private Image diceGaugeFill;     // Radial Fill (시계방향 게이지)
    [SerializeField] private UIFXGlow moduleGlow;     // 구매하신 Glow Filter 에셋
    [SerializeField] private TextMeshProUGUI diceStatText; // "ATK +10%" 등 짧은 수치

    [Header("UI - Head 연출 (World Space)")]
    [SerializeField] private GameObject headDiceObj;  // 캐릭터 머리 위 UI 부모
    [SerializeField] private Image headDiceIcon;      // 머리 위 주사위 아이콘
    [SerializeField] private Animator headDiceAnim;   // 굴림 애니메이션 (회색 주사위 뱅글뱅글)

    private int currentDiceValue;

    private void Awake()
    {
        ResetUI();
    }

    private void Update()
    {
        UpdateTimer();
    }

    private void UpdateTimer()
    {
        currentTimer += Time.deltaTime;
        float targetTime = isEffectActive ? diceDuration : rollWaitTime;

        // Radial Fill 게이지 업데이트 (1 -> 0으로 줄어듬)
        if (diceGaugeFill != null)
        {
            diceGaugeFill.fillAmount = 1f - (currentTimer / targetTime);
        }

        // 타이머 종료 시 상태 전환
        if (currentTimer >= targetTime)
        {
            if (isEffectActive) StartCoroutine(PrepareNextRoll());
            else RollDice();
        }
    }

    private void RollDice()
    {
        currentTimer = 0;
        isEffectActive = true;
        currentDiceValue = Random.Range(0, 6);

        // 1. 좌상단 UI 업데이트
        diceIcon.sprite = GetDiceSprite(currentDiceValue); // 스프라이트 교체 로직 필요
        diceStatText.text = GetStatShortText(currentDiceValue);

        // 2. UIFX - Glow 적용 (주사위 색상에 맞춰 빛나기)
        moduleGlow.glowColor = diceColors[currentDiceValue];
        moduleGlow.enabled = true;

        // 3. 머리 위 연출 (당첨!)
        StartCoroutine(ShowHeadResult());
    }

    private IEnumerator PrepareNextRoll()
    {
        isEffectActive = false;
        currentTimer = 0;
        moduleGlow.enabled = false;
        diceStatText.text = ""; // 텍스트 비우기

        // 대시 상태나 기본 대기 색상으로 변경
        diceIcon.color = diceColors[6];
        yield return null;
    }

    private IEnumerator ShowHeadResult()
    {
        headDiceObj.SetActive(true);
        headDiceAnim.SetTrigger("OnRoll"); // 회색 주사위 뱅글뱅글
        yield return new WaitForSeconds(0.5f); // 0.5초간 굴림 연출

        headDiceAnim.enabled = false; // 애니 멈추고 결과 출력
        headDiceIcon.sprite = GetDiceSprite(currentDiceValue);
        headDiceIcon.color = diceColors[currentDiceValue];

        // 쫀득한 스케일 연출 (Pop!)
        headDiceIcon.transform.localScale = Vector3.one * 1.5f;
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 5f;
            headDiceIcon.transform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, t);
            yield return null;
        }

        yield return new WaitForSeconds(1.5f);
        headDiceObj.SetActive(false);
    }

    // 헬퍼 함수: 주사위 결과에 따른 짧은 텍스트 반환
    private string GetStatShortText(int val)
    {
        return val switch
        {
            0 => "ATK +10%",   // Blood-Rush
            1 => "DMG x2",     // Twin-Strike
            2 => "PERM ATK",   // Solar
            3 => "HEAL/SHIELD",// Life-Shell
            4 => "SPD +50%",   // Slip-Stream
            5 => "AMMO x6",    // Overdrive
            _ => ""
        };
    }

    private Sprite GetDiceSprite(int val) { /* 주사위 스프라이트 배열에서 가져오는 로직 */ return null; }

    private void ResetUI()
    {
        isEffectActive = false;
        currentTimer = 0;
        moduleGlow.enabled = false;
        headDiceObj.SetActive(false);
    }
}