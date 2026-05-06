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
    [SerializeField] private float aimZoomMultiplier = 0.95f;       
    [SerializeField] private float aimTransitionSpeed = 5f;

    [Header("Cinematic Settings")]
    public bool isCinematicFocus = false;                          
    public bool isCinematicZoom = false;                           
    [SerializeField] private float cinematicZoomMultiplier = 0.8f; 
    [SerializeField] private float cinematicZoomSpeed = 2.0f;      

    [Header("Shake Settings")]
    [SerializeField] private float shakeDecaySpeed = 5f;
    [SerializeField] private float uiOffsetSmoothSpeed = 10f;
    [SerializeField] private float shakeFrequency = 35f;           

    // ==========================================
    // [수정됨] 카메라 제한 및 여유 공간 설정
    // ==========================================
    [Header("Bounds Settings")]
    public bool useBounds = false;                                 
    private Bounds currentBounds;                                  
    
    [Tooltip("벽에 닿았을 때 마우스로 더 볼 수 있는 여유 거리")]
    public float boundsPadding = 1.5f;                             
    // ==========================================

    private float _currentShakeDecay;                              

    private Vector3 _offset;
    private Vector3 _uiOffset;
    private Vector3 _currentUiOffset;
    private Vector3 _shakeOffset;
    private float _shakeTimer = 0f;
    private float _currentShakeMagnitude = 0f;
    private float _lastHitShakeTime = -1f;
    [SerializeField] private float hitShakeCooldown = 0.05f;

    private float _shakeSeedX;                                     
    private float _shakeSeedY;                                     

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
        if (DataManager.instance != null && DataManager.instance.data.cameraShake == 0)
            return;

        if (magnitude <= _currentShakeMagnitude && Time.time - _lastHitShakeTime < hitShakeCooldown)
            return;

        _currentShakeMagnitude = Mathf.Max(_currentShakeMagnitude, magnitude);
        _shakeTimer = Mathf.Max(_shakeTimer, duration);

        _currentShakeDecay = customDecay > 0f ? customDecay : shakeDecaySpeed;

        _shakeSeedX = Random.Range(0f, 100f);
        _shakeSeedY = Random.Range(0f, 100f);

        _lastHitShakeTime = Time.time;
    }

    private void UpdateShake()
    {
        if (_shakeTimer > 0f)
        {
            _shakeTimer -= Time.unscaledDeltaTime;

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

        // ==========================================
        // [수정됨] 마우스 오프셋(finalOffset)을 적용하기 '전'에 플레이어 위치를 기준으로 제한
        // ==========================================
        if (useBounds && cam != null)
        {
            float camHeight = cam.orthographicSize;
            float camWidth = camHeight * cam.aspect;

            // padding을 추가하여 여유 구역 생성
            float minX = currentBounds.min.x + camWidth - boundsPadding;
            float maxX = currentBounds.max.x - camWidth + boundsPadding;
            float minY = currentBounds.min.y + camHeight - boundsPadding;
            float maxY = currentBounds.max.y - camHeight + boundsPadding;

            if (minX > maxX) minX = maxX = currentBounds.center.x; 
            if (minY > maxY) minY = maxY = currentBounds.center.y; 

            // 플레이어가 갈 수 있는 기본 목표 좌표를 먼저 제한합니다.
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        }

        // ==========================================
        // 그 후에 마우스 오프셋을 더해줍니다. 
        // 제한 구역에 걸려도 마우스를 움직이면 finalOffset만큼은 화면이 더 움직입니다!
        // ==========================================
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

    public void SetCameraBounds(Bounds bounds)
    {
        currentBounds = bounds;
        useBounds = true;
    }

    public void ClearCameraBounds()
    {
        useBounds = false;
    }
}