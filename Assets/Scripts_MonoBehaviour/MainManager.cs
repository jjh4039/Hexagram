using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainManager : MonoBehaviour
{
    public static MainManager Instance;

    [Header("BGM Settings")] public AudioClip mainBgmIntro;
    public AudioClip mainBgmLoop;
    [SerializeField] private float bgmFadeInDuration = 1.5f;

    [Header("Audio Settings")] public AudioClip introTypingSound;
    public AudioClip dialogueTypingSound;

    [Header("Intro Guide Settings")] [SerializeField]
    private CanvasGroup introGuideGroup;

    [SerializeField] private float introGuideDelay = 1.0f;
    [SerializeField] private float introGuideFadeDuration = 1.0f;
    [SerializeField] private float introGuideDisplayDuration = 3.0f;

    [Header("Speech Bubbles (First Boss Cutscene)")]
    public CanvasGroup[] speechBubbles;

    public TextMeshProUGUI[] speechTexts;
    public float bubbleAnimDuration = 0.18f;
    public float bubbleStartScale = 0.98f;
    public float bubbleEndScale = 0.98f;
    public float bubbleMoveOffset = 5f;

    [Header("Ending Settings")] public TextMeshProUGUI endingDifficultyText;
    public TextMeshProUGUI endingText1;
    public TextMeshProUGUI endingText2;
    public TextMeshProUGUI testerText; 
    public string titleSceneName = "Title";

    private bool isCutsceneActive = false;
    public bool IsCutsceneActive => isCutsceneActive;

    private Vector2[] bubbleOriginalPos;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ShopBottomSlotHoverSystem.ResetHealKitState();

        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.ChangeGamePhase(GamePhase.SafeZone);
            InputStateManager.Instance.ChangeInputState(InputState.Normal);
        }

        bubbleOriginalPos = new Vector2[speechBubbles.Length];
        for (int i = 0; i < speechBubbles.Length; i++)
        {
            if (speechBubbles[i] != null)
            {
                RectTransform rectTransform = speechBubbles[i].GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    bubbleOriginalPos[i] = rectTransform.anchoredPosition;
                }

                speechBubbles[i].gameObject.SetActive(false);
            }
        }

        if (SoundManager.instance != null && mainBgmLoop != null)
        {
            SoundManager.instance.PlayBGM(mainBgmLoop, mainBgmIntro, bgmFadeInDuration);
        }

        // ★ 모든 엔딩 텍스트들을 시작할 때 투명하게(Alpha 0) 세팅
        SetTextAlphaZero(endingDifficultyText);
        SetTextAlphaZero(endingText1);
        SetTextAlphaZero(endingText2);
        SetTextAlphaZero(testerText);

        if (introGuideGroup != null)
        {
            introGuideGroup.alpha = 0f;
            introGuideGroup.gameObject.SetActive(false);
            StartCoroutine(Co_ShowIntroGuide());
        }
    }

    private void SetTextAlphaZero(TextMeshProUGUI text)
    {
        if (text != null)
        {
            Color c = text.color;
            c.a = 0f;
            text.color = c;
        }
    }

    private IEnumerator Co_ShowIntroGuide()
    {
        yield return new WaitForSeconds(introGuideDelay);

        introGuideGroup.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < introGuideFadeDuration)
        {
            elapsed += Time.deltaTime;
            introGuideGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / introGuideFadeDuration);
            yield return null;
        }

        introGuideGroup.alpha = 1f;

        yield return new WaitForSeconds(introGuideDisplayDuration);

        elapsed = 0f;
        while (elapsed < introGuideFadeDuration)
        {
            elapsed += Time.deltaTime;
            introGuideGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / introGuideFadeDuration);
            yield return null;
        }

        introGuideGroup.alpha = 0f;
        introGuideGroup.gameObject.SetActive(false);
    }

    public void StartFirstBossCutscene(Transform targetStatue)
    {
        StartCoroutine(Co_PlayFirstBossCutscene(targetStatue));
    }

    private IEnumerator Co_PlayFirstBossCutscene(Transform targetStatue)
    {
        isCutsceneActive = true;

        if (InputStateManager.Instance != null) InputStateManager.Instance.ChangeInputState(InputState.UI);
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            GameManager.instance.player.canControl = false;
            GameManager.instance.player.rigid.linearVelocity = Vector2.zero;
        }

        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.SetTarget(targetStatue);
            CameraFollow.Instance.isCinematicFocus = true;
            CameraFollow.Instance.isCinematicZoom = true;
        }

        if (CinematicManager.Instance != null)
        {
            StartCoroutine(CinematicManager.Instance.Co_FadeGameplayUI(false));
            CinematicManager.Instance.StartCoroutine("Co_AnimateLetterBox", true);
        }

        if (SoundManager.instance != null) SoundManager.instance.StopBGM(2f);

        yield return new WaitForSeconds(1.5f);

        int bubbleIndex = 0;
        yield return StartCoroutine(Co_AnimateBubble(bubbleIndex, "제 1구역 통과 완료.", true));
        yield return new WaitForSeconds(2.5f);

        ChangeBubbleText(bubbleIndex, "이례적인 생존은 <size=120%><color=#FF5C5C>데이터 지연</color></size>일 뿐입니다.");
        yield return new WaitForSeconds(3.0f);

        ChangeBubbleText(bubbleIndex, "환경을 재구성합니다. <size=120%><color=#5CE1E6>여름 프로토콜</size></color>");
        yield return new WaitForSeconds(3.5f);

        yield return StartCoroutine(Co_AnimateBubble(bubbleIndex, "", false));
        yield return new WaitForSeconds(1.0f);

        if (TransitionManager.Instance != null)
        {
            yield return StartCoroutine(TransitionManager.Instance.Co_FadeToBlack(2.0f));
        }

        yield return new WaitForSeconds(1.0f);

        // ★ 엔딩 크레딧 시퀀스 시작
        if (endingText1 != null && endingText2 != null)
        {
            // 1. 난이도 출력 (난이도가 0보다 클 때만)
            if (endingDifficultyText != null && DataManager.instance != null &&
                DataManager.instance.data.difficultyLevel > 0)
            {
                endingDifficultyText.text = $"난이도 {DataManager.instance.data.difficultyLevel}";
                yield return StartCoroutine(Co_FadeText(endingDifficultyText, 0f, 1f, 1.5f));
                yield return new WaitForSeconds(1f);
            }

            // 2. 기본 멘트 1
            endingText1.text = "프로토타입 버전 데모가 종료되었습니다.";
            yield return StartCoroutine(Co_FadeText(endingText1, 0f, 1f, 1.5f));
            yield return new WaitForSeconds(1f);

            // 3. 기본 멘트 2
            endingText2.text = "플레이해주셔서 감사합니다.";
            yield return StartCoroutine(Co_FadeText(endingText2, 0f, 1f, 1.5f));

            // 4. 테스터 이름 (지정된 텍스트와 서식을 코드에서 직접 주입)
            if (testerText != null)
            {
                testerText.text = "<size=120%><color=#FFDF75>Special Thanks (Beta Tester)</color></size>\n\n" +
                                  "<color=#FFF6D9>NOAH, AFEE, ! Sami, R2turnTrue, 여울, 엥, BMUTED, jm, 도오오오마뱀\n" +
                                  "태윤, taeyul, 그저 사람, Mulpas1022, JaeJitv2522, 공룡파티, 공허, Space_IX\n" +
                                  "살쾡이, 0y0, ㄱㄹㄸ, 춘수, 명이, 죠죠의 전설, 왕눈이, 뷁뒑쉙, zero, 화니화니\n" +
                                  "Hoshino, lua, Naul, 조정후, !싸이버, 이리아, hermit, 대수르, 아라키(콜스)\n" +
                                  "IdH, trivial, ㅇㅇ, Liato, 642ye, 최회장원조, FROG, 강지수123, F_iS_ma\n" +
                                  "우니, rosybell, 좀데, 사2퍼, wmmare, Asta, 최고는건랜스, gawrgura</color>";

                yield return StartCoroutine(Co_FadeText(testerText, 0f, 1f, 2.0f));
                yield return new WaitForSeconds(4.0f);
            }

            // 5. 떠있는 모든 텍스트들을 동시에 스르륵 페이드 아웃
            if (endingDifficultyText != null) StartCoroutine(Co_FadeText(endingDifficultyText, 1f, 0f, 1.5f));
            if (testerText != null) StartCoroutine(Co_FadeText(testerText, 1f, 0f, 1.5f));
            Coroutine fade1 = StartCoroutine(Co_FadeText(endingText1, 1f, 0f, 1.5f));
            Coroutine fade2 = StartCoroutine(Co_FadeText(endingText2, 1f, 0f, 1.5f));

            yield return fade1; // 페이드 아웃이 끝날 때까지 대기
        }
        else
        {
            yield return new WaitForSeconds(3.0f);
        }

        // 보상 정산 및 저장
        if (DataManager.instance != null && GameManager.instance != null)
        {
            int timeReward = Mathf.Min(Mathf.FloorToInt(GameManager.instance.currentPlayTime / 180f), 10);
            int dmgReward = GameManager.instance.totalDamageDealt / 800;
            int clearBonus = 5;
            DataManager.instance.data.totalGems += (timeReward + dmgReward + clearBonus);
            DataManager.instance.SaveGame();
        }

        // 타이틀로 이동
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.LoadScene(titleSceneName, 0f, 1.5f);
        }
        else
        {
            SceneManager.LoadScene(titleSceneName);
        }

        isCutsceneActive = false;
    }

    private IEnumerator Co_FadeText(TextMeshProUGUI text, float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        Color c = text.color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            text.color = c;
            yield return null;
        }

        c.a = endAlpha;
        text.color = c;
    }

    private IEnumerator Co_AnimateBubble(int index, string text, bool isShowing)
    {
        if (index >= speechBubbles.Length) yield break;

        CanvasGroup cg = speechBubbles[index];
        TextMeshProUGUI tmp = speechTexts[index];
        RectTransform rectTransform = cg.GetComponent<RectTransform>();

        Vector2 originalPos = bubbleOriginalPos[index];

        if (isShowing)
        {
            tmp.text = "";
            cg.gameObject.SetActive(true);
            cg.alpha = 0f;
            cg.transform.localScale = Vector3.one * bubbleStartScale;

            if (rectTransform != null) rectTransform.anchoredPosition = originalPos - new Vector2(0, bubbleMoveOffset);

            tmp.text = text;

            float timer = 0f;
            while (timer < bubbleAnimDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / bubbleAnimDuration;

                cg.alpha = progress;
                cg.transform.localScale = Vector3.Lerp(Vector3.one * bubbleStartScale, Vector3.one, progress);

                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = Vector2.Lerp(originalPos - new Vector2(0, bubbleMoveOffset),
                        originalPos, progress);
                }

                yield return null;
            }

            cg.alpha = 1f;
            cg.transform.localScale = Vector3.one;
            if (rectTransform != null) rectTransform.anchoredPosition = originalPos;
        }
        else
        {
            float timer = 0f;
            while (timer < bubbleAnimDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / bubbleAnimDuration;

                cg.alpha = 1f - progress;
                cg.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * bubbleEndScale, progress);

                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = Vector2.Lerp(originalPos,
                        originalPos - new Vector2(0, bubbleMoveOffset), progress);
                }

                yield return null;
            }

            cg.gameObject.SetActive(false);
            tmp.text = "";

            if (rectTransform != null) rectTransform.anchoredPosition = originalPos;
        }
    }

    private void ChangeBubbleText(int index, string text)
    {
        if (index >= speechTexts.Length) return;
        speechTexts[index].text = text;
    }

    public void PlayIntroTypingSound()
    {
        if (introTypingSound != null && SoundManager.instance != null)
        {
            SoundManager.instance.PlaySFX(introTypingSound, 0.4f, 0.15f);
        }
    }

    public void PlayDialogueTypingSound()
    {
        if (dialogueTypingSound != null && SoundManager.instance != null)
        {
            SoundManager.instance.PlaySFX(dialogueTypingSound, 0.3f, 0.15f);
        }
    }
}