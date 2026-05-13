using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Screen Fade")]
    public Image fadeImage;                        // 전체를 덮는 검은 화면

    [Header("Intro Cutscene & Text Animator")]
    public TextMeshProUGUI introText;              // 텍스트 애니메이터 연결 컴포넌트

    [Header("Skip UI Settings")]
    public GameObject skipNoticeContainer;         // 스킵 안내 UI 최상위 객체
    public RectTransform skipFillMask;             // 스킵 텍스트 마스크
    public float skipHoldTime = 2f;              // 스킵 처리에 필요한 시간

    [Header("Speech Bubbles")]
    public CanvasGroup[] speechBubbles;            // 말풍선 캔버스 그룹 배열
    public TextMeshProUGUI[] speechTexts;          // 말풍선 텍스트 컴포넌트 배열
    public float bubbleAnimDuration = 0.18f;       // 말풍선 페이드 및 크기 변화 시간
    public float bubbleStartScale = 0.9f;         // 말풍선 등장 시 초기 크기 비율
    public float bubbleEndScale = 0.95f;           // 말풍선 퇴장 시 최종 크기 비율

    [Header("Audio")]
    public AudioClip introTypingSound;             // 인트로 터미널 전용 사운드
    public AudioClip dialogueTypingSound;          // 대화 말풍선 전용 사운드

    private bool isCutsceneActive;                 // 현재 컷신 진행 여부
    private bool canSkip;                          // 스킵 가능 여부
    private float currentHoldTimer = 0f;           // 스킵 키 누적 시간
    private float maxMaskWidth;                    // 마스크의 최대 너비
    private float introTimer = 0f;                 // 인트로 진행 시간 측정용

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 1f);
        }

        if (skipFillMask != null)
        {
            maxMaskWidth = skipFillMask.sizeDelta.x;
            SetSkipFillAmount(0f); 
        }

        foreach (var bubble in speechBubbles)
        {
            if (bubble != null) bubble.gameObject.SetActive(false);
        }

        StartCoroutine(Co_PlayIntro());
    }

    private void Update()
    {
        if (!isCutsceneActive) return;

        introTimer += Time.deltaTime;

        bool isSkipPressed = false;
        
        if (canSkip && InputStateManager.Instance != null && InputStateManager.Instance.Actions != null)
        {
            isSkipPressed = InputStateManager.Instance.Actions.UI.CloseUI.IsPressed();
        }

        bool shouldShowNotice = canSkip && ((introTimer <= 1.5f) || isSkipPressed);
        if (skipNoticeContainer != null)
        {
            if (skipNoticeContainer.activeSelf != shouldShowNotice)
            {
                skipNoticeContainer.SetActive(shouldShowNotice);
            }
        }

        if (isSkipPressed)
        {
            currentHoldTimer += Time.deltaTime;
            
            float fillRatio = Mathf.Clamp01(currentHoldTimer / skipHoldTime);
            SetSkipFillAmount(fillRatio);

            if (currentHoldTimer >= skipHoldTime)
            {
                SkipCutscene();
            }
        }
        else
        {
            if (currentHoldTimer > 0f)
            {
                currentHoldTimer -= Time.deltaTime * 2f; 
                currentHoldTimer = Mathf.Max(0f, currentHoldTimer);
                
                float fillRatio = Mathf.Clamp01(currentHoldTimer / skipHoldTime);
                SetSkipFillAmount(fillRatio);
            }
        }
    }

    private void SetSkipFillAmount(float ratio)
    {
        if (skipFillMask != null)
        {
            skipFillMask.sizeDelta = new Vector2(maxMaskWidth * ratio, skipFillMask.sizeDelta.y);
        }
    }

    private IEnumerator Co_PlayIntro()
    {
        isCutsceneActive = true;
        canSkip = true;
        introTimer = 0f; 
        
        // 1. 플레이어 조종권 박탈 및 UI 모드 진입
        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.ChangeInputState(InputState.UI);
        }

        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            GameManager.instance.player.canControl = false;
            GameManager.instance.player.rigid.linearVelocity = Vector2.zero;
        }

        // 2. 컷신 연출 세팅 (카메라 통제 & 크게 확대, 게임 UI 숨김, 레터박스)
        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.isCinematicFocus = true;
            CameraFollow.instance.SetInstantCustomZoom(2.4f); 
        }

        if (CinematicManager.instance != null)
        {
            StartCoroutine(CinematicManager.instance.Co_FadeGameplayUI(false));
            CinematicManager.instance.StartCoroutine("Co_AnimateLetterBox", true);
        }

        if (skipNoticeContainer != null) skipNoticeContainer.SetActive(true);
        
        yield return new WaitForSeconds(1f);
            
        introText.text = "시스템 부팅 중...";
        yield return new WaitForSeconds(2.5f); 
        
        introText.text = "불량품 식별 번호 Err-73...";
        yield return new WaitForSeconds(2.5f);

        introText.text = "최하급 '주사위 모듈' 탑재 확인,\n즉각 폐기 처리를 권장합니다.";
        yield return new WaitForSeconds(5f);
        
        yield return StartCoroutine(Co_PlaySpeechBubbles());

        // 3. 인트로 텍스트가 사라지는 시점부터 스킵 완전 차단
        canSkip = false;
        if (skipNoticeContainer != null) skipNoticeContainer.SetActive(false);

        yield return StartCoroutine(Co_FadeOutIntroText());
        
        // 4. 검은 화면 걷히기 (줌인된 플레이어 + 레터박스가 보임)
        yield return StartCoroutine(Co_FadeOutScreen());

        // 5. 3초간의 정적 대기와 함께 카메라 서서히 원래 크기로 줌아웃
        if (CameraFollow.instance != null)
        {
            yield return StartCoroutine(CameraFollow.instance.Co_RestoreZoom(3f));
        }
        else
        {
            yield return new WaitForSeconds(3f);
        }

        // 6. 컷신 종료 전 UI 및 레터박스 복구 (yield return 제거하여 즉시 조작 가능하게 변경)
        if (CinematicManager.instance != null)
        {
            StartCoroutine(CinematicManager.instance.Co_FadeGameplayUI(true));
            CinematicManager.instance.StartCoroutine("Co_AnimateLetterBox", false);
        }

        EndCutscene();
    }

    private IEnumerator Co_PlaySpeechBubbles()
    {
        yield return StartCoroutine(Co_AnimateBubble(0, "허, 또 불량품이야?", true));
        yield return new WaitForSeconds(1.5f); 

        yield return StartCoroutine(Co_AnimateBubble(1, "그러면 그렇지, 다음이나 기다리자고.", true));
        yield return new WaitForSeconds(2.2f);

        Coroutine hide1 = StartCoroutine(Co_AnimateBubble(0, "", false));
        Coroutine hide2 = StartCoroutine(Co_AnimateBubble(1, "", false));
        yield return hide1;
        yield return hide2;
        yield return new WaitForSeconds(1.4f); 

        yield return StartCoroutine(Co_AnimateBubble(0, "<size=130%>...<color=#5CE1E6>재검사 시험</color></size>은 어때?", true));
        yield return new WaitForSeconds(2.25f);

        yield return StartCoroutine(Co_AnimateBubble(1, "미쳤구나, <size=130%><color=#FF5C5C>주사위 모듈</color></size>인거 안보이냐?", true));
        yield return new WaitForSeconds(2.5f);

        StartCoroutine(Co_AnimateBubble(0, "", false));
        yield return new WaitForSeconds(0.2f);
        
        ChangeBubbleText(1, "<size=120%><color=#FF5C5C>당장 쓰레기통에 버려.</color></size>");
        yield return new WaitForSeconds(3f); 

        yield return StartCoroutine(Co_AnimateBubble(1, "", false));
    }

    private IEnumerator Co_FadeOutIntroText()
    {
        if (introText != null)
        {
            float alpha = introText.color.a;
            while (alpha > 0f)
            {
                alpha -= Time.deltaTime * 0.4f; 
                introText.color = new Color(introText.color.r, introText.color.g, introText.color.b, alpha);
                yield return null;
            }
            
            introText.text = ""; 
            introText.color = new Color(introText.color.r, introText.color.g, introText.color.b, 1f); 
        }
    }

    private IEnumerator Co_FadeOutScreen()
    {
        if (fadeImage != null)
        {
            float timer = 0f;
            float fadeDuration = 3.5f;
            Color startColor = fadeImage.color;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / fadeDuration;
                fadeImage.color = new Color(startColor.r, startColor.g, startColor.b, 1f - progress);
                yield return null;
            }
            
            fadeImage.gameObject.SetActive(false);
        }
    }

    private IEnumerator Co_AnimateBubble(int index, string text, bool isShowing)
    {
        if (index >= speechBubbles.Length) yield break;

        CanvasGroup cg = speechBubbles[index];
        TextMeshProUGUI tmp = speechTexts[index];

        if (isShowing)
        {
            tmp.text = ""; 
            cg.gameObject.SetActive(true);
            cg.alpha = 0f;
            cg.transform.localScale = Vector3.one * bubbleStartScale; 
            tmp.text = text; 

            float timer = 0f;
            while (timer < bubbleAnimDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / bubbleAnimDuration;
                cg.alpha = progress;
                cg.transform.localScale = Vector3.Lerp(Vector3.one * bubbleStartScale, Vector3.one, progress);
                yield return null;
            }
            cg.alpha = 1f;
            cg.transform.localScale = Vector3.one;
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
                yield return null;
            }
            cg.gameObject.SetActive(false);
            tmp.text = ""; 
        }
    }

    private void ChangeBubbleText(int index, string text)
    {
        if (index >= speechTexts.Length) return;
        speechTexts[index].text = text;
    }

    public void SkipCutscene()
    {
        if (!isCutsceneActive || !canSkip) return;
        
        canSkip = false;
        StopAllCoroutines(); 
        
        StartCoroutine(Co_SkipFadeOut());
    }

    private IEnumerator Co_SkipFadeOut()
    {
        if (skipNoticeContainer != null) skipNoticeContainer.SetActive(false);

        float timer = 0f;
        float textFadeDuration = 0.3f; 

        float introStartAlpha = introText != null ? introText.color.a : 0f;
        float[] bubbleStartAlphas = new float[speechBubbles.Length];
        
        for (int i = 0; i < speechBubbles.Length; i++)
        {
            bubbleStartAlphas[i] = (speechBubbles[i] != null && speechBubbles[i].gameObject.activeSelf) ? speechBubbles[i].alpha : 0f;
        }

        while (timer < textFadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / textFadeDuration;

            if (introText != null && introStartAlpha > 0f)
            {
                float a = Mathf.Lerp(introStartAlpha, 0f, progress);
                introText.color = new Color(introText.color.r, introText.color.g, introText.color.b, a);
            }

            for (int i = 0; i < speechBubbles.Length; i++)
            {
                if (speechBubbles[i] != null && speechBubbles[i].gameObject.activeSelf && bubbleStartAlphas[i] > 0f)
                {
                    speechBubbles[i].alpha = Mathf.Lerp(bubbleStartAlphas[i], 0f, progress);
                }
            }

            yield return null;
        }

        if (introText != null)
        {
            introText.text = "";
            introText.color = new Color(introText.color.r, introText.color.g, introText.color.b, 1f);
        }

        foreach (var bubble in speechBubbles)
        {
            if (bubble != null) bubble.gameObject.SetActive(false);
        }

        if (fadeImage != null && fadeImage.gameObject.activeSelf)
        {
            timer = 0f;
            float screenFadeDuration = 0.5f; 
            float fadeScreenStartAlpha = fadeImage.color.a;

            while (timer < screenFadeDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / screenFadeDuration;
                float a = Mathf.Lerp(fadeScreenStartAlpha, 0f, progress);
                fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, a);
                yield return null;
            }

            fadeImage.gameObject.SetActive(false);
        }

        // 스킵 시에도 3초 대기하며 천천히 원래대로 줌아웃 (스킵 불가능 영역)
        if (CameraFollow.instance != null)
        {
            yield return StartCoroutine(CameraFollow.instance.Co_RestoreZoom(3f));
        }
        else
        {
            yield return new WaitForSeconds(3f);
        }

        // 레터박스 걷히기 시작과 동시에 조작 권한 반환을 위해 yield return 제거
        if (CinematicManager.instance != null)
        {
            StartCoroutine(CinematicManager.instance.Co_FadeGameplayUI(true));
            CinematicManager.instance.StartCoroutine("Co_AnimateLetterBox", false);
        }

        EndCutscene();
    }

    private void EndCutscene()
    {
        isCutsceneActive = false;
        canSkip = false;
        
        if (skipNoticeContainer != null) skipNoticeContainer.SetActive(false);

        // 카메라 통제 해제
        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.isCinematicFocus = false;
        }

        // 플레이어 조종권 부여 및 일반 모드 전환
        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.ChangeInputState(InputState.Normal);
        }

        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            GameManager.instance.player.canControl = true;
        }
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