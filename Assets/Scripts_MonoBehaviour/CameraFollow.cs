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
    [SerializeField] private float uiOffsetSmoothSpeed = 10f; // 상점 열고 닫을 때
    
    private float _currentShakeDecay; // 현재 적용 중인 감쇠 속도

    private Vector3 _offset;
    private Vector3 _uiOffset;
    private Vector3 _currentUiOffset;
    private Vector3 _shakeOffset;
    private float _shakeTimer = 0f;
    private float _currentShakeMagnitude = 0f;
    private float _lastHitShakeTime = -1f;
    [SerializeField] private float hitShakeCooldown = 0.05f;

    private float _originalSmoothSpeed;
    private float _currentInfluence;
    private float _currentMaxOffset;
    private Camera cam;

    private bool _wasAiming = false;
    private float _dynamicBaseOrthoSize = 5f;

    private void Awake()
    {
        instance = this;
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        _currentInfluence = mouseInfluence;
        _currentMaxOffset = maxMouseOffset;
        _originalSmoothSpeed = smoothSpeed;

        if (player == null) return;

        _offset = transform.position - player.position;
        _offset.x = 0;
        _offset.y = 0;
    }

    public void HitShake(float duration, float magnitude, float customDecay = -1f)
    {
        if (magnitude <= _currentShakeMagnitude && Time.time - _lastHitShakeTime < hitShakeCooldown)
            return;

        _currentShakeMagnitude = Mathf.Max(_currentShakeMagnitude, magnitude);
        _shakeTimer = Mathf.Max(_shakeTimer, duration);

        // 커스텀 감쇠값이 들어오면 그것을 쓰고, 아니면 원래의 빠른 감쇠 속도를 씁니다.
        _currentShakeDecay = customDecay > 0f ? customDecay : shakeDecaySpeed;

        _lastHitShakeTime = Time.time;
    }

    private void UpdateShake()
    {
        if (_shakeTimer > 0f)
        {
            _shakeTimer -= Time.deltaTime;
            float x = Random.Range(-1f, 1f) * _currentShakeMagnitude;
            float y = Random.Range(-1f, 1f) * _currentShakeMagnitude;
            _shakeOffset = new Vector3(x, y, 0);

            // ★ [수정됨] shakeDecaySpeed 대신 currentShakeDecay 사용
            _currentShakeMagnitude = Mathf.Lerp(_currentShakeMagnitude, 0f, Time.deltaTime * _currentShakeDecay);
        }
        else _shakeOffset = Vector3.zero;
    }

    void LateUpdate()
{
    if (player == null) return;
    if (Mouse.current == null) return;

    UpdateShake();

    bool isRightClickDown = Mouse.current.rightButton.isPressed;
    bool isGunEquipped = (weaponManager != null && weaponManager.CurrentWeapon == WeaponManager.WeaponType.Gun);
    bool isAiming = isRightClickDown && isGunEquipped;

    if (isAiming && !_wasAiming)
    {
        if (pixelCam == null || pixelCam.enabled)
        {
            _dynamicBaseOrthoSize = cam.orthographicSize;
        }
    }
    _wasAiming = isAiming;

    if (pixelCam != null)
    {
        if (isAiming)
        {
            pixelCam.enabled = false;
        }
        else if (!pixelCam.enabled && Mathf.Abs(cam.orthographicSize - _dynamicBaseOrthoSize) < 0.01f)
        {
            cam.orthographicSize = _dynamicBaseOrthoSize;
            pixelCam.enabled = true;
        }
    }

    float targetInfluence = isAiming ? aimMouseInfluence : mouseInfluence;
    float targetMaxOffset = isAiming ? aimMaxMouseOffset : maxMouseOffset;
    float targetOrthoSize = isAiming ? (_dynamicBaseOrthoSize * aimZoomMultiplier) : _dynamicBaseOrthoSize;

    _currentInfluence = Mathf.Lerp(_currentInfluence, targetInfluence, aimTransitionSpeed * Time.deltaTime);
    _currentMaxOffset = Mathf.Lerp(_currentMaxOffset, targetMaxOffset, aimTransitionSpeed * Time.deltaTime);

    if (cam != null && cam.orthographic && (pixelCam == null || !pixelCam.enabled))
    {
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrthoSize, aimTransitionSpeed * Time.deltaTime);
    }

    _currentUiOffset = Vector3.Lerp(
        _currentUiOffset,
        _uiOffset,
        uiOffsetSmoothSpeed * Time.deltaTime
    );

    Vector3 targetPosition = player.position + _offset + _currentUiOffset;
    Vector3 finalOffset = Vector3.zero;

    bool isTrackingRealPlayer = (GameManager.instance && player == GameManager.instance.player.transform);

    if (isTrackingRealPlayer)
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector3 directionToMouse = mouseWorldPos - player.position;
        directionToMouse.z = 0;

        finalOffset = directionToMouse * _currentInfluence;
        finalOffset = Vector3.ClampMagnitude(finalOffset, _currentMaxOffset);
    }

    targetPosition += finalOffset;

    Vector3 smoothedPos = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    transform.position = smoothedPos + _shakeOffset;
}

    public void SetTarget(Transform newTarget, float customSpeed = -1f)
    {
        player = newTarget;

        if (customSpeed > 0f)
        {
            smoothSpeed = customSpeed;
        }
    }

    // ★ [수정됨] 타겟 복구 시 속도도 원래대로 복구
    public void ResetTargetToPlayer()
    {
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            player = GameManager.instance.player.transform;
        }

        smoothSpeed = (_originalSmoothSpeed > 2) ? _originalSmoothSpeed : 5f;
        // 보스 시작에서 에러가 생길 수 있어서 2로 하드코딩
    }

    public void SnapToTarget()
    {
        if (player == null) return;
        _currentUiOffset = _uiOffset;
        Vector3 targetPos = player.position + _offset + _currentUiOffset;
        transform.position = targetPos;
        _shakeOffset = Vector3.zero;
        _shakeTimer = 0f;
        _currentShakeMagnitude = 0f;
    }
    
    public void SetUIOffset(Vector3 offset)
    {
        _uiOffset = offset;
    }

    public void ResetUIOffset()
    {
        _uiOffset = Vector3.zero;
    }
}