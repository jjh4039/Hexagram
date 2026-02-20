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

    private bool isAiming = false;
    private Color currentBulletColor = Color.white;
    private Material currentBulletMaterial;

    private void Awake()
    {
        weaponManager = GetComponentInParent<WeaponManager>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        lineRenderer = GetComponent<LineRenderer>();
    }

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

        // 마우스 커서 원상복구
        if (GameManager.instance != null && GameManager.instance.cursor != null)
        {
            GameManager.instance.cursor.ChangeCursor(CursorType.Default);
        }

        // ★ [수정 1] 버그의 핵심 원인 해결! 스크립트가 꺼질 때 에임 상태도 반드시 초기화
        isAiming = false;
    }

    private void Update()
    {
        Vector2 mouseScreenPos = weaponManager.InputActions.Player.Look.ReadValue<Vector2>();
        mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        RotateWeapon();

        if (!GameManager.instance.player.isCharging && !weaponManager.IsSwapping)
            DrawLaser(); // 레이저 그리기
        else if (lineRenderer != null)
            lineRenderer.enabled = false;

        HandleChargingVisuals();
        HandleAimCursor();
    }

    // ★ [수정됨] 레이캐스트를 쏴서 벽에 닿으면 거기까지만 그리기
    private void DrawLaser()
    {
        if (lineRenderer == null || muzzlePoint == null) return;

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, muzzlePoint.position); // 시작점 (총구)

        // 총이 바라보는 방향 (오른쪽)
        Vector2 direction = transform.right;

        // 1. 레이저 발사! (총구 위치에서, 방향으로, 길이만큼, 장애물 레이어만 감지)
        RaycastHit2D hit = Physics2D.Raycast(muzzlePoint.position, direction, laserLength, obstacleLayer);

        if (hit.collider != null)
        {
            // 2. 무언가(벽)에 닿았다면? -> 닿은 위치(hit.point)까지만 그립니다.
            // (Tag 확인이 필요하면 if (hit.collider.CompareTag("Wall")) 등을 쓸 수 있지만,
            // LayerMask로 거르는 게 성능상 훨씬 좋습니다.)
            lineRenderer.SetPosition(1, hit.point);
        }
        else
        {
            // 3. 아무것도 안 닿았으면? -> 최대 길이까지 쭉 그립니다.
            Vector2 endPoint = (Vector2)muzzlePoint.position + (direction * laserLength);
            lineRenderer.SetPosition(1, endPoint);
        }
    }

    // ... (이하 함수들은 기존과 동일하게 유지) ...
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

        GameObject bulletObj = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
        Bullet bulletScript = bulletObj.GetComponent<Bullet>();

        if (bulletScript != null)
        {
            bulletScript.SetupVisuals(currentBulletColor, currentBulletMaterial);
        }

        // 사운드 재생
        if (sfxShoot != null)
            SoundManager.instance.PlaySFX(sfxShoot, 0.2f);

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

    private void HandleAimCursor()
    {
        // ★ [수정 2] 주사위 충전 중(무기를 넣은 상태)이거나 다른 무기일 때의 처리
        if (GameManager.instance.player.isCharging || weaponManager.CurrentWeapon != WeaponManager.WeaponType.Gun)
        {
            // 만약 우클릭 조준 중에 주사위 쿨타임이 돌아서 무기가 들어갔다면 강제로 커서 초기화
            if (isAiming)
            {
                isAiming = false;
                GameManager.instance.cursor.ChangeCursor(CursorType.Default);
            }
            return; // 이후 로직(우클릭 감지) 실행 안 함
        }

        // ★ [수정 3] IsSwapping 체크 삭제 -> 무기를 꺼내는 도중에도 우클릭을 누르면 즉시 커서 변경!

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
}
