using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow instance; // 어디서든 쉽게 부르기 위한 싱글톤

    [Header("Target")]
    public Transform player;

    [Header("Settings")]
    [Range(1f, 10f)] public float smoothSpeed = 5f;
    [Range(0.01f, 0.5f)] public float mouseInfluence = 0.05f;
    public float maxMouseOffset = 1.0f;

    private Vector3 offset;
    private Vector3 shakeOffset; // 흔들림 값을 저장할 변수

    private void Awake()
    {
        instance = this; // 나 자신을 전역 변수에 등록
    }

    void Start()
    {
        if (player == null) return;
        offset = transform.position - player.position;
        offset.x = 0;
        offset.y = 0;
    }

    // 외부(Gun)에서 이 함수를 부르면 흔들림 시작!
    public void Shake(float duration, float magnitude)
    {
        StopAllCoroutines(); // 기존 흔들림이 있다면 멈추고
        StartCoroutine(ShakeRoutine(duration, magnitude)); // 새로 흔들기
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // 랜덤한 위치로 흔들기 (원 안의 랜덤 좌표)
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            shakeOffset = new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 흔들림 끝 -> 원위치
        shakeOffset = Vector3.zero;
    }

    void LateUpdate()
    {
        if (player == null) return;
        if (Mouse.current == null) return;

        // --- [기존 로직: 따라가기] ---
        Vector3 targetPosition = player.position + offset;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector3 directionToMouse = mouseWorldPos - player.position;
        directionToMouse.z = 0;

        Vector3 finalOffset = directionToMouse * mouseInfluence;
        finalOffset = Vector3.ClampMagnitude(finalOffset, maxMouseOffset);
        targetPosition += finalOffset;

        Vector3 smoothedPos = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

        // --- [추가된 로직: 흔들림 더하기] ---
        // 부드럽게 이동한 위치(smoothedPos)에다가 + 흔들림(shakeOffset)을 더함!
        transform.position = smoothedPos + shakeOffset;
    }
}
