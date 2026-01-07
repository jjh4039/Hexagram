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
    // ★ [삭제됨] muzzleFlashEffect 변수 삭제! 
    [SerializeField] private float shakeDuration = 0.1f;
    [SerializeField] private float shakeMagnitude = 0.2f;

    [SerializeField] private float fadeSpeed = 10f;

    // ★ [추가] 현재 적용된 색상과 재질을 기억할 변수
    private Color currentBulletColor = Color.white;
    private Material currentBulletMaterial;

    private void Awake()
    {
        weaponManager = GetComponentInParent<WeaponManager>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        lineRenderer = GetComponent<LineRenderer>();
    }

    // ... (OnEnable, OnDisable, Update 등은 기존 로직 유지) ...
    private void OnEnable()
    {
        if (lineRenderer != null) lineRenderer.enabled = false;
        if (weaponManager?.InputActions != null)
            weaponManager.InputActions.Player.Attack.performed += OnAttack;
    }

    private void OnDisable()
    {
        if (lineRenderer != null) lineRenderer.enabled = false;
        if (weaponManager?.InputActions != null)
            weaponManager.InputActions.Player.Attack.performed -= OnAttack;
    }

    private void Update()
    {
        Vector2 mouseScreenPos = weaponManager.InputActions.Player.Look.ReadValue<Vector2>();
        mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        RotateWeapon();

        if (!GameManager.instance.player.isCharging && !weaponManager.IsSwapping) DrawLaser();
        else if (lineRenderer != null) lineRenderer.enabled = false;

        HandleChargingVisuals();
    }
    // ... (여기까지 기존 유지) ...


    // ★ [수정] 외부에서 색상 받을 때, 기억만 해둠 (파티클 재생 X)
    public void UpdateVisuals(Color color, Material newMaterial)
    {
        // 1. 레이저는 바로 변경
        if (lineRenderer != null)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }

        // 2. 총알에 넘겨줄 정보 저장 (나중에 쏠 때 씀)
        currentBulletColor = color;
        currentBulletMaterial = newMaterial;

        // ★ [삭제됨] 여기서 파티클 Renderer 접근해서 색 바꾸던 로직 삭제
    }

    // ... (OnAttack 등 유지) ...
    private void OnAttack(InputAction.CallbackContext context)
    {
        if (weaponManager.IsSwapping) return;
        if (weaponManager.CurrentWeapon != WeaponManager.WeaponType.Gun) return;
        if (GameManager.instance.player.isCharging) return;
        if (Time.time < nextFireTime) return;

        if (GameManager.instance.stats.currentAmmo >= 100)
        {
            GameManager.instance.stats.currentAmmo -= 100;
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
        else Debug.Log("탄약 부족!");
    }

    private void Shoot()
    {
        if (bulletPrefab == null || muzzlePoint == null) return;

        // ★ [수정] 총알 생성 후 색상 전달
        GameObject bulletObj = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
        Bullet bulletScript = bulletObj.GetComponent<Bullet>();

        if (bulletScript != null)
        {
            // 아까 기억해둔 색상을 총알에게 전달!
            bulletScript.SetupVisuals(currentBulletColor, currentBulletMaterial);
        }

        Recoil();

        // ★ [삭제됨] muzzleFlashEffect.Play() 삭제!

        if (CameraFollow.instance != null) CameraFollow.instance.Shake(shakeDuration, shakeMagnitude);
    }

    // ... (Recoil, RotateWeapon 등 나머지 유지) ...
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

    private void HandleChargingVisuals()
    {
        if (GameManager.instance.player == null) return;
        bool isCharging = GameManager.instance.player.isCharging;
        float targetAlpha = isCharging ? 0f : 1f;
        Color currentColor = spriteRenderer.color;
        float newAlpha = Mathf.Lerp(currentColor.a, targetAlpha, Time.deltaTime * fadeSpeed);
        spriteRenderer.color = new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha);
    }

    private void DrawLaser()
    {
        if (lineRenderer == null || muzzlePoint == null) return;
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, muzzlePoint.position);
        Vector2 endPoint = (Vector2)muzzlePoint.position + ((Vector2)transform.right * laserLength);
        lineRenderer.SetPosition(1, endPoint);
    }
}