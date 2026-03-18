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
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Shooting Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float fireRate = 0.125f;
    private float nextFireTime = 0f;

    [Header("Recoil Settings")]
    [SerializeField] private float playerKnockbackForce = 3f;
    [SerializeField] private float gunRecoilDistance = 0.5f;
    [SerializeField] private float gunRecoilDuration = 0.2f;

    [Header("VFX Settings")]
    [SerializeField] private float shakeDuration = 0.05f;
    [SerializeField] private float shakeMagnitude = 0.02f;
    [SerializeField] private float fadeSpeed = 10f;

    [Header("Sound")]
    [SerializeField] private AudioClip sfxShoot;
    [SerializeField] private AudioClip sfxEmpty; // ★ 탄약 없을 때 '틱' 소리

    [SerializeField] private GameObject damageTextPrefab; // 총알 부족 텍스트
    private bool isAiming = false;
    private Color currentBulletColor = Color.white;
    private Material currentBulletMaterial;

    private void Awake()
    {
        weaponManager = GetComponentInParent<WeaponManager>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer != null) lineRenderer.useWorldSpace = true;
    }

    private void OnEnable()
    {
        if (lineRenderer != null) lineRenderer.enabled = false;
        // ★ 에러 수정: WeaponManager의 InputActions가 삭제되었으므로 구독 해제
    }

    private void OnDisable()
    {
        if (lineRenderer != null) lineRenderer.enabled = false;
        // ★ 에러 수정: WeaponManager의 InputActions가 삭제되었으므로 구독 해제

        // 마우스 커서 원상복구
        if (GameManager.instance != null && GameManager.instance.cursor != null)
        {
            GameManager.instance.cursor.ChangeCursor(CursorType.Default);
        }

        isAiming = false;
    }

    private void Update()
    {
        if (GameManager.instance.player == null) return;

        // [통제탑 연결] 컷신 등으로 조작이 끊기면 레이저 끄고 커서 초기화 후 리턴!
        if (!GameManager.instance.player.canControl)
        {
            if (lineRenderer != null) lineRenderer.enabled = false;
            if (isAiming)
            {
                isAiming = false;
                GameManager.instance.cursor.ChangeCursor(CursorType.Default);
            }
            return;
        }

        // ★ 에러 수정: WeaponManager 대신 중앙 통제소인 Player의 Input을 사용합니다.
        Vector2 mouseScreenPos = GameManager.instance.player.Input.Player.Look.ReadValue<Vector2>();
        mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        RotateWeapon();

        if (!GameManager.instance.player.isCharging && !weaponManager.IsSwapping)
            DrawLaser();
        else if (lineRenderer != null)
            lineRenderer.enabled = false;

        HandleChargingVisuals();
        HandleAimCursor();
    }

    private void DrawLaser()
    {
        if (lineRenderer == null || muzzlePoint == null) return;

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, muzzlePoint.position);

        Vector2 direction = transform.right;

        RaycastHit2D hit = Physics2D.Raycast(muzzlePoint.position, direction, laserLength, obstacleLayer);

        if (hit.collider != null)
        {
            lineRenderer.SetPosition(1, hit.point);
        }
        else
        {
            Vector2 endPoint = (Vector2)muzzlePoint.position + (direction * laserLength);
            lineRenderer.SetPosition(1, endPoint);
        }
    }

    public void UpdateVisuals(Color color, Material newMaterial)
    {
        if (lineRenderer != null)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
        currentBulletColor = color;
        currentBulletMaterial = newMaterial;
    }

    public void TriggerAttack()
    {
        if (weaponManager.IsSwapping || GameManager.instance.player.isCharging) return;
        if (Time.time < nextFireTime) return;

        // ★ [피드백 추가] 탄약이 100(1발분)보다 적으면
        if (GameManager.instance.stats.currentAmmo < 100)
        {
            if (sfxEmpty != null) SoundManager.instance.PlaySFX(sfxEmpty, 0.4f, 0.05f);
            SpawnAmmoEmptyText();

            Debug.Log("탄약 부족!");
            return;
        }

        // 탄약 충분할 때만 발사
        GameManager.instance.stats.currentAmmo -= 100;
        Shoot();
        nextFireTime = Time.time + fireRate;
    }

    private void Shoot()
    {
        if (bulletPrefab == null || muzzlePoint == null) return;

        GameObject bulletObj = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
        Bullet bulletScript = bulletObj.GetComponent<Bullet>();

        if (bulletScript != null)
        {
            bulletScript.SetupVisuals(currentBulletColor, currentBulletMaterial);
        }

        if (sfxShoot != null) SoundManager.instance.PlaySFX(sfxShoot, 0.2f, 0.1f);

        Recoil();
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(shakeDuration, shakeMagnitude);
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
        Vector3 originalLocalPos = new Vector3(0.05f, 0, 0);
        Vector3 recoilOffset = transform.right * -gunRecoilDistance;

        transform.position += recoilOffset;
        Vector3 recoilLocalPos = transform.localPosition;

        float elapsed = 0f;
        while (elapsed < gunRecoilDuration)
        {
            elapsed += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(recoilLocalPos, originalLocalPos, elapsed / gunRecoilDuration);
            yield return null;
        }
        transform.localPosition = originalLocalPos;
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

    private void HandleAimCursor()
    {
        if (GameManager.instance.player.isCharging || weaponManager.CurrentWeapon != WeaponManager.WeaponType.Gun)
        {
            if (isAiming)
            {
                isAiming = false;
                GameManager.instance.cursor.ChangeCursor(CursorType.Default);
            }
            return;
        }

        if (Mouse.current != null)
        {
            bool isHoldingRightClick = Mouse.current.rightButton.isPressed;

            if (isHoldingRightClick && !isAiming)
            {
                isAiming = true;
                GameManager.instance.cursor.ChangeCursor(CursorType.Aim);
            }
            else if (!isHoldingRightClick && isAiming)
            {
                isAiming = false;
                GameManager.instance.cursor.ChangeCursor(CursorType.Default);
            }
        }
    }

    private void SpawnAmmoEmptyText()
    {
        if (damageTextPrefab == null) return;

        // 현재 마우스 월드 좌표(mouseWorldPos)에 텍스트 생성
        GameObject textObj = Instantiate(damageTextPrefab, mouseWorldPos, Quaternion.identity);
        DamageText dt = textObj.GetComponent<DamageText>();

        if (dt != null)
        {
            // 아까 추가한 문자열용 Setup 호출
            dt.Setup("탄약 부족!", Color.red);
        }
    }
}