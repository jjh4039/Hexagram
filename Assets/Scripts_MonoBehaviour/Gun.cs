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
    private Color currentBulletColor = Color.white;
    private Material currentBulletMaterial;

    private void Awake()
    {
        weaponManager = GetComponentInParent<WeaponManager>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer != null)
            lineRenderer.useWorldSpace = true;
    }

    private void OnEnable()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    private void OnDisable()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = false;

        if (GameManager.instance != null && GameManager.instance.cursor != null)
        {
            GameManager.instance.cursor.ChangeCursor(CursorType.Default);
        }

        isAiming = false;
    }

    private void Update()
    {
        if (GameManager.instance.player == null)
            return;

        if (!GameManager.instance.player.canControl)
        {
            if (lineRenderer != null)
                lineRenderer.enabled = false;

            if (isAiming)
            {
                isAiming = false;
                GameManager.instance.cursor.ChangeCursor(CursorType.Default);
            }

            return;
        }

        Vector2 mouseScreenPos = GameManager.instance.player.Input.Player.Look.ReadValue<Vector2>();
        mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        RotateWeapon();

        if (!weaponManager.IsSwapping)
            DrawLaser();
        else if (lineRenderer != null)
            lineRenderer.enabled = false;

        HandleAimCursor();
    }

    private void DrawLaser()
    {
        if (lineRenderer == null || muzzlePoint == null)
            return;

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
        if (weaponManager.IsSwapping)
            return;

        float finalAttackSpeed = GetFinalAttackSpeed();
        float attackSpeedAdjustedFireRate = fireRate / finalAttackSpeed;
        float recoilDurationAdjusted = Mathf.Max(minRecoilDuration, gunRecoilDuration / finalAttackSpeed);
        float finalFireInterval = Mathf.Max(attackSpeedAdjustedFireRate, recoilDurationAdjusted);

        if (Time.time < nextFireTime)
            return;

        if (GameManager.instance.stats.currentAmmo < 100)
        {
            if (sfxEmpty != null)
                SoundManager.instance.PlaySFX(sfxEmpty, 0.4f, 0.05f);

            SpawnAmmoEmptyText();
            Debug.Log("탄약 부족!");
            return;
        }

        GameManager.instance.stats.currentAmmo -= 100;
        Shoot(recoilDurationAdjusted);
        nextFireTime = Time.time + finalFireInterval;
    }

    private float GetFinalAttackSpeed()
    {
        if (GameManager.instance == null || GameManager.instance.stats == null)
            return 1f;

        float finalAttackSpeed = GameManager.instance.stats.attackSpeed * GameManager.instance.stats.attackSpeedMultiplier;
        return Mathf.Max(0.01f, finalAttackSpeed);
    }

    private void Shoot(float recoilDurationAdjusted)
    {
        if (bulletPrefab == null || muzzlePoint == null)
            return;

        GameObject bulletObj = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
        Bullet bulletScript = bulletObj.GetComponent<Bullet>();

        if (bulletScript != null)
        {
            bulletScript.SetupVisuals(currentBulletColor, currentBulletMaterial);
        }

        if (sfxShoot != null)
            SoundManager.instance.PlaySFX(sfxShoot, 0.2f, 0.1f);

        Recoil(recoilDurationAdjusted);

        if (CameraFollow.instance != null)
            CameraFollow.instance.HitShake(shakeDuration, shakeMagnitude);
    }

    private void Recoil(float recoilDurationAdjusted)
    {
        StopCoroutine("VisualRecoilRoutine");
        StartCoroutine(VisualRecoilRoutine(recoilDurationAdjusted));
        StopCoroutine("KnockbackRoutine");
        StartCoroutine("KnockbackRoutine");
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

    private System.Collections.IEnumerator VisualRecoilRoutine(float recoilDuration)
    {
        Vector3 originalLocalPos = new Vector3(0.05f, 0f, 0f);
        Vector3 recoilOffset = transform.right * -gunRecoilDistance;

        transform.localPosition += recoilOffset;
        Vector3 recoilLocalPos = transform.localPosition;

        float elapsed = 0f;
        while (elapsed < recoilDuration)
        {
            elapsed += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(recoilLocalPos, originalLocalPos, elapsed / recoilDuration);
            yield return null;
        }

        transform.localPosition = originalLocalPos;
    }

    private void RotateWeapon()
    {
        Vector2 lookDir = mouseWorldPos - (Vector2)transform.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Vector3 localScale = Vector3.one * 0.8f;
        localScale.y = (angle > 90f || angle < -90f) ? 0.8f : -0.8f;
        localScale.z = 1f;
        transform.localScale = localScale;
    }

    private void HandleAimCursor()
    {
        if (weaponManager.CurrentWeapon != WeaponManager.WeaponType.Gun)
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
        if (damageTextPrefab == null)
            return;

        GameObject textObj = Instantiate(damageTextPrefab, mouseWorldPos, Quaternion.identity);
        DamageText dt = textObj.GetComponent<DamageText>();

        if (dt != null)
        {
            dt.Setup("탄약 부족!", Color.red);
        }
    }
}