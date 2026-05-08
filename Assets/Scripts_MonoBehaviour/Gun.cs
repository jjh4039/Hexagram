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
    [SerializeField] private float laserLength = 100f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Shooting Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float fireRate = 0.125f;
    private float nextFireTime = 0f;

    [Header("Recoil Settings")]
    [SerializeField] private float playerKnockbackForce = 3f;
    [SerializeField] private float gunRecoilDistance = 0.3f;
    [SerializeField] private float gunRecoilDuration = 0.2f;
    [SerializeField] private float minRecoilDuration = 0.05f;

    [Header("VFX Settings")]
    [SerializeField] private float shakeDuration = 0.05f;
    [SerializeField] private float shakeMagnitude = 0.02f;

    [Header("Sound")]
    [SerializeField] private AudioClip sfxShoot;
    [SerializeField] private AudioClip sfxEmpty;

    [SerializeField] private GameObject damageTextPrefab;
    private bool isAiming = false;

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

    private void UpdateMousePosition()
    {
        Vector2 screenPos = Vector2.zero;
        var actions = InputStateManager.Instance.Actions;
        var state = InputStateManager.Instance.CurrentInputState;

        if (state == InputState.Normal) screenPos = actions.Normal.Look.ReadValue<Vector2>();
        else if (state == InputState.Combat) screenPos = actions.Combat.Look.ReadValue<Vector2>();

        mouseWorldPos = Camera.main.ScreenToWorldPoint(screenPos);
    }

    private Vector2 GetSafeMuzzlePosition()
    {
        if (weaponManager == null || muzzlePoint == null) return transform.position;

        Vector2 origin = weaponManager.transform.position;
        Vector2 target = muzzlePoint.position;
        Vector2 dir = (target - origin).normalized;
        float dist = Vector2.Distance(origin, target);

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, obstacleLayer);

        if (hit.collider != null)
        {
            return hit.point - (dir * 0.05f);
        }

        return target;
    }

    private void DrawLaser()
    {
        if (lineRenderer == null || muzzlePoint == null) return;

        Vector2 safeMuzzlePos = GetSafeMuzzlePosition();

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, safeMuzzlePos);

        RaycastHit2D hit = Physics2D.Raycast(safeMuzzlePos, transform.right, laserLength, obstacleLayer);
        lineRenderer.SetPosition(1, hit.collider != null ? hit.point : safeMuzzlePos + (Vector2)transform.right * laserLength);
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

        Vector2 safeMuzzlePos = GetSafeMuzzlePosition();

        GameObject bulletObj = Instantiate(bulletPrefab, safeMuzzlePos, muzzlePoint.rotation);
        Bullet bullet = bulletObj.GetComponent<Bullet>();

        if (bullet != null)
        {
            // 수정: UpdateVisuals가 삭제되었으므로, 탄환 프리팹 고유의 설정대로 생성
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
            player.isRecoiling = true;

            player.rigid.AddForce(-transform.right * playerKnockbackForce, ForceMode2D.Impulse);
            yield return new WaitForSeconds(0.1f);

            if (!player.isKnockedBack)
            {
                player.rigid.linearVelocity = Vector2.zero;
            }

            player.isAttacking = false;
            player.isRecoiling = false;
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
        if (dt != null) dt.Setup("총알 부족!", Color.red, 2f);
    }
}