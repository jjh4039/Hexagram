using UnityEngine;
using UnityEngine.InputSystem;

public class Gun : MonoBehaviour
{
    private WeaponManager weaponManager;
    private SpriteRenderer spriteRenderer;
    private Vector2 mouseWorldPos;

    [Header("Aiming Settings")]
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float laserLength = 50f;

    [Header("Shooting Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float fireRate = 0.125f;
    private float nextFireTime = 0f;

    [Header("Recoil Settings")]
    [SerializeField] private float playerKnockbackForce = 3f;
    [SerializeField] private float gunRecoilDistance = 0.2f;
    [SerializeField] private float gunRecoilDuration = 0.1f;

    [Header("VFX Settings")]
    [SerializeField] private ParticleSystem muzzleFlashEffect;
    [SerializeField] private float shakeDuration = 0.1f;
    [SerializeField] private float shakeMagnitude = 0.2f;

    // ★ [추가] 투명화 속도
    [SerializeField] private float fadeSpeed = 10f;

    private void Awake()
    {
        weaponManager = GetComponentInParent<WeaponManager>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        Vector2 mouseScreenPos = weaponManager.InputActions.Player.Look.ReadValue<Vector2>();
        mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        RotateWeapon();
        DrawLaser();

        // ★ [추가] 충전 중이면 투명해지기 로직
        HandleChargingVisuals();
    }

    private void HandleChargingVisuals()
    {
        if (GameManager.instance.player == null) return;

        bool isCharging = GameManager.instance.player.isCharging;

        // 1. 레이저 끄기/켜기
        if (lineRenderer != null) lineRenderer.enabled = !isCharging;

        // 2. 투명도 조절 (충전 중이면 0, 아니면 1)
        float targetAlpha = isCharging ? 0f : 1f;
        Color currentColor = spriteRenderer.color;

        // 부드럽게 변환 (Lerp)
        float newAlpha = Mathf.Lerp(currentColor.a, targetAlpha, Time.deltaTime * fadeSpeed);
        spriteRenderer.color = new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha);
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (weaponManager.IsSwapping) return;
        if (weaponManager.CurrentWeapon != WeaponManager.WeaponType.Gun) return;

        // ★ [추가] 충전 중이면 발사 불가!
        if (GameManager.instance.player.isCharging) return;

        if (Time.time < nextFireTime) return;

        if (GameManager.instance.stats.currentAmmo >= 100)
        {
            GameManager.instance.stats.currentAmmo -= 100;
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
        else
        {
            Debug.Log("탄약 부족!");
        }
    }

    // ... (Shoot, Recoil, RotateWeapon 등 나머지 코드는 기존 유지) ...
    private void Shoot()
    {
        if (bulletPrefab == null || muzzlePoint == null) return;
        Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
        Recoil();
        if (muzzleFlashEffect != null) muzzleFlashEffect.Play();
        if (CameraFollow.instance != null) CameraFollow.instance.Shake(shakeDuration, shakeMagnitude);
    }

    private void OnEnable()
    {
        if (lineRenderer != null) lineRenderer.enabled = true;
        if (weaponManager?.InputActions != null)
            weaponManager.InputActions.Player.Attack.performed += OnAttack;
    }

    private void OnDisable()
    {
        if (lineRenderer != null) lineRenderer.enabled = false;
        if (weaponManager?.InputActions != null)
            weaponManager.InputActions.Player.Attack.performed -= OnAttack;
    }

    private void Recoil()
    {
        StopCoroutine("VisualRecoilRoutine"); StartCoroutine("VisualRecoilRoutine");
        StopCoroutine("KnockbackRoutine"); StartCoroutine("KnockbackRoutine");
    }

    private System.Collections.IEnumerator KnockbackRoutine()
    {
        if (GameManager.instance.player != null)
        {
            var player = GameManager.instance.player;
            player.isAttacking = true;
            player.rigid.AddForce(-transform.right * playerKnockbackForce, ForceMode2D.Impulse);
            yield return new WaitForSeconds(0.1f);
            player.rigid.linearVelocity = Vector2.zero;
            player.isAttacking = false;
        }
    }

    private System.Collections.IEnumerator VisualRecoilRoutine()
    {
        Vector3 originalPos = new Vector3(0.05f, 0, 0);
        Vector3 recoilPos = originalPos - (Vector3.right * gunRecoilDistance);
        transform.localPosition = recoilPos;
        float elapsed = 0f;
        while (elapsed < gunRecoilDuration)
        {
            elapsed += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(recoilPos, originalPos, elapsed / gunRecoilDuration);
            yield return null;
        }
        transform.localPosition = originalPos;
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

    private void DrawLaser()
    {
        if (lineRenderer == null || muzzlePoint == null) return;

        // ★ 충전 중이면 레이저 그리지 않음 (혹시 몰라 이중 체크)
        if (GameManager.instance.player.isCharging) return;

        lineRenderer.SetPosition(0, muzzlePoint.position);
        Vector2 endPoint = (Vector2)muzzlePoint.position + ((Vector2)transform.right * laserLength);
        lineRenderer.SetPosition(1, endPoint);
    }
}