using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Intro Cutscene & Text Animator")]
    public TextMeshProUGUI introText;              // Text Animator 연결 컴포넌트

    [Header("Skip UI Settings")]
    public GameObject skipNoticeContainer;         // 스킵 안내 UI 최상위 객체
    public RectTransform skipFillMask;             // 스킵 텍스트 마스크
    public float skipHoldTime = 3f;                // 스킵 처리에 필요한 시간

    [Header("Speech Bubbles")]
    public CanvasGroup[] speechBubbles;            // 말풍선 캔버스 그룹 배열
    public TextMeshProUGUI[] speechTexts;          // 말풍선 텍스트 컴포넌트 배열
    public float bubbleAnimDuration = 0.15f;       // 말풍선 페이드 및 크기 변화 시간
    public float bubbleStartScale = 0.95f;         // 말풍선 등장 시 초기 크기 비율
    public float bubbleEndScale = 0.95f;           // 말풍선 퇴장 시 최종 크기 비율

    [Header("Audio")]
    public AudioClip introTypingSound;             // 인트로(터미널) 전용 사운드
    public AudioClip dialogueTypingSound;          // 대화(말풍선) 전용 사운드

    private bool isCutsceneActive;                 // 현재 컷신 진행 여부
    private float currentHoldTimer = 0f;           // 스킵 키 누적 시간
    private float maxMaskWidth;                    // 마스크의 최대 너비

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
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

        bool isSkipPressed = false;
        if (InputStateManager.Instance != null && InputStateManager.Instance.Actions != null)
        {
            isSkipPressed = InputStateManager.Instance.Actions.UI.CloseUI.IsPressed();
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
        
        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.ChangeInputState(InputState.UI);
        }

        if (skipNoticeContainer != null) skipNoticeContainer.SetActive(true);

        introText.text = "시스템 부팅 중...";
        yield return new WaitForSeconds(2.5f); 
        
        introText.text = "불량품 식별 번호 Err-73...";
        yield return new WaitForSeconds(2.5f);

        introText.text = "최하급 주사위 모듈 탑재 확인.\n즉각 폐기 처리를 권장합니다.";
        yield return new WaitForSeconds(5.5f);
        
        // 1. 말풍선 대화 진행
        yield return StartCoroutine(Co_PlaySpeechBubbles());

        // 2. ★ 대화 종료 후 터미널 텍스트가 서서히 사라짐 (누락됐던 부분)
        yield return StartCoroutine(Co_FadeOutIntroText());

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
        yield return new WaitForSeconds(1.8f); 

        yield return StartCoroutine(Co_AnimateBubble(0, "<size=130%>...<color=#5CE1E6>재검사 시험</color></size>은 어때?", true));
        yield return new WaitForSeconds(2.25f);

        yield return StartCoroutine(Co_AnimateBubble(1, "미쳤구나, <size=130%><color=#FF5C5C>주사위 모듈</color></size>인거 안보이냐?", true));
        yield return new WaitForSeconds(2.5f);

        StartCoroutine(Co_AnimateBubble(0, "", false));
        yield return new WaitForSeconds(0.2f);
        ChangeBubbleText(1, "<size=120%>당장 쓰레기통에 버려.</size>");
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
                // 숫자가 작을수록 더 천천히 사라짐 (1f -> 0.4f)
                alpha -= Time.deltaTime * 0.4f; 
                introText.color = new Color(introText.color.r, introText.color.g, introText.color.b, alpha);
                yield return null;
            }
            
            introText.text = ""; 
            introText.color = new Color(introText.color.r, introText.color.g, introText.color.b, 1f); 
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
        if (!isCutsceneActive) return;
        
        isCutsceneActive = false; 
        StopAllCoroutines();
        
        StartCoroutine(Co_SkipFadeOut());
    }

    private IEnumerator Co_SkipFadeOut()
    {
        if (skipNoticeContainer != null) skipNoticeContainer.SetActive(false);

        float timer = 0f;
        float fadeDuration = 0.5f;

        float introStartAlpha = introText != null ? introText.color.a : 0f;
        float[] bubbleStartAlphas = new float[speechBubbles.Length];
        
        for (int i = 0; i < speechBubbles.Length; i++)
        {
            bubbleStartAlphas[i] = (speechBubbles[i] != null && speechBubbles[i].gameObject.activeSelf) ? speechBubbles[i].alpha : 0f;
        }

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;

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

        EndCutscene();
    }

    private void EndCutscene()
    {
        isCutsceneActive = false;
        
        if (skipNoticeContainer != null) skipNoticeContainer.SetActive(false);

        if (InputStateManager.Instance != null)
        {
            InputStateManager.Instance.ChangeInputState(InputState.Normal);
        }
    }

    // 인트로 전용 사운드 (터미널용)
    public void PlayIntroTypingSound()
    {
        if (introTypingSound != null && SoundManager.instance != null)
        {
            SoundManager.instance.PlaySFX(introTypingSound, 0.4f, 0.15f);
        }
    }

    // 대화 전용 사운드 (말풍선용)
    public void PlayDialogueTypingSound()
    {
        if (dialogueTypingSound != null && SoundManager.instance != null)
        {
            SoundManager.instance.PlaySFX(dialogueTypingSound, 0.3f, 0.15f);
        }
    }
}