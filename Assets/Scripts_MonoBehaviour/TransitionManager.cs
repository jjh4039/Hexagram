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

        // ★ 핵심 방어 로직: 씬을 넘어가기 직전 혹은 직후에 멈춰있던 시간(TimeScale)을 1로 강제 복구합니다.
        // 게임 오버 연출이나 일시정지 상태에서 씬을 넘어가도 다음 씬이 멈추지 않게 해줍니다.
        Time.timeScale = 1f;

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