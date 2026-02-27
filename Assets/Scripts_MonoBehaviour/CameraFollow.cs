using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow instance;

    [Header("Target")]
    public Transform player;

    [Header("Weapon Link (For Aiming)")]
    public WeaponManager weaponManager;

    [Header("Pixel Perfect Camera")]
    public Behaviour pixelCam;

    [Header("Settings")]
    [Range(1f, 10f)] public float smoothSpeed = 5f;
    [Range(0.01f, 0.5f)] public float mouseInfluence = 0.05f;
    public float maxMouseOffset = 1.0f;

    [Header("Aim & Zoom Settings")]
    [SerializeField] private float aimMouseInfluence = 0.15f;
    [SerializeField] private float aimMaxMouseOffset = 2.0f;

    [Tooltip("에임 시 화면을 몇 배로 만들 것인가? (0.95 = 5% 줌인)")]
    [SerializeField] private float aimZoomMultiplier = 0.95f;
    [SerializeField] private float aimTransitionSpeed = 5f;

    [Header("Shake Settings")]
    [SerializeField] private float shakeDecaySpeed = 5f;

    private Vector3 offset;
    private Vector3 shakeOffset;
    private float shakeTimer = 0f;
    private float currentShakeMagnitude = 0f;
    private float lastHitShakeTime = -1f;
    [SerializeField] private float hitShakeCooldown = 0.05f;

    private float currentInfluence;
    private float currentMaxOffset;
    private Camera cam;

    private bool wasAiming = false;
    private float dynamicBaseOrthoSize = 5f;

    private void Awake()
    {
        instance = this;
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (player == null) return;

        offset = transform.position - player.position;
        offset.x = 0;
        offset.y = 0;

        currentInfluence = mouseInfluence;
        currentMaxOffset = maxMouseOffset;
    }

    public void HitShake(float duration, float magnitude)
    {
        // 1. 새로 들어온 진동이 현재 진행 중인 진동보다 '약한데' 쿨다운이 안 지났다면 무시 (작은 진동이 연속으로 올 때 최적화)
        if (magnitude <= currentShakeMagnitude && Time.time - lastHitShakeTime < hitShakeCooldown)
            return;

        // 2. [핵심] 진동 강도는 기존 진동과 새 진동 중 '더 강한 것'을 채택 (작은 진동이 큰 진동을 씹어먹는 현상 방지)
        currentShakeMagnitude = Mathf.Max(currentShakeMagnitude, magnitude);

        // 3. 남은 시간도 더 긴 쪽을 채택하여 갑자기 멈추지 않게 함
        shakeTimer = Mathf.Max(shakeTimer, duration);

        lastHitShakeTime = Time.time;
    }

    private void UpdateShake()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float x = Random.Range(-1f, 1f) * currentShakeMagnitude;
            float y = Random.Range(-1f, 1f) * currentShakeMagnitude;
            shakeOffset = new Vector3(x, y, 0);
            currentShakeMagnitude = Mathf.Lerp(currentShakeMagnitude, 0f, Time.deltaTime * shakeDecaySpeed);
        }
        else shakeOffset = Vector3.zero;
    }

    void LateUpdate()
    {
        if (player == null) return;
        if (Mouse.current == null) return;

        UpdateShake();

        bool isRightClickDown = Mouse.current.rightButton.isPressed;
        bool isGunEquipped = (weaponManager != null && weaponManager.CurrentWeapon == WeaponManager.WeaponType.Gun);
        bool isAiming = isRightClickDown && isGunEquipped;

        // ★ [버그 해결!] 우클릭을 누르기 시작한 '첫 프레임'
        if (isAiming && !wasAiming)
        {
            // 픽셀 퍼펙트 카메라가 '켜져 있을 때만' (즉, 줌이 완전히 풀린 기본 상태일 때만) 진짜 사이즈 캡처!
            // 꺼져 있다면 줌 아웃 도중에 다시 누른 것이므로 기존의 값을 안전하게 유지합니다.
            if (pixelCam == null || pixelCam.enabled)
            {
                dynamicBaseOrthoSize = cam.orthographicSize;
            }
        }
        wasAiming = isAiming;

        if (pixelCam != null)
        {
            if (isAiming)
            {
                pixelCam.enabled = false;
            }
            else if (!pixelCam.enabled && Mathf.Abs(cam.orthographicSize - dynamicBaseOrthoSize) < 0.01f)
            {
                cam.orthographicSize = dynamicBaseOrthoSize;
                pixelCam.enabled = true;
            }
        }

        float targetInfluence = isAiming ? aimMouseInfluence : mouseInfluence;
        float targetMaxOffset = isAiming ? aimMaxMouseOffset : maxMouseOffset;

        float targetOrthoSize = isAiming ? (dynamicBaseOrthoSize * aimZoomMultiplier) : dynamicBaseOrthoSize;

        currentInfluence = Mathf.Lerp(currentInfluence, targetInfluence, aimTransitionSpeed * Time.deltaTime);
        currentMaxOffset = Mathf.Lerp(currentMaxOffset, targetMaxOffset, aimTransitionSpeed * Time.deltaTime);

        if (cam != null && cam.orthographic && (pixelCam == null || !pixelCam.enabled))
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrthoSize, aimTransitionSpeed * Time.deltaTime);
        }

        Vector3 targetPosition = player.position + offset;
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector3 directionToMouse = mouseWorldPos - player.position;
        directionToMouse.z = 0;

        Vector3 finalOffset = directionToMouse * currentInfluence;
        finalOffset = Vector3.ClampMagnitude(finalOffset, currentMaxOffset);
        targetPosition += finalOffset;

        Vector3 smoothedPos = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPos + shakeOffset;
    }

    public void SnapToTarget()
    {
        if (player == null) return;
        Vector3 targetPos = player.position + offset;
        transform.position = targetPos;
        shakeOffset = Vector3.zero;
        shakeTimer = 0f; currentShakeMagnitude = 0f;
    }
}