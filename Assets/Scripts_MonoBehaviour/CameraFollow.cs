using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance;

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
    public bool isCinematicFocus;
    public bool isCinematicZoom;
    [SerializeField] private float cinematicZoomMultiplier = 0.8f;
    [SerializeField] private float cinematicZoomSpeed = 2.0f;

    [Header("Shake Settings")]
    [SerializeField] private float shakeDecaySpeed = 5f;
    [SerializeField] private float uiOffsetSmoothSpeed = 10f;
    [SerializeField] private float shakeFrequency = 35f;

    [Header("Bounds Settings")]
    public bool useBounds;
    private Bounds _currentBounds;
    public float boundsPadding = 1.5f;

    private bool _isCustomZooming;

    private float _currentShakeDecay;
    private Vector3 _offset;
    private Vector3 _uiOffset;
    private Vector3 _currentUiOffset;
    private Vector3 _shakeOffset;
    private float _shakeTimer;
    private float _currentShakeMagnitude;
    private float _lastHitShakeTime = -1f;
    [SerializeField] private float hitShakeCooldown = 0.05f;

    private float _shakeSeedX;
    private float _shakeSeedY;

    private float _originalSmoothSpeed;
    private float _currentInfluence;
    private float _currentMaxOffset;
    private Camera _cam;

    private bool _wasAiming;
    private float _dynamicBaseOrthoSize = 5f;

    private Vector3 _smoothedMouseOffset; 

    private void Awake()
    {
        Instance = this;
        _cam = GetComponent<Camera>();
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
        if (DataManager.instance && DataManager.instance.data.cameraShake == 0)
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
        if (!player) return;

        bool isUIState = (InputStateManager.Instance && InputStateManager.Instance.CurrentInputState == InputState.UI);
        
        if (isUIState && !isCinematicFocus) 
        {
            // ★ 수정: UI 창이 열렸을 때 마우스 추적을 끊고 원래 위치로 부드럽게 복귀시킵니다.
            _smoothedMouseOffset = Vector3.Lerp(_smoothedMouseOffset, Vector3.zero, aimTransitionSpeed * Time.unscaledDeltaTime);
        }

        if (Mouse.current == null && !isCinematicFocus) return;

        bool isRightClickDown = Mouse.current != null && Mouse.current.rightButton.isPressed;
        bool isGunEquipped = (weaponManager && weaponManager.CurrentWeapon == WeaponManager.WeaponType.Gun);
        
        // UI 상태가 아닐 때만 조준 활성화
        bool isAiming = isRightClickDown && isGunEquipped && !isUIState; 

        if ((isAiming && !_wasAiming) || (isCinematicZoom && !_wasAiming))
        {
            if (!pixelCam || pixelCam.enabled)
            {
                _dynamicBaseOrthoSize = _cam.orthographicSize;
            }
        }
        _wasAiming = isAiming || isCinematicZoom;

        if (pixelCam)
        {
            if (isAiming || isCinematicZoom || _isCustomZooming)
            {
                pixelCam.enabled = false;
            }
            else if (!pixelCam.enabled && Mathf.Abs(_cam.orthographicSize - _dynamicBaseOrthoSize) < 0.01f)
            {
                _cam.orthographicSize = _dynamicBaseOrthoSize;
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

        if (_cam != null && _cam.orthographic && (!pixelCam || !pixelCam.enabled))
        {
            if (!_isCustomZooming)
            {
                float dt = isCinematicZoom ? Time.unscaledDeltaTime : Time.deltaTime;
                _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, targetOrthoSize, currentZoomSpeed * dt);
            }
        }

        _currentUiOffset = Vector3.Lerp(
            _currentUiOffset,
            _uiOffset,
            uiOffsetSmoothSpeed * Time.deltaTime
        );

        Vector3 targetPosition = player.position + _offset + _currentUiOffset;
        Vector3 targetMouseOffset = Vector3.zero;

        bool isTrackingRealPlayer = (GameManager.instance && player == GameManager.instance.player.transform);

        // UI 상태가 아닐 때만 마우스 추적 활성화
        if (isTrackingRealPlayer && !isCinematicFocus && !isUIState && Mouse.current != null && _uiOffset == Vector3.zero)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = _cam.ScreenToWorldPoint(mouseScreenPos); 
            Vector3 directionToMouse = mouseWorldPos - player.position;
            directionToMouse.z = 0;

            targetMouseOffset = directionToMouse * _currentInfluence;
            targetMouseOffset = Vector3.ClampMagnitude(targetMouseOffset, _currentMaxOffset);
        }

        if (!isUIState)
        {
            _smoothedMouseOffset = Vector3.Lerp(_smoothedMouseOffset, targetMouseOffset, aimTransitionSpeed * Time.deltaTime);
        }

        if (useBounds && _cam != null)
        {
            float camHeight = _cam.orthographicSize;
            float camWidth = camHeight * _cam.aspect;

            float minX = _currentBounds.min.x + camWidth - boundsPadding;
            float maxX = _currentBounds.max.x - camWidth + boundsPadding;
            float minY = _currentBounds.min.y + camHeight - boundsPadding;
            float maxY = _currentBounds.max.y - camHeight + boundsPadding;

            if (minX > maxX) minX = maxX = _currentBounds.center.x;
            if (minY > maxY) minY = maxY = _currentBounds.center.y;

            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        }

        targetPosition += _smoothedMouseOffset; 

        Vector3 currentUnshakenPos = transform.position - _shakeOffset;
        float moveDt = isCinematicFocus ? Time.unscaledDeltaTime : Time.deltaTime;
        Vector3 smoothedPos = Vector3.Lerp(currentUnshakenPos, targetPosition, smoothSpeed * moveDt);

        UpdateShake();

        transform.position = smoothedPos + _shakeOffset;
    }

    public void SetTarget(Transform newTarget, float customSpeed = -1f)
    {
        player = newTarget;
        if (customSpeed > 0f) smoothSpeed = customSpeed;
    }

    public void ResetTargetToPlayer()
    {
        if (GameManager.instance && GameManager.instance.player)
        {
            player = GameManager.instance.player.transform;
        }
        smoothSpeed = (_originalSmoothSpeed > 2) ? _originalSmoothSpeed : 5f;
    }

    public void SnapToTarget()
    {
        if (!player) return;
        _currentUiOffset = _uiOffset;
        _smoothedMouseOffset = Vector3.zero; 
        Vector3 targetPos = player.position + _offset + _currentUiOffset;
        transform.position = targetPos;
        _shakeOffset = Vector3.zero;
        _shakeTimer = 0f;
        _currentShakeMagnitude = 0f;
    }

    public void SetUIOffset(Vector3 offset) { _uiOffset = offset; }
    public void ResetUIOffset() { _uiOffset = Vector3.zero; }
    public void SetCameraBounds(Bounds bounds) { _currentBounds = bounds; useBounds = true; }
    public void ClearCameraBounds() { useBounds = false; }

    public void SetInstantCustomZoom(float targetSize)
    {
        if (!_cam) return;

        _isCustomZooming = true;

        if (pixelCam && pixelCam.enabled)
        {
            pixelCam.enabled = false;
            _dynamicBaseOrthoSize = _cam.orthographicSize;
        }
        else if (_dynamicBaseOrthoSize <= 0f)
        {
            _dynamicBaseOrthoSize = _cam.orthographicSize;
        }

        _cam.orthographicSize = targetSize;
    }

    public IEnumerator Co_RestoreZoom(float duration)
    {
        if (!_cam) yield break;

        float startSize = _cam.orthographicSize;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            _cam.orthographicSize = Mathf.Lerp(startSize, _dynamicBaseOrthoSize, t);
            yield return null;
        }

        _cam.orthographicSize = _dynamicBaseOrthoSize;
        _isCustomZooming = false;
    }
}