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
    [SerializeField] private float bgmFadeInDuration = 1.5f; // ★ 추가: BGM 페이드 인 시간

    [Header("Audio Settings")]
    public AudioClip introTypingSound;           
    public AudioClip dialogueTypingSound;        

    [Header("Speech Bubbles (First Boss Cutscene)")]
    public CanvasGroup[] speechBubbles;
    public TextMeshProUGUI[] speechTexts;
    public float bubbleAnimDuration = 0.18f;
    public float bubbleStartScale = 0.98f;
    public float bubbleEndScale = 0.98f;
    public float bubbleMoveOffset = 5f;

    [Header("Ending Settings")]
    public TextMeshProUGUI endingText1;          
    public TextMeshProUGUI endingText2;          
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

        // ★ 수정: 인트로와 루프가 있는 메인 BGM을 페이드 인하며 재생합니다.
        if (SoundManager.instance != null && mainBgmLoop != null)
        {
            SoundManager.instance.PlayBGM(mainBgmLoop, mainBgmIntro, bgmFadeInDuration);
        }

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

        if (endingText1 != null && endingText2 != null)
        {
            endingText1.text = "프로토타입 버전 데모가 종료되었습니다.";
            yield return StartCoroutine(Co_FadeText(endingText1, 0f, 1f, 1.5f));
            yield return new WaitForSeconds(1f); 

            endingText2.text = "플레이해주셔서 감사합니다.";
            yield return StartCoroutine(Co_FadeText(endingText2, 0f, 1f, 1.5f));
            yield return new WaitForSeconds(2.0f); 

            Coroutine fade1 = StartCoroutine(Co_FadeText(endingText1, 1f, 0f, 1.5f));
            Coroutine fade2 = StartCoroutine(Co_FadeText(endingText2, 1f, 0f, 1.5f));
            yield return fade1;
        }
        else
        {
            yield return new WaitForSeconds(3.0f); 
        }

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