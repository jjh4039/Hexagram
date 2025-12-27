using UnityEngine;
using UnityEngine.InputSystem;

public class Gun : MonoBehaviour
{
    private WeaponManager weaponManager;
    private SpriteRenderer spriteRenderer;
    private Vector2 mouseWorldPos;

    [Header("Aiming Settings")]
    [SerializeField] private Transform muzzlePoint; // 총구 위치
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float laserLength = 50f;

    private void Awake()
    {
        weaponManager = GetComponentInParent<WeaponManager>();

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        // 매니저가 가지고 있는 단 하나의 인풋 액션에서 값을 읽음
        Vector2 mouseScreenPos = weaponManager.InputActions.Player.Look.ReadValue<Vector2>();
        mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        RotateWeapon();
        DrawLaser();
    }

    private void OnEnable()
    {
        if (lineRenderer != null) lineRenderer.enabled = true;
    }

    private void OnDisable()
    {
        if (lineRenderer != null) lineRenderer.enabled = false;
    }

    private void RotateWeapon()
    {
        Vector2 lookDir = mouseWorldPos - (Vector2)transform.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle);

        Vector3 localScale = Vector3.one * 0.8f;
        localScale.y = (angle > 90 || angle < -90) ? 0.8f : -0.8f;
        localScale.z = 1f;

        transform.localScale = localScale;
    }

    // 조준선
    private void DrawLaser()
    {
        if (lineRenderer == null || muzzlePoint == null) return;

        lineRenderer.SetPosition(0, muzzlePoint.position);

        Vector2 direction = transform.right;
        if (transform.localScale.x < 0 || transform.localScale.y < 0)
        {
            // direction = -transform.right; 
        }

        Vector2 endPoint = (Vector2)muzzlePoint.position + (direction * laserLength);

        lineRenderer.SetPosition(1, endPoint);
    }
}
