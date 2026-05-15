using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header("--- Settings ---")]
    [Range(0f, 1f)] public float masterVolume = 0.5f;

    [Range(0f, 1f)] public float bgmVolume = 0.2f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("--- Background Music ---")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource bgmLoopSource; // 인스펙터에서 반드시 AudioSource를 추가로 할당해야 함

    [Header("--- SFX Pooling ---")]
    [SerializeField] private int poolSize = 20;

    private List<AudioSource> sfxPool;

    private Dictionary<AudioClip, float> lastPlayTimes = new Dictionary<AudioClip, float>(); // 동일 사운드 겹침 방지용
    private const float MIN_SFX_INTERVAL = 0.05f; // 동일 사운드 재생 최소 간격

    private Coroutine bgmFadeRoutine; // BGM 페이드 코루틴 추적용

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePool();
    }

    private void InitializePool()
    {
        sfxPool = new List<AudioSource>();
        GameObject poolRoot = new GameObject("SFX_Pool_Root");
        poolRoot.transform.parent = transform;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = new GameObject($"SFX_Source_{i}");
            go.transform.parent = poolRoot.transform;
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.gameObject.SetActive(false);
            sfxPool.Add(source);
        }
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1.0f, float pitchVariation = 0.1f)
    {
        if (clip == null) return;

        if (lastPlayTimes.TryGetValue(clip, out float lastTime))
        {
            if (Time.unscaledTime - lastTime < MIN_SFX_INTERVAL) return;
        }

        lastPlayTimes[clip] = Time.unscaledTime;

        AudioSource source = GetPooledSource();

        if (source != null)
        {
            source.gameObject.SetActive(true);
            source.Stop(); 

            source.volume = masterVolume * sfxVolume * volumeScale;
            source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            source.clip = clip;
            source.Play();

            StopCoroutine(nameof(DisableSourceRoutine));
            StartCoroutine(DisableSourceRoutine(source, clip.length));
        }
    }

    public void PlayBGM(AudioClip loopClip, AudioClip introClip = null, float fadeInDuration = 1f)
    {
        if (loopClip == null) return;

        if (bgmFadeRoutine != null) StopCoroutine(bgmFadeRoutine); // 기존 페이드 중단

        bgmSource.Stop();
        if (bgmLoopSource != null) bgmLoopSource.Stop();

        float finalVolume = masterVolume * bgmVolume;

        if (introClip == null)
        {
            bgmSource.clip = loopClip;
            bgmSource.loop = true;
            bgmSource.volume = finalVolume;
            bgmSource.Play();
        }
        else
        {
            double introDuration = (double)introClip.samples / introClip.frequency;
            double startTime = AudioSettings.dspTime + 0.1;

            bgmSource.clip = introClip;
            bgmSource.loop = false;
            bgmSource.volume = 0f; // 페이드 인을 위해 0부터 시작
            bgmSource.PlayScheduled(startTime);

            if (bgmLoopSource != null)
            {
                bgmLoopSource.clip = loopClip;
                bgmLoopSource.loop = true;
                bgmLoopSource.volume = finalVolume; // 루프는 타겟 볼륨으로 대기
                bgmLoopSource.PlayScheduled(startTime + introDuration);
            }

            bgmFadeRoutine = StartCoroutine(Co_FadeInBGM(bgmSource, finalVolume, fadeInDuration));
        }
    }

    public void StopBGM(float fadeOutDuration = 1f)
    {
        if (bgmFadeRoutine != null) StopCoroutine(bgmFadeRoutine); // 기존 페이드 중단
        bgmFadeRoutine = StartCoroutine(Co_FadeOutBGM(fadeOutDuration));
    }

    private IEnumerator Co_FadeInBGM(AudioSource source, float targetVolume, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; // 일시정지 중에도 페이드 적용
            source.volume = Mathf.Lerp(0f, targetVolume, timer / duration);
            yield return null;
        }
        source.volume = targetVolume;
    }

    private IEnumerator Co_FadeOutBGM(float duration)
    {
        float startVolume1 = bgmSource != null ? bgmSource.volume : 0f;
        float startVolume2 = bgmLoopSource != null ? bgmLoopSource.volume : 0f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = timer / duration;

            if (bgmSource != null) bgmSource.volume = Mathf.Lerp(startVolume1, 0f, progress);
            if (bgmLoopSource != null) bgmLoopSource.volume = Mathf.Lerp(startVolume2, 0f, progress);

            yield return null;
        }

        if (bgmSource != null)
        {
            bgmSource.volume = 0f;
            bgmSource.Stop();
        }
        
        if (bgmLoopSource != null)
        {
            bgmLoopSource.volume = 0f;
            bgmLoopSource.Stop();
        }
    }

    private AudioSource GetPooledSource()
    {
        foreach (var source in sfxPool)
        {
            if (!source.gameObject.activeSelf) return source;
        }

        AudioSource oldestSource = sfxPool[0];
        float longestTime = 0;

        foreach (var source in sfxPool)
        {
            if (source.time > longestTime)
            {
                longestTime = source.time;
                oldestSource = source;
            }
        }

        return oldestSource;
    }

    private IEnumerator DisableSourceRoutine(AudioSource source, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration + 0.1f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        source.gameObject.SetActive(false);
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = volume;
        float finalVolume = masterVolume * bgmVolume;
        
        if (bgmSource != null) bgmSource.volume = finalVolume;
        if (bgmLoopSource != null) bgmLoopSource.volume = finalVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = volume;
        float finalVolume = masterVolume * bgmVolume;

        if (bgmSource != null) bgmSource.volume = finalVolume;
        if (bgmLoopSource != null) bgmLoopSource.volume = finalVolume;
    }
}