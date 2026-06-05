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
    [SerializeField] private bool fadeInOnStart = true;         
    [SerializeField] private float startFadeDuration = 1.5f;    

    private int _fadeToken = 0;                              // 중복 페이드 연출 캔슬용 고유 토큰
    private bool _isLoadingScene = false;                    // 씬 로딩 중 광클(중복 호출) 방어

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null); 
            DontDestroyOnLoad(gameObject); 
            
            if (fadeCanvasGroup != null)
            {
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
        if (fadeInOnStart && Instance == this)
        {
            StartCoroutine(Co_FadeToClear(startFadeDuration));
        }
    }

    public void LoadScene(string sceneName, float fadeOutDuration = 1.5f, float fadeInDuration = 1.5f)
    {
        if (_isLoadingScene) return;                         // 이미 로딩 중이면 중복 실행 차단
        _isLoadingScene = true;
        
        StartCoroutine(Co_LoadSceneSequence(sceneName, fadeOutDuration, fadeInDuration));
    }

    private IEnumerator Co_LoadSceneSequence(string sceneName, float fadeOutTime, float fadeInTime)
    {
        if (fadeOutTime > 0f)
        {
            yield return StartCoroutine(Co_FadeToBlack(fadeOutTime));
        }
        else
        {
            SetBlackScreen(true);
        }

        Time.timeScale = 1f;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false; 

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        yield return null;
        yield return null;
        yield return null;

        if (fadeInTime > 0f)
        {
            yield return StartCoroutine(Co_FadeToClear(fadeInTime));
        }
        else
        {
            SetBlackScreen(false);
        }

        _isLoadingScene = false;                             // 다음 씬 로딩을 위해 락 해제
    }

    public IEnumerator Co_FadeToBlack(float duration)
    {
        if (fadeCanvasGroup == null) yield break;

        _fadeToken++;                                        // 새 연출 시작을 알리는 토큰 갱신
        int myToken = _fadeToken;

        fadeCanvasGroup.blocksRaycasts = true; 
        float timer = 0f;
        float startAlpha = fadeCanvasGroup.alpha;            // 현재 알파값부터 시작하여 끊김 방지

        while (timer < duration)
        {
            if (_fadeToken != myToken) yield break;          // 새 페이드가 요청되었다면 즉시 정지(충돌 방어)

            timer += Mathf.Min(Time.unscaledDeltaTime, 0.1f); 
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, timer / duration);
            yield return null;
        }
        
        if (_fadeToken == myToken) fadeCanvasGroup.alpha = 1f;
    }

    public IEnumerator Co_FadeToClear(float duration)
    {
        if (fadeCanvasGroup == null) yield break;

        _fadeToken++;                                        // 새 연출 시작을 알리는 토큰 갱신
        int myToken = _fadeToken;

        float timer = 0f;
        float startAlpha = fadeCanvasGroup.alpha;            // 덮어씌워지더라도 자연스럽게 이어짐

        while (timer < duration)
        {
            if (_fadeToken != myToken) yield break;          // 충돌 방어

            timer += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / duration);
            yield return null;
        }
        
        if (_fadeToken == myToken) 
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false; 
        }
    }

    public void SetBlackScreen(bool isBlack)
    {
        if (fadeCanvasGroup == null) return;
        
        _fadeToken++;                                        // 즉시 설정 시에도 진행 중인 코루틴 캔슬
        
        fadeCanvasGroup.alpha = isBlack ? 1f : 0f;
        fadeCanvasGroup.blocksRaycasts = isBlack;
    }
}