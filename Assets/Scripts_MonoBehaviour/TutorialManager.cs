using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Scene Transition")] 
    public string nextSceneName = "Main";

    [Header("Screen Fade")] 
    public Image fadeImage;

    [Header("Intro Cutscene & Text Animator")]
    public float initialDelay = 2.0f;              
    public TextMeshProUGUI introText;

    [Header("Skip UI Settings")] 
    public GameObject skipNoticeContainer;
    public RectTransform skipFillMask;
    public float skipHoldTime = 2f;

    [Header("Speech Bubbles")] 
    public CanvasGroup[] speechBubbles;
    public TextMeshProUGUI[] speechTexts;
    public float bubbleAnimDuration = 0.18f;
    public float bubbleStartScale = 0.98f;
    public float bubbleEndScale = 0.98f;
    public float bubbleMoveOffset = 5f;

    [Header("Audio")] 
    public AudioClip introTypingSound;
    public AudioClip dialogueTypingSound;

    [Header("BGM")] 
    public AudioClip bgm1Loop;
    public AudioClip bgm2Intro;
    public AudioClip bgm2Loop;

    private bool isCutsceneActive;
    public bool IsCutsceneActive => isCutsceneActive;
    private bool canSkip;
    private float currentHoldTimer = 0f;
    private float maxMaskWidth;
    private float introTimer = 0f;

    private Vector2[] bubbleOriginalPos;

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

        if (SoundManager.instance != null && bgm1Loop != null)
        {
            SoundManager.instance.PlayBGM(bgm1Loop, null, 1.5f);
        }

        StartCoroutine(Co_PlayIntro());
    }

    // ★ 추가: 코루틴 누수 에러 방어
    private void OnDestroy()
    {
        StopAllCoroutines();
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

        bool shouldShowNotice = canSkip && ((introTimer <= 3.0f) || isSkipPressed);
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
        canSkip = false; 
        introTimer = 0f;

        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.ChangeInputState(InputState.UI);
        }

        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            GameManager.instance.player.canControl = false;
            GameManager.instance.player.rigid.linearVelocity = Vector2.zero;
        }

        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.isCinematicFocus = true;
            CameraFollow.Instance.SetInstantCustomZoom(2.4f);
        }

        if (CinematicManager.Instance != null)
        {
            StartCoroutine(CinematicManager.Instance.Co_FadeGameplayUI(false));
            CinematicManager.Instance.StartCoroutine("Co_AnimateLetterBox", true);
        }

        yield return new WaitForSeconds(initialDelay);

        introTimer = 0f; 
        canSkip = true; 
        if (skipNoticeContainer != null) skipNoticeContainer.SetActive(true);

        yield return new WaitForSeconds(1f);

        introText.text = "시스템 부팅 중...";
        yield return new WaitForSeconds(2.5f);

        introText.text = "불량품 식별 번호 Err-73...";
        yield return new WaitForSeconds(2.5f);

        introText.text = "최하급 '주사위 모듈' 탑재 확인,\n즉각 폐기 처리를 권장합니다.";
        yield return new WaitForSeconds(4f);

        yield return StartCoroutine(Co_PlaySpeechBubbles());

        canSkip = false;
        if (skipNoticeContainer != null) skipNoticeContainer.SetActive(false);

        yield return StartCoroutine(Co_FadeOutIntroText());

        if (SoundManager.instance != null) SoundManager.instance.StopBGM(2f);

        yield return StartCoroutine(Co_FadeOutScreen());

        if (CameraFollow.Instance != null)
        {
            yield return StartCoroutine(CameraFollow.Instance.Co_RestoreZoom(3f));
        }
        else
        {
            yield return new WaitForSeconds(3f);
        }

        if (CinematicManager.Instance != null)
        {
            StartCoroutine(CinematicManager.Instance.Co_FadeGameplayUI(true));
            CinematicManager.Instance.StartCoroutine("Co_AnimateLetterBox", false);
        }

        EndCutscene();
    }

    private IEnumerator Co_PlaySpeechBubbles()
    {
        yield return StartCoroutine(Co_AnimateBubble(0, "허, 또 불량품이야?", true));
        yield return new WaitForSeconds(1.5f);

        yield return StartCoroutine(Co_AnimateBubble(1, "그러면 그렇지, 다음이나 기다리자고.", true));
        yield return new WaitForSeconds(1.8f);

        Coroutine hide1 = StartCoroutine(Co_AnimateBubble(0, "", false));
        Coroutine hide2 = StartCoroutine(Co_AnimateBubble(1, "", false));
        yield return hide1;
        yield return hide2;
        yield return new WaitForSeconds(1.2f);

        yield return StartCoroutine(
            Co_AnimateBubble(0, "<size=130%>...<color=#5CE1E6>재검사 시험</color></size>은 어때?", true));
        yield return new WaitForSeconds(2.25f);

        yield return StartCoroutine(Co_AnimateBubble(1, "미쳤구나, <size=130%><color=#FF5C5C>주사위 모듈</color></size>인거 안보이냐?",
            true));
        yield return new WaitForSeconds(2.5f);

        StartCoroutine(Co_AnimateBubble(0, "", false));
        yield return new WaitForSeconds(0.2f);

        ChangeBubbleText(1, "<size=160%><color=#FF5C5C>당장 쓰레기통에 버려.</color></size>");
        yield return new WaitForSeconds(2f);

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

        if (SoundManager.instance != null) SoundManager.instance.StopBGM(0.5f);

        float timer = 0f;
        float textFadeDuration = 0.3f;

        float introStartAlpha = introText != null ? introText.color.a : 0f;
        float[] bubbleStartAlphas = new float[speechBubbles.Length];

        for (int i = 0; i < speechBubbles.Length; i++)
        {
            bubbleStartAlphas[i] = (speechBubbles[i] != null && speechBubbles[i].gameObject.activeSelf)
                ? speechBubbles[i].alpha
                : 0f;
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

        if (CameraFollow.Instance != null)
        {
            yield return StartCoroutine(CameraFollow.Instance.Co_RestoreZoom(3f));
        }
        else
        {
            yield return new WaitForSeconds(3f);
        }

        if (CinematicManager.Instance != null)
        {
            StartCoroutine(CinematicManager.Instance.Co_FadeGameplayUI(true));
            CinematicManager.Instance.StartCoroutine("Co_AnimateLetterBox", false);
        }

        EndCutscene();
    }

    private void EndCutscene()
    {
        isCutsceneActive = false;
        canSkip = false;

        if (skipNoticeContainer != null) skipNoticeContainer.SetActive(false);

        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.isCinematicFocus = false;
        }

        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.ChangeInputState(InputState.Normal);
        }

        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            GameManager.instance.player.canControl = true;
        }

        if (SoundManager.instance != null && bgm2Loop != null)
        {
            SoundManager.instance.PlayBGM(bgm2Loop, bgm2Intro, 1.5f);
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

    public void StartFinalCutscene(Transform statueTransform)
    {
        StartCoroutine(Co_PlayFinalCutscene(statueTransform));
    }

    private IEnumerator Co_PlayFinalCutscene(Transform statueTransform)
    {
        isCutsceneActive = true;
        canSkip = false;

        if (InputStateManager.Instance != null) InputStateManager.Instance.ChangeInputState(InputState.UI);
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            GameManager.instance.player.canControl = false;
            GameManager.instance.player.rigid.linearVelocity = Vector2.zero;
        }

        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.SetTarget(statueTransform);
            CameraFollow.Instance.isCinematicFocus = true;
            CameraFollow.Instance.isCinematicZoom = true;
        }

        if (CinematicManager.Instance != null)
        {
            StartCoroutine(CinematicManager.Instance.Co_FadeGameplayUI(false));
            CinematicManager.Instance.StartCoroutine("Co_AnimateLetterBox", true);
        }

        yield return new WaitForSeconds(1.5f);

        int finalBubbleIndex = 2;
        yield return StartCoroutine(Co_AnimateBubble(finalBubbleIndex, "최하급 주사위 모듈, Err-73_재검사", true));
        yield return new WaitForSeconds(2.5f);

        ChangeBubbleText(finalBubbleIndex, "예상 합격률은 <size=130%><color=#FF5C5C>0% 미만</size></color>입니다.");
        yield return new WaitForSeconds(2.5f);

        ChangeBubbleText(finalBubbleIndex, "<size=110%>응시하시겠습니까?</size>");
        yield return new WaitForSeconds(2f);

        yield return StartCoroutine(Co_AnimateBubble(finalBubbleIndex, "", false));
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(Co_AnimateBubble(finalBubbleIndex, "<size=120%>...</size>", true));
        yield return new WaitForSeconds(3f);

        ChangeBubbleText(finalBubbleIndex, "<color=#FF5C5C><size=130%>재검사 프로토콜, 가동합니다.</size></color>");
        yield return new WaitForSeconds(3f);

        yield return StartCoroutine(Co_AnimateBubble(finalBubbleIndex, "", false));
        yield return new WaitForSeconds(0.5f);

        if (SoundManager.instance != null) SoundManager.instance.StopBGM(1.5f);

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            float timer = 0f;
            float fadeDuration = 1.5f;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                fadeImage.color = new Color(0, 0, 0, timer / fadeDuration);
                yield return null;
            }

            fadeImage.color = Color.black;
        }

        yield return new WaitForSeconds(1.0f);

        yield return StartCoroutine(Co_AnimateBubble(finalBubbleIndex, "행운을 빕니다.", true));
        yield return new WaitForSeconds(2.5f);

        yield return StartCoroutine(Co_AnimateBubble(finalBubbleIndex, "", false));

        yield return new WaitForSeconds(1.0f);

        if (DataManager.instance != null && DataManager.instance.data != null)
        {
            DataManager.instance.data.isTutorialClear = true;
            DataManager.instance.SaveGame();
        }

        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.LoadScene(nextSceneName, 0f, 1.5f);
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}