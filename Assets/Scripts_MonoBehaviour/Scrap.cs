using UnityEngine;
using System.Collections;

public class Scrap : MonoBehaviour
{
    [Header("Settings")]
    public int value = 1;
    [SerializeField] private float acceleration = 40f;
    [SerializeField] private float initialSpeed = 2f;
    [SerializeField] private float rotateSpeed = 360f;
    [SerializeField] private float magnetDelay = 0.5f;

    [Header("Sound")]
    [SerializeField] private AudioClip sfxCollect; // ¡Ú [Ãß°¡] È¹µæ »ç¿îµå

    private Transform target;
    private bool isCollected = false;
    private float activationTime;

    private float currentSpeed = 0f;

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

            transform.position = Vector3.MoveTowards(transform.position, target.position, currentSpeed * Time.deltaTime);
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

        if (GameManager.instance != null)
        {
            GameManager.instance.AddScrap(value);
        }

        // ¡Ú [Ãß°¡] È¹µæ »ç¿îµå Àç»ý
        if (sfxCollect != null)
        {
            SoundManager.instance.PlaySFX(sfxCollect, 1f);
        }

        Debug.Log($"°íÃ¶ È¹µæ! (+{value})");
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