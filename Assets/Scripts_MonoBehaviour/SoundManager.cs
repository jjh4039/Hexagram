using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [System.Serializable]
    public struct BgmVolumeData
    {
        public AudioClip clip;
        [Range(0f, 2f)] public float volumeMultiplier; 
    }

    [Header("--- Settings ---")]
    [Range(0f, 1f)] public float masterVolume = 0.5f;

    [Range(0f, 1f)] public float bgmVolume = 0.2f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("--- Background Music ---")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource bgmLoopSource;

    [Header("--- BGM Volume Tuning ---")]
    public List<BgmVolumeData> bgmTuningList;
    private float currentBgmMultiplier = 1f; 

    [Header("--- SFX Pooling ---")]
    [SerializeField] private int poolSize = 20;

    private List<AudioSource> sfxPool;
    private Dictionary<AudioClip, float> lastPlayTimes = new Dictionary<AudioClip, float>();
    private const float MIN_SFX_INTERVAL = 0.05f;

    private Coroutine bgmFadeRoutine; 
    
    // ★ 추가: 페이드 진행 중 환경설정 슬라이더 충돌 방지용 플래그
    private bool isBgmFading = false; 

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

        Application.runInBackground = true;

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

        if (bgmFadeRoutine != null) StopCoroutine(bgmFadeRoutine); 
        isBgmFading = false; // 강제 중단 시 플래그 초기화

        bgmSource.Stop();
        if (bgmLoopSource != null) bgmLoopSource.Stop();

        currentBgmMultiplier = 1f; 
        foreach (var bgmData in bgmTuningList)
        {
            if (bgmData.clip == loopClip)
            {
                currentBgmMultiplier = bgmData.volumeMultiplier;
                break;
            }
        }

        float finalVolume = masterVolume * bgmVolume * currentBgmMultiplier;

        // ★ 수정: 인트로가 없을 때도 자연스럽게 페이드 인 적용
        if (introClip == null)
        {
            bgmSource.clip = loopClip;
            bgmSource.loop = true;
            
            if (fadeInDuration > 0f)
            {
                bgmSource.volume = 0f;
                bgmSource.Play();
                bgmFadeRoutine = StartCoroutine(Co_FadeInBGM(bgmSource, fadeInDuration));
            }
            else
            {
                bgmSource.volume = finalVolume;
                bgmSource.Play();
            }
        }
        // 인트로가 있을 때
        else
        {
            double introDuration = (double)introClip.samples / introClip.frequency;
            double startTime = AudioSettings.dspTime + 0.1;

            bgmSource.clip = introClip;
            bgmSource.loop = false;
            
            if (fadeInDuration > 0f)
            {
                bgmSource.volume = 0f; 
                bgmFadeRoutine = StartCoroutine(Co_FadeInBGM(bgmSource, fadeInDuration));
            }
            else
            {
                bgmSource.volume = finalVolume;
            }
            
            bgmSource.PlayScheduled(startTime);

            if (bgmLoopSource != null)
            {
                bgmLoopSource.clip = loopClip;
                bgmLoopSource.loop = true;
                bgmLoopSource.volume = finalVolume; 
                bgmLoopSource.PlayScheduled(startTime + introDuration);
            }
        }
    }

    public void StopBGM(float fadeOutDuration = 1f)
    {
        if (bgmFadeRoutine != null) StopCoroutine(bgmFadeRoutine); 
        bgmFadeRoutine = StartCoroutine(Co_FadeOutBGM(fadeOutDuration));
    }

    private IEnumerator Co_FadeInBGM(AudioSource source, float duration)
    {
        isBgmFading = true;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float currentTargetVolume = masterVolume * bgmVolume * currentBgmMultiplier;
            source.volume = Mathf.Lerp(0f, currentTargetVolume, timer / duration);
            yield return null;
        }
        source.volume = masterVolume * bgmVolume * currentBgmMultiplier;
        isBgmFading = false;
    }

    private IEnumerator Co_FadeOutBGM(float duration)
    {
        isBgmFading = true;
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
        isBgmFading = false;
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
        
        // ★ 핵심: 현재 페이드 인/아웃 중이라면, 코루틴이 알아서 처리하게 냅두고 강제로 덮어씌우지 않음 (충돌 방지)
        if (isBgmFading) return; 

        float finalVolume = masterVolume * bgmVolume * currentBgmMultiplier; 
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

        // ★ 핵심: 페이드 중이면 BGM 소스는 건드리지 않음 (SFX는 적용됨)
        if (isBgmFading) return; 

        float finalVolume = masterVolume * bgmVolume * currentBgmMultiplier; 
        if (bgmSource != null) bgmSource.volume = finalVolume;
        if (bgmLoopSource != null) bgmLoopSource.volume = finalVolume;
    }
}