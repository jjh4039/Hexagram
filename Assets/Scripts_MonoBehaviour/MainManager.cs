using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainManager : MonoBehaviour
{
    public static MainManager Instance;

    [Header("BGM Settings")]
    public AudioClip mainBgmIntro;
    public AudioClip mainBgmLoop;

    [Header("Audio Settings")]
    public AudioClip introTypingSound;           // 인트로 텍스트 타이핑 사운드
    public AudioClip dialogueTypingSound;        // 말풍선 텍스트 타이핑 사운드

    [Header("Speech Bubbles (First Boss Cutscene)")]
    public CanvasGroup[] speechBubbles;
    public TextMeshProUGUI[] speechTexts;
    public float bubbleAnimDuration = 0.18f;
    public float bubbleStartScale = 0.98f;
    public float bubbleEndScale = 0.98f;
    public float bubbleMoveOffset = 5f;

    [Header("Ending Settings")]
    public TextMeshProUGUI endingText1;          // ★ 수정: 첫 번째 줄 엔딩 텍스트
    public TextMeshProUGUI endingText2;          // ★ 수정: 두 번째 줄 엔딩 텍스트
    public string titleSceneName = "Title";      // 데모가 끝나고 돌아갈 타이틀 씬 이름

    private bool isCutsceneActive = false;
    public bool IsCutsceneActive => isCutsceneActive;

    private Vector2[] bubbleOriginalPos;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 말풍선 초기 세팅
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

        // 씬 시작 시 메인 BGM 플레이
        if (SoundManager.instance != null && mainBgmLoop != null)
        {
            SoundManager.instance.PlayBGM(mainBgmLoop, mainBgmIntro, 1.5f);
        }

        // 엔딩 텍스트들 투명하게 초기화
        if (endingText1 != null)
        {
            Color c = endingText1.color;
            c.a = 0f;
            endingText1.color = c;
        }
        if (endingText2 != null)
        {
            Color c = endingText2.color;
            c.a = 0f;
            endingText2.color = c;
        }
    }

    // 석상에서 호출할 함수
    public void StartFirstBossCutscene(Transform targetStatue)
    {
        StartCoroutine(Co_PlayFirstBossCutscene(targetStatue));
    }

    private IEnumerator Co_PlayFirstBossCutscene(Transform targetStatue)
    {
        isCutsceneActive = true;

        // 1. 플레이어 조작 제한 및 카메라 줌인
        if (InputStateManager.Instance != null) InputStateManager.Instance.ChangeInputState(InputState.UI);
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            GameManager.instance.player.canControl = false;
            GameManager.instance.player.rigid.linearVelocity = Vector2.zero;
        }

        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.SetTarget(targetStatue);
            CameraFollow.instance.isCinematicFocus = true;
            CameraFollow.instance.isCinematicZoom = true;
        }

        if (CinematicManager.instance != null)
        {
            StartCoroutine(CinematicManager.instance.Co_FadeGameplayUI(false));
            CinematicManager.instance.StartCoroutine("Co_AnimateLetterBox", true);
        }

        // 엔딩 컷신 돌입 시 BGM을 서서히 끔
        if (SoundManager.instance != null) SoundManager.instance.StopBGM(2f);

        yield return new WaitForSeconds(1.5f);

        // 2. 대화 연출 시작 (텍스트 3개 연속 출력)
        int bubbleIndex = 0;
        yield return StartCoroutine(Co_AnimateBubble(bubbleIndex, "제 1구역 통과 완료.", true));
        yield return new WaitForSeconds(2.5f);

        // ★ 확정된 대사 (태그 마감 추가)
        ChangeBubbleText(bubbleIndex, "이례적인 생존은 <size=120%><color=#FF5C5C>데이터 지연</color></size>일 뿐입니다.");
        yield return new WaitForSeconds(3.0f);

        ChangeBubbleText(bubbleIndex, "환경을 재구성합니다. <size=120%><color=#5CE1E6>여름 프로토콜</size></color>");
        yield return new WaitForSeconds(3.5f);

        yield return StartCoroutine(Co_AnimateBubble(bubbleIndex, "", false));
        yield return new WaitForSeconds(1.0f);

        // 3. 화면 완전 암전 (TransitionManager 사용)
        if (TransitionManager.Instance != null)
        {
            yield return StartCoroutine(TransitionManager.Instance.Co_FadeToBlack(2.0f));
        }

        yield return new WaitForSeconds(1.0f); // 완전히 까매진 후 1초 정적

        // 4. 중앙 엔딩(인트로 폼) 텍스트 누적 출력
        if (endingText1 != null && endingText2 != null)
        {
            // 첫 번째 줄 출력
            endingText1.text = "프로토타입 버전 데모가 종료되었습니다.";
            yield return StartCoroutine(Co_FadeText(endingText1, 0f, 1f, 1.5f));
            yield return new WaitForSeconds(1f); // 첫 번째 줄이 켜진 상태로 잠시 대기

            // 두 번째 줄 출력
            endingText2.text = "플레이해주셔서 감사합니다.";
            yield return StartCoroutine(Co_FadeText(endingText2, 0f, 1f, 1.5f));
            yield return new WaitForSeconds(2.0f); // 두 줄 모두 켜진 상태로 여운 대기

            // 두 줄 동시에 페이드 아웃
            Coroutine fade1 = StartCoroutine(Co_FadeText(endingText1, 1f, 0f, 1.5f));
            Coroutine fade2 = StartCoroutine(Co_FadeText(endingText2, 1f, 0f, 1.5f));
            yield return fade1;
        }
        else
        {
            yield return new WaitForSeconds(3.0f); // 텍스트가 없더라도 여운 대기
        }

        // 5. 타이틀 씬으로 완전히 복귀
        if (TransitionManager.Instance != null)
        {
            // 이미 화면이 까맣기 때문에 fadeOut은 0초로 스킵, 타이틀에서 밝아질 때 fadeIn 1.5초
            TransitionManager.Instance.LoadScene(titleSceneName, 0f, 1.5f);
        }
        else
        {
            SceneManager.LoadScene(titleSceneName);
        }

        isCutsceneActive = false;
        // 맵 UI는 띄우지 않고 깔끔하게 종료!
    }

    // 엔딩 텍스트 페이드용 코루틴
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

    // --- 말풍선 애니메이션 ---

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

    // --- 타이핑 사운드 함수 ---

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