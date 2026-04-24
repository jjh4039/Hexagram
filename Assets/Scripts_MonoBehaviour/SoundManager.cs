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

    [Header("--- SFX Pooling ---")]
    [SerializeField] private GameObject sfxSourcePrefab; // 껍데기 프리팹 (AudioSource 컴포넌트 달린 빈 오브젝트)
    [SerializeField] private int poolSize = 20;          // 동시에 낼 수 있는 소리 개수 (넉넉하게 20개)

    private List<AudioSource> sfxPool;

    private void Awake()
    {
        // 1. 싱글톤 패턴 (어디서든 SoundManager.instance 로 부르기 위해)
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바껴도 파괴되지 않음 (마을 -> 던전 유지)
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePool();
    }

    // 2. 오디오 소스 풀링 초기화
    private void InitializePool()
    {
        sfxPool = new List<AudioSource>();
        GameObject poolRoot = new GameObject("SFX_Pool_Root");
        poolRoot.transform.parent = transform;

        // 미리 20개를 생성해서 꺼둡니다.
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

    // ★ 3. 외부에서 효과음 재생할 때 부르는 함수
    // clip: 소리 파일, pitchVariation: 음 높낮이 랜덤 (타격감 핵심!)
    public void PlaySFX(AudioClip clip, float volumeScale = 1.0f, float pitchVariation = 0.1f)
    {
        if (clip == null) return;

        AudioSource source = GetPooledSource();

        if (source != null)
        {
            source.gameObject.SetActive(true);

            // 볼륨 계산: 마스터 볼륨 * 효과음 설정 볼륨 * 개별 소리 크기
            source.volume = masterVolume * sfxVolume * volumeScale;

            // 피치(음정) 랜덤 변화: 기관총 쏠 때 기계음을 덜하게 만듦
            source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);

            source.clip = clip;
            source.Play();

            // 재생이 끝나면 자동으로 풀에 반납 (비활성화)
            StartCoroutine(DisableSourceRoutine(source, clip.length));
        }
    }

    // 4. 배경음악 재생 함수
    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null) return;
        if (bgmSource.clip == clip) return; // 이미 같은 노래면 무시

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.volume = masterVolume * bgmVolume;
        bgmSource.Play();
    }

    // ★ 5. 환경설정에서 볼륨 조절할 때 호출할 함수들 (유지보수용)
    public void SetBGMVolume(float volume)
    {
        bgmVolume = volume;
        if (bgmSource != null) bgmSource.volume = masterVolume * bgmVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        // (옵션) 현재 재생 중인 효과음들도 소리를 줄이고 싶다면 여기서 sfxPool을 순회하며 조절
    }

    // --- 내부 로직 ---
    private AudioSource GetPooledSource()
    {
        // 놀고 있는(비활성화된) 친구를 찾아서 리턴
        foreach (var source in sfxPool)
        {
            if (!source.gameObject.activeSelf) return source;
        }

        // 만약 20개가 다 시끄럽게 떠들고 있다면? 
        // 1. 그냥 무시하거나 
        // 2. 제일 오래된 걸 뺏거나
        // 3. 풀을 늘리거나. (여기선 그냥 null 리턴해서 무시합니다. 20개면 충분함)
        return null;
    }

    private IEnumerator DisableSourceRoutine(AudioSource source, float duration)
    {
        // 클립 길이보다 0.1초 더 기다렸다가 끔 (안전장치)
        yield return new WaitForSeconds(duration + 0.1f);
        source.gameObject.SetActive(false);
    }
}