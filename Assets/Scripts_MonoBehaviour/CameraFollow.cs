using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow instance;

    [Header("Target")]
    public Transform player;

    [Header("Settings")]
    [Range(1f, 10f)] public float smoothSpeed = 5f;
    [Range(0.01f, 0.5f)] public float mouseInfluence = 0.05f;
    public float maxMouseOffset = 1.0f;

    [Header("Shake Settings")]
    [SerializeField] private float shakeDecaySpeed = 5f; // 감쇠 속도 (과하지 않게)

    private Vector3 offset;
    private Vector3 shakeOffset;

    private float shakeTimer = 0f;
    private float currentShakeMagnitude = 0f;
    private float lastHitShakeTime = -1f;
    [SerializeField] private float hitShakeCooldown = 0.05f;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (player == null) return;

        offset = transform.position - player.position;
        offset.x = 0;
        offset.y = 0;
    }

    // 누적형 Shake
    public void HitShake(float duration, float magnitude)
    {
        if (Time.time - lastHitShakeTime < hitShakeCooldown)
            return;

        lastHitShakeTime = Time.time;

        shakeTimer = duration;
        currentShakeMagnitude = magnitude;
    }

    private void UpdateShake()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;

            float x = Random.Range(-1f, 1f) * currentShakeMagnitude;
            float y = Random.Range(-1f, 1f) * currentShakeMagnitude;

            shakeOffset = new Vector3(x, y, 0);

            // 점점 강도 줄이기 (자연스럽게 사라짐)
            currentShakeMagnitude = Mathf.Lerp(currentShakeMagnitude, 0f, Time.deltaTime * shakeDecaySpeed);
        }
        else
        {
            shakeOffset = Vector3.zero;
        }
    }

    void LateUpdate()
    {
        if (player == null) return;
        if (Mouse.current == null) return;

        UpdateShake();

        Vector3 targetPosition = player.position + offset;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector3 directionToMouse = mouseWorldPos - player.position;
        directionToMouse.z = 0;

        Vector3 finalOffset = directionToMouse * mouseInfluence;
        finalOffset = Vector3.ClampMagnitude(finalOffset, maxMouseOffset);
        targetPosition += finalOffset;

        Vector3 smoothedPos =
            Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

        transform.position = smoothedPos + shakeOffset;
    }

    public void SnapToTarget()
    {
        if (player == null) return;

        Vector3 targetPos = player.position + offset;
        transform.position = targetPos;

        shakeOffset = Vector3.zero;
        shakeTimer = 0f;
        currentShakeMagnitude = 0f;
    }
}
