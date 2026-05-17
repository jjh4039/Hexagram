using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance;

    [Header("UI References")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Start Settings")]
    [SerializeField] private bool fadeInOnStart = true;         // ★ 추가: 게임 최초 실행 시 페이드 인 여부
    [SerializeField] private float startFadeDuration = 1.5f;    // ★ 추가: 최초 페이드 인 소요 시간

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null); // 에러 방지 (최상위 강제 이동)
            DontDestroyOnLoad(gameObject); 
            
            if (fadeCanvasGroup != null)
            {
                // ★ 수정: fadeInOnStart가 켜져 있으면 최초에 까만 화면으로 덮어둠
                if (fadeInOnStart)
                {
                    fadeCanvasGroup.alpha = 1f;
                    fadeCanvasGroup.blocksRaycasts = true;
                }
                else
                {
                    fadeCanvasGroup.alpha = 0f;
                    fadeCanvasGroup.blocksRaycasts = false;
                }
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // ★ 추가: 게임(또는 씬)이 처음 시작될 때 스르륵 밝아지도록 코루틴 실행
        if (fadeInOnStart && Instance == this)
        {
            StartCoroutine(Co_FadeToClear(startFadeDuration));
        }
    }

    public void LoadScene(string sceneName, float fadeOutDuration = 1.5f, float fadeInDuration = 1.5f)
    {
        StartCoroutine(Co_LoadSceneSequence(sceneName, fadeOutDuration, fadeInDuration));
    }

    private IEnumerator Co_LoadSceneSequence(string sceneName, float fadeOutTime, float fadeInTime)
    {
        // 1. 화면 까매지기
        if (fadeOutTime > 0f)
        {
            yield return StartCoroutine(Co_FadeToBlack(fadeOutTime));
        }
        else
        {
            SetBlackScreen(true);
        }

        // 2. 비동기 씬 로딩 시작
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false; 

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // 3. 씬 활성화 허용
        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        // 핵심 해결: 씬 로딩 직후 발생하는 거대한 프레임 드랍(렉)이 
        // 완전히 안정화될 때까지 안전빵으로 3프레임 정도 더 대기합니다.
        yield return null;
        yield return null;
        yield return null;

        // 4. 화면 밝아지기 (페이드 인)
        if (fadeInTime > 0f)
        {
            yield return StartCoroutine(Co_FadeToClear(fadeInTime));
        }
        else
        {
            SetBlackScreen(false);
        }
    }

    public IEnumerator Co_FadeToBlack(float duration)
    {
        if (fadeCanvasGroup == null) yield break;

        fadeCanvasGroup.blocksRaycasts = true; 
        float timer = 0f;
        float startAlpha = fadeCanvasGroup.alpha;

        while (timer < duration)
        {
            // Time Spike 방지. 한 프레임에 아무리 렉이 걸려도 최대 0.1초까지만 진행도를 올림
            timer += Mathf.Min(Time.unscaledDeltaTime, 0.1f); 
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, timer / duration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;
    }

    public IEnumerator Co_FadeToClear(float duration)
    {
        if (fadeCanvasGroup == null) yield break;

        float timer = 0f;
        float startAlpha = fadeCanvasGroup.alpha;

        while (timer < duration)
        {
            // Time Spike 방지
            timer += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / duration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false; 
    }

    public void SetBlackScreen(bool isBlack)
    {
        if (fadeCanvasGroup == null) return;
        fadeCanvasGroup.alpha = isBlack ? 1f : 0f;
        fadeCanvasGroup.blocksRaycasts = isBlack;
    }
}