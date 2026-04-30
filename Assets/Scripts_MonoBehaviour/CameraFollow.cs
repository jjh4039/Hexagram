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
    [SerializeField] private float aimZoomMultiplier = 0.95f;      // 에임 시 화면 배율 설정
    [SerializeField] private float aimTransitionSpeed = 5f;

    [Header("Cinematic Settings")] 
    public bool isCinematicFocus = false;                          // 연출 중 마우스 추적 무시 플래그
    public bool isCinematicZoom = false;                           // 연출 중 줌 여부 플래그
    [SerializeField] private float cinematicZoomMultiplier = 0.8f; // 사망 시 화면 확대 배율
    [SerializeField] private float cinematicZoomSpeed = 2.0f;      // 줌 확대 진입 속도

    [Header("Shake Settings")] 
    [SerializeField] private float shakeDecaySpeed = 5f;
    [SerializeField] private float uiOffsetSmoothSpeed = 10f;
    [SerializeField] private float shakeFrequency = 35f;           // 변경: 진동 속도(주파수) 조절용 변수 추가

    private float _currentShakeDecay;                              // 현재 적용 중인 감쇠 속도

    private Vector3 _offset;
    private Vector3 _uiOffset;
    private Vector3 _currentUiOffset;
    private Vector3 _shakeOffset;
    private float _shakeTimer = 0f;
    private float _currentShakeMagnitude = 0f;
    private float _lastHitShakeTime = -1f;
    [SerializeField] private float hitShakeCooldown = 0.05f;

    private float _shakeSeedX;                                     // 변경: 펄린 노이즈 X축 시작점
    private float _shakeSeedY;                                     // 변경: 펄린 노이즈 Y축 시작점

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

        _currentShakeDecay = customDecay > 0f ? customDecay : shakeDecaySpeed;

        // 불가피한 수정: 매번 같은 패턴으로 흔들리지 않도록 무작위 시드값 배정
        _shakeSeedX = Random.Range(0f, 100f);
        _shakeSeedY = Random.Range(0f, 100f);

        _lastHitShakeTime = Time.time;
    }

    private void UpdateShake()
    {
        if (_shakeTimer > 0f)
        {
            _shakeTimer -= Time.unscaledDeltaTime;
            
            // 불가피한 수정: Random.Range 대신 Mathf.PerlinNoise 적용 (뚝뚝 끊기지 않는 부드러운 진동)
            // Time.unscaledTime을 사용하여 HitStop(시간 느려짐) 중에도 정상 속도로 흔들리게 처리
            float x = (Mathf.PerlinNoise(_shakeSeedX + Time.unscaledTime * shakeFrequency, 0f) - 0.5f) * 2f * _currentShakeMagnitude;
            float y = (Mathf.PerlinNoise(0f, _shakeSeedY + Time.unscaledTime * shakeFrequency) - 0.5f) * 2f * _currentShakeMagnitude;
            
            _shakeOffset = new Vector3(x, y, 0);
            
            _currentShakeMagnitude = Mathf.MoveTowards(_currentShakeMagnitude, 0f, Time.unscaledDeltaTime * _currentShakeDecay);
        }
        else 
        {
            _shakeOffset = Vector3.zero;
            _currentShakeMagnitude = 0f;
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        if (InputStateManager.Instance != null && InputStateManager.Instance.CurrentInputState == InputState.UI)
        {
            if (!isCinematicFocus) return;
        }

        if (Mouse.current == null && !isCinematicFocus) return;

        bool isRightClickDown = Mouse.current != null && Mouse.current.rightButton.isPressed;
        bool isGunEquipped = (weaponManager != null && weaponManager.CurrentWeapon == WeaponManager.WeaponType.Gun);
        bool isAiming = isRightClickDown && isGunEquipped;

        if ((isAiming && !_wasAiming) || (isCinematicZoom && !_wasAiming))
        {
            if (pixelCam == null || pixelCam.enabled)
            {
                _dynamicBaseOrthoSize = cam.orthographicSize;
            }
        }
        _wasAiming = isAiming || isCinematicZoom;

        if (pixelCam != null)
        {
            if (isAiming || isCinematicZoom)
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

        float targetOrthoSize = _dynamicBaseOrthoSize;
        if (isCinematicZoom) targetOrthoSize = _dynamicBaseOrthoSize * cinematicZoomMultiplier;
        else if (isAiming) targetOrthoSize = _dynamicBaseOrthoSize * aimZoomMultiplier;

        float currentZoomSpeed = isCinematicZoom ? cinematicZoomSpeed : aimTransitionSpeed;

        _currentInfluence = Mathf.Lerp(_currentInfluence, targetInfluence, aimTransitionSpeed * Time.deltaTime);
        _currentMaxOffset = Mathf.Lerp(_currentMaxOffset, targetMaxOffset, aimTransitionSpeed * Time.deltaTime);

        if (cam != null && cam.orthographic && (pixelCam == null || !pixelCam.enabled))
        {
            float dt = isCinematicZoom ? Time.unscaledDeltaTime : Time.deltaTime;
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrthoSize, currentZoomSpeed * dt);
        }

        _currentUiOffset = Vector3.Lerp(
            _currentUiOffset,
            _uiOffset,
            uiOffsetSmoothSpeed * Time.deltaTime
        );

        Vector3 targetPosition = player.position + _offset + _currentUiOffset;
        Vector3 finalOffset = Vector3.zero;

        bool isTrackingRealPlayer = (GameManager.instance && player == GameManager.instance.player.transform);

        if (isTrackingRealPlayer && !isCinematicFocus && Mouse.current != null)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            Vector3 directionToMouse = mouseWorldPos - player.position;
            directionToMouse.z = 0;

            finalOffset = directionToMouse * _currentInfluence;
            finalOffset = Vector3.ClampMagnitude(finalOffset, _currentMaxOffset);
        }

        targetPosition += finalOffset;

        Vector3 currentUnshakenPos = transform.position - _shakeOffset;

        float moveDt = isCinematicFocus ? Time.unscaledDeltaTime : Time.deltaTime;
        Vector3 smoothedPos = Vector3.Lerp(currentUnshakenPos, targetPosition, smoothSpeed * moveDt);

        UpdateShake();

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

    public void ResetTargetToPlayer()
    {
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            player = GameManager.instance.player.transform;
        }

        smoothSpeed = (_originalSmoothSpeed > 2) ? _originalSmoothSpeed : 5f;
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