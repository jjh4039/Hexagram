using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header("--- Settings ---")] [Range(0f, 1f)]
    public float masterVolume = 0.5f;

    [Range(0f, 1f)] public float bgmVolume = 0.2f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("--- Background Music ---")] [SerializeField]
    private AudioSource bgmSource;

    [Header("--- SFX Pooling ---")] [SerializeField]
    private int poolSize = 20;

    private List<AudioSource> sfxPool;

    // 동일 사운드 겹침 방지용 딕셔너리
    private Dictionary<AudioClip, float> lastPlayTimes = new Dictionary<AudioClip, float>();
    private const float MIN_SFX_INTERVAL = 0.05f; // 동일 사운드 최소 재생 간격

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

        // 1. 동일 사운드 쿨타임 체크 (소리 뭉침 및 먹먹함 방지)
        if (lastPlayTimes.TryGetValue(clip, out float lastTime))
        {
            if (Time.unscaledTime - lastTime < MIN_SFX_INTERVAL) return;
        }

        lastPlayTimes[clip] = Time.unscaledTime;

        // 2. 채널 가져오기 (비어있는게 없다면 가장 오래된 것을 뺏어옴)
        AudioSource source = GetPooledSource();

        if (source != null)
        {
            source.gameObject.SetActive(true);
            source.Stop(); // 재사용 시 이전 소리 정지

            source.volume = masterVolume * sfxVolume * volumeScale;
            source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            source.clip = clip;
            source.Play();

            // 코루틴 관리 최적화: 이미 실행 중인 비활성화 루틴이 꼬이지 않게 처리
            StopCoroutine(nameof(DisableSourceRoutine));
            StartCoroutine(DisableSourceRoutine(source, clip.length));
        }
    }

    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null) return;
        if (bgmSource.clip == clip) return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.volume = masterVolume * bgmVolume;
        bgmSource.Play();
    }

    private AudioSource GetPooledSource()
    {
        // 1. 비활성화된 채널 찾기
        foreach (var source in sfxPool)
        {
            if (!source.gameObject.activeSelf) return source;
        }

        // 2. 모든 채널이 사용 중이라면 가장 먼저 재생을 시작했던 채널을 강제로 재사용 (Voice Stealing)
        // 리스트의 첫 번째가 보통 가장 오래된 소리일 확률이 높음
        AudioSource oldestSource = sfxPool[0];
        float longestTime = 0;

        foreach (var source in sfxPool)
        {
            // 재생 시간이 가장 많이 경과한(남은 시간이 적은) 소리를 찾음
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
            // 일시정지 중에도 소리가 꺼져야 하므로 unscaledDeltaTime 사용
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        source.gameObject.SetActive(false);
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = volume;
        if (bgmSource != null) bgmSource.volume = masterVolume * bgmVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
    }


    public void SetMasterVolume(float volume)
    {
        masterVolume = volume;
        if (bgmSource != null) bgmSource.volume = masterVolume * bgmVolume;
    }
}