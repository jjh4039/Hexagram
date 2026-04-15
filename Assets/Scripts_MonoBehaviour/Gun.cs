using UnityEngine;
using UnityEngine.InputSystem;

// 원거리 공격 기능을 담당하는 총기 클래스
public class Gun : MonoBehaviour
{
    private WeaponManager weaponManager;                         // 무기 교체 관리자
    private SpriteRenderer spriteRenderer;                       // 총기 렌더러
    private Vector2 mouseWorldPos;                               // 마우스의 월드 좌표

    [Header("Aiming Settings")]
    [SerializeField] private Transform muzzlePoint;              // 총구 위치
    [SerializeField] private LineRenderer lineRenderer;          // 조준 레이저
    [SerializeField] private float laserLength = 100f;           // 레이저 최대 거리
    [SerializeField] private LayerMask obstacleLayer;            // 충돌 장애물 레이어

    [Header("Shooting Settings")]
    [SerializeField] private GameObject bulletPrefab;            // 발사체 프리팹
    [SerializeField] private float fireRate = 0.125f;            // 발사 속도
    private float nextFireTime = 0f;                             // 다음 발사 가능 시간

    [Header("Recoil Settings")]
    [SerializeField] private float playerKnockbackForce = 3f;    // 발사 시 반동 힘
    [SerializeField] private float gunRecoilDistance = 0.3f;     // 총기 밀림 거리
    [SerializeField] private float gunRecoilDuration = 0.2f;     // 반동 연출 시간
    [SerializeField] private float minRecoilDuration = 0.05f;    // 최소 반동 시간

    [Header("VFX Settings")]
    [SerializeField] private float shakeDuration = 0.05f;        // 화면 흔들림 시간
    [SerializeField] private float shakeMagnitude = 0.02f;       // 화면 흔들림 강도

    [Header("Sound")]
    [SerializeField] private AudioClip sfxShoot;                 // 발사 사운드
    [SerializeField] private AudioClip sfxEmpty;                 // 잔탄 부족 사운드

    [SerializeField] private GameObject damageTextPrefab;        // 안내 텍스트 프리팹
    private bool isAiming = false;                               // 현재 조준 중인지 확인
    private Color currentBulletColor = Color.white;              // 현재 탄환 색상
    private Material currentBulletMaterial;                      // 현재 탄환 머티리얼

    private void Awake()
    {
        weaponManager = GetComponentInParent<WeaponManager>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer != null) lineRenderer.useWorldSpace = true;
    }

    private void OnDisable()
    {
        if (lineRenderer != null) lineRenderer.enabled = false;
        
        if (GameManager.instance != null && GameManager.instance.cursor != null)
            GameManager.instance.cursor.ChangeCursor(CursorType.Default);

        isAiming = false;
    }

    private void Update()
    {
        if (GameManager.instance.player == null || InputStateManager.Instance == null) return;

        // UI 모드이거나 제어가 불가능한 경우 시각 효과 차단
        if (InputStateManager.Instance.CurrentInputState == InputState.UI || !GameManager.instance.player.canControl)
        {
            if (lineRenderer != null) lineRenderer.enabled = false;
            if (isAiming)
            {
                isAiming = false;
                GameManager.instance.cursor.ChangeCursor(CursorType.Default);
            }
            return;
        }

        UpdateMousePosition();
        RotateWeapon();

        if (!weaponManager.IsSwapping) DrawLaser();
        else if (lineRenderer != null) lineRenderer.enabled = false;

        HandleAimCursor();
    }

    // 현재 조작 상태에 따른 마우스 위치 갱신
    private void UpdateMousePosition()
    {
        Vector2 screenPos = Vector2.zero;
        var actions = InputStateManager.Instance.Actions;
        var state = InputStateManager.Instance.CurrentInputState;

        // ReadValue를 통해 현재 마우스 좌표를 안전하게 읽어옴
        if (state == InputState.Normal) screenPos = actions.Normal.Look.ReadValue<Vector2>();
        else if (state == InputState.Combat) screenPos = actions.Combat.Look.ReadValue<Vector2>();

        mouseWorldPos = Camera.main.ScreenToWorldPoint(screenPos);
    }

    private void DrawLaser()
    {
        if (lineRenderer == null || muzzlePoint == null) return;

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, muzzlePoint.position);

        RaycastHit2D hit = Physics2D.Raycast(muzzlePoint.position, transform.right, laserLength, obstacleLayer);
        lineRenderer.SetPosition(1, hit.collider != null ? hit.point : (Vector2)muzzlePoint.position + (Vector2)transform.right * laserLength);
    }

    public void UpdateVisuals(Color color, Material newMaterial)
    {
        if (lineRenderer != null) { lineRenderer.startColor = color; lineRenderer.endColor = color; }
        currentBulletColor = color;
        currentBulletMaterial = newMaterial;
    }

    public void TriggerAttack()
    {
        if (weaponManager.IsSwapping) return;

        float finalAtkSpeed = GetFinalAttackSpeed();
        float interval = Mathf.Max(fireRate / finalAtkSpeed, Mathf.Max(minRecoilDuration, gunRecoilDuration / finalAtkSpeed));

        if (Time.time < nextFireTime) return;

        if (GameManager.instance.stats.currentAmmo < 100)
        {
            if (sfxEmpty != null) SoundManager.instance.PlaySFX(sfxEmpty, 0.4f, 0.05f);
            SpawnAmmoEmptyText();
            return;
        }

        GameManager.instance.stats.currentAmmo -= 100;
        Shoot(Mathf.Max(minRecoilDuration, gunRecoilDuration / finalAtkSpeed));
        nextFireTime = Time.time + interval;
    }

    private float GetFinalAttackSpeed()
    {
        if (GameManager.instance?.stats == null) return 1f;
        return Mathf.Max(0.01f, GameManager.instance.stats.attackSpeed * GameManager.instance.stats.diceAttackSpeedMultiplier);
    }

    private void Shoot(float recoilDur)
    {
        if (bulletPrefab == null || muzzlePoint == null) return;

        Player player = GameManager.instance.player;
        PlayerStats stats = GameManager.instance.stats;

        float strongMult = 1f;
        if (player != null && player.TryConsumeStrongAttack(out float mult)) strongMult = mult;

        GameObject bulletObj = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
        Bullet bullet = bulletObj.GetComponent<Bullet>();

        if (bullet != null)
        {
            bullet.SetupVisuals(currentBulletColor, currentBulletMaterial);
            bullet.SetupCombatData(stats.rangeAttackPower, stats.rangedDamageVariance, stats.criticalChance, 
                stats.GetFinalCriticalDamageMultiplier(), stats.diceDamageMultiplier, stats.diceRangedDamageMultiplier, strongMult);
        }

        if (sfxShoot != null) SoundManager.instance.PlaySFX(sfxShoot, 0.2f, 0.1f);
        Recoil(recoilDur);
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(shakeDuration, shakeMagnitude);
    }

    private void Recoil(float dur)
    {
        StopCoroutine("VisualRecoilRoutine");
        StartCoroutine(VisualRecoilRoutine(dur));
        StopCoroutine("KnockbackRoutine");
        StartCoroutine("KnockbackRoutine");
    }

    private System.Collections.IEnumerator KnockbackRoutine()
    {
        Player player = GameManager.instance.player;
        if (player != null)
        {
            player.isAttacking = true;
            player.rigid.AddForce(-transform.right * playerKnockbackForce, ForceMode2D.Impulse);
            yield return new WaitForSeconds(0.1f);
            player.rigid.linearVelocity = Vector2.zero;
            player.isAttacking = false;
        }
    }

    private System.Collections.IEnumerator VisualRecoilRoutine(float dur)
    {
        Vector3 origin = new Vector3(0.05f, 0f, 0f);
        transform.localPosition += transform.right * -gunRecoilDistance;
        Vector3 recoilPos = transform.localPosition;

        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(recoilPos, origin, elapsed / dur);
            yield return null;
        }
        transform.localPosition = origin;
    }

    private void RotateWeapon()
    {
        Vector2 lookDir = mouseWorldPos - (Vector2)transform.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        float flipY = (angle > 90f || angle < -90f) ? 0.8f : -0.8f;
        transform.localScale = new Vector3(0.8f, flipY, 1f);
    }

    private void HandleAimCursor()
    {
        if (weaponManager.CurrentWeapon != WeaponManager.WeaponType.Gun)
        {
            if (isAiming) { isAiming = false; GameManager.instance.cursor.ChangeCursor(CursorType.Default); }
            return;
        }
        
        bool isHoldingRight = false;
        var actions = InputStateManager.Instance.Actions;
        var state = InputStateManager.Instance.CurrentInputState;

        if (state == InputState.Normal) 
            isHoldingRight = actions.Normal.Aim.ReadValue<float>() > 0.5f;
        else if (state == InputState.Combat) 
            isHoldingRight = actions.Combat.Aim.ReadValue<float>() > 0.5f;

        // 커서 변경 로직 (기존 유지)
        if (isHoldingRight && !isAiming)
        {
            isAiming = true;
            GameManager.instance.cursor.ChangeCursor(CursorType.Aim);
        }
        else if (!isHoldingRight && isAiming)
        {
            isAiming = false;
            GameManager.instance.cursor.ChangeCursor(CursorType.Default);
        }
    }

    private void SpawnAmmoEmptyText()
    {
        if (damageTextPrefab == null) return;
        GameObject obj = Instantiate(damageTextPrefab, mouseWorldPos, Quaternion.identity);
        DamageText dt = obj.GetComponent<DamageText>();
        if (dt != null) dt.Setup("Out of ammo", Color.red);
    }
}