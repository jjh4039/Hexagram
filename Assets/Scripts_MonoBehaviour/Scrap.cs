using UnityEngine;
using System.Collections;

public class Scrap : MonoBehaviour
{
    [Header("Settings")] 
    public int value = 11;                                // 스크랩의 기본 가치
    [SerializeField] private float acceleration = 40f;   // 가속도
    [SerializeField] private float initialSpeed = 2f;    // 초기 속도
    [SerializeField] private float rotateSpeed = 720f;   // 회전 속도
    [SerializeField] private float magnetDelay = 0.5f;   // 자석 효과 지연 시간

    [Header("Sound")] 
    [SerializeField] private AudioClip sfxCollect;       // 획득 효과음

    private Transform target;                            // 따라갈 대상
    private bool isCollected = false;                    // 획득 여부
    private float activationTime;                        // 활성화 시간

    private float currentSpeed = 0f;                     // 현재 이동 속도

    // 외부(ScrapPile 등)에서 기본 가치를 설정할 때 사용하는 함수입니다.
    public void SetValue(int newValue)
    {
        value = newValue;
    }

    private void Start()
    {
        activationTime = Time.time + magnetDelay;
        StartCoroutine(PopRoutine());
    }

    private void Update()
    {
        if (isCollected) return;
        if (Time.time < activationTime) return;

        if (target != null)
        {
            if (currentSpeed == 0) currentSpeed = initialSpeed;
            currentSpeed += acceleration * Time.deltaTime;

            transform.position =
                Vector3.MoveTowards(transform.position, target.position, currentSpeed * Time.deltaTime);
            transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);

            float distance = Vector2.Distance(transform.position, target.position);
            if (distance < 0.2f)
            {
                Collect();
            }
        }
    }

    private void Collect()
    {
        isCollected = true;

        if (GameManager.instance)
        {
            // 기본 가치의 90% ~ 110% 사이로 무작위 오차 적용
            float randomMultiplier = Random.Range(0.9f, 1.1f);
            int finalValue = Mathf.Max(0, Mathf.RoundToInt(value * randomMultiplier));

            GameManager.instance.AddScrap(finalValue);
        }
        
        if (sfxCollect)
        {
            SoundManager.instance.PlaySFX(sfxCollect, 1f);
        }
        
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            target = other.transform;
        }
    }

    private IEnumerator PopRoutine()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(-0.8f, 0.8f), 0);

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float height = Mathf.Sin(t * Mathf.PI) * 0.5f;

            if (target == null || Time.time < activationTime)
            {
                transform.position = Vector3.Lerp(startPos, targetPos, t) + Vector3.up * height;
            }

            yield return null;
        }
    }
}