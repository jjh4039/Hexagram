using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

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
    [SerializeField] private GameObject hitEffectPrefab; 
    [SerializeField] private float shakeDuration = 0.05f;
    [SerializeField] private float shakeMagnitude = 0.02f;

    [Header("Sound")]
    [SerializeField] private AudioClip sfxShoot;
    [SerializeField] private AudioClip sfxEmpty;

    [SerializeField] private GameObject damageTextPrefab;
    private bool isAiming = false;

    [Header("Object Pool Settings")]
    [SerializeField] private Transform poolParent; 
    [SerializeField] private int bulletPoolSize = 30;
    [SerializeField] private int hitEffectPoolSize = 20;
    
    private static Queue<GameObject> _bulletPool = new Queue<GameObject>();
    private static Queue<GameObject> _hitEffectPool = new Queue<GameObject>();
    private static Transform _poolContainer; 

    private bool _wasHoldingAttack = false; 

    private ContactFilter2D obstacleFilter; // 트리거를 무시하기 위한 물리 필터
    private RaycastHit2D[] hitResults = new RaycastHit2D[1]; // 레이캐스트 결과 캐싱용 배열

    private void Awake()
    {
        weaponManager = GetComponentInParent<WeaponManager>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer != null) lineRenderer.useWorldSpace = true;

        obstacleFilter = new ContactFilter2D();
        obstacleFilter.layerMask = obstacleLayer;
        obstacleFilter.useLayerMask = true;
        obstacleFilter.useTriggers = false; 

        InitPools();
    }

    private void InitPools()
    {
        if (_poolContainer == null)
        {
            _poolContainer = new GameObject("Gun_ObjectPool").transform;
            if (poolParent != null) _poolContainer.SetParent(poolParent);

            _bulletPool.Clear(); 
            _hitEffectPool.Clear(); 
        }

        if (_bulletPool.Count == 0 && bulletPrefab != null)
        {
            for (int i = 0; i < bulletPoolSize; i++)
            {
                GameObject obj = Instantiate(bulletPrefab, _poolContainer);
                obj.SetActive(false);
                _bulletPool.Enqueue(obj);
            }
        }

        if (_hitEffectPool.Count == 0 && hitEffectPrefab != null)
        {
            for (int i = 0; i < hitEffectPoolSize; i++)
            {
                GameObject obj = Instantiate(hitEffectPrefab, _poolContainer);
                obj.SetActive(false);
                _hitEffectPool.Enqueue(obj);
            }
        }
    }

    private void OnDisable()
    {
        if (lineRenderer != null) lineRenderer.enabled = false;

        if (GameManager.instance != null && GameManager.instance.cursor != null)
            GameManager.instance.cursor.ChangeCursor(CursorType.Default);

        isAiming = false;
        _wasHoldingAttack = false; 

        CancelInvoke("ResetKnockbackState");
        ResetKnockbackState();
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
            _wasHoldingAttack = false;
            return;
        }

        UpdateMousePosition();
        RotateWeapon();

        if (!weaponManager.IsSwapping) DrawLaser();
        else if (lineRenderer != null) lineRenderer.enabled = false;

        HandleAimCursor();
        
        TryAutoFire();
    }

    private bool IsHoldingAttack()
    {
        if (InputStateManager.Instance == null) return false;
        
        var state = InputStateManager.Instance.CurrentInputState;
        var actions = InputStateManager.Instance.Actions;

        if (state == InputState.Normal)
            return actions.Normal.Attack.ReadValue<float>() > 0.5f;
        if (state == InputState.Combat)
            return actions.Combat.Attack.ReadValue<float>() > 0.5f;

        return false;
    }

    private void TryAutoFire()
    {
        if (weaponManager.IsSwapping || weaponManager.CurrentWeapon != WeaponManager.WeaponType.Gun) 
        {
            _wasHoldingAttack = false;
            return;
        }

        bool isHolding = IsHoldingAttack();

        if (isHolding)
        {
            if (Time.time >= nextFireTime)
            {
                bool isAutoFiring = _wasHoldingAttack; 
                ExecuteTriggerAttack(isAutoFiring); 
            }
        }
        
        _wasHoldingAttack = isHolding;
    }

    public void TriggerAttack()
    {
        if (weaponManager.IsSwapping) return;
        ExecuteTriggerAttack(false); 
    }

    private void ExecuteTriggerAttack(bool isAutoFiring)
    {
        float finalAtkSpeed = GetFinalAttackSpeed();
        float interval = Mathf.Max(fireRate / finalAtkSpeed, Mathf.Max(minRecoilDuration, gunRecoilDuration / finalAtkSpeed));

        if (Time.time < nextFireTime) return;

        if (GameManager.instance.stats.currentAmmo < 100)
        {
            if (!isAutoFiring)
            {
                if (sfxEmpty != null) SoundManager.instance.PlaySFX(sfxEmpty, 0.4f, 0.05f);
                SpawnAmmoEmptyText();
            }
            
            nextFireTime = Time.time + interval; 
            return;
        }

        GameManager.instance.stats.currentAmmo -= 100;
        Shoot(Mathf.Max(minRecoilDuration, gunRecoilDuration / finalAtkSpeed));
        nextFireTime = Time.time + interval;
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

        int hitCount = Physics2D.Raycast(origin, dir, obstacleFilter, hitResults, dist);

        if (hitCount > 0)
        {
            return hitResults[0].point - (dir * 0.05f);
        }

        return target;
    }

    private void DrawLaser()
    {
        if (lineRenderer == null || muzzlePoint == null) return;

        Vector2 safeMuzzlePos = GetSafeMuzzlePosition();

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, safeMuzzlePos);

        int hitCount = Physics2D.Raycast(safeMuzzlePos, transform.right, obstacleFilter, hitResults, laserLength);
        
        lineRenderer.SetPosition(1, hitCount > 0 ? hitResults[0].point : safeMuzzlePos + (Vector2)transform.right * laserLength);
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

        GameObject bulletObj = null;
        
        while (_bulletPool.Count > 0) 
        {
            bulletObj = _bulletPool.Dequeue();
            if (bulletObj != null) break;
        }

        if (bulletObj == null)
        {
            bulletObj = Instantiate(bulletPrefab, _poolContainer); 
        }

        bulletObj.transform.position = safeMuzzlePos;
        bulletObj.transform.rotation = muzzlePoint.rotation;
        
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            float finalDamageMult = stats.finalAttackPower * stats.buffFinalDamageMultiplier; 
            
            bullet.SetupCombatData(
                stats.rangeAttackPower, 
                stats.rangedDamageVariance, 
                stats.GetFinalCriticalChance(), 
                stats.GetFinalCriticalDamageMultiplier(), 
                stats.diceDamageMultiplier, 
                stats.diceRangedDamageMultiplier, 
                strongMult, 
                stats.bonusPenetration, 
                finalDamageMult
            );
        }

        bulletObj.SetActive(true); 

        if (sfxShoot != null) SoundManager.instance.PlaySFX(sfxShoot, 0.2f, 0.1f);
        Recoil(recoilDur);
        if (CameraFollow.Instance != null) CameraFollow.Instance.HitShake(shakeDuration, shakeMagnitude);
    }

    public static void ReturnBullet(GameObject obj)
    {
        if (obj == null) return; 
        obj.SetActive(false);
        _bulletPool.Enqueue(obj);
    }

    public static void SpawnHitEffect(Vector3 position, Quaternion rotation, Material mat, Color col)
    {
        if (_hitEffectPool.Count == 0) return; 

        GameObject vfxObj = null;

        while (_hitEffectPool.Count > 0) 
        {
            vfxObj = _hitEffectPool.Dequeue();
            if (vfxObj != null) break;
        }

        if (vfxObj == null) return; 
        
        vfxObj.transform.position = position;
        vfxObj.transform.rotation = rotation * Quaternion.Euler(0f, 0f, 180f);

        ParticleSystem ps = vfxObj.GetComponent<ParticleSystem>();
        ParticleSystemRenderer psr = vfxObj.GetComponent<ParticleSystemRenderer>();

        if (psr != null && mat != null) psr.material = mat;
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = col;
        }

        vfxObj.SetActive(true);
        if (ps != null) ps.Play();

        DelayReturner returner = vfxObj.GetComponent<DelayReturner>();
        if (returner == null) returner = vfxObj.AddComponent<DelayReturner>();
        
        returner.StartDelayReturn(1.0f, () => {
            if (vfxObj != null) 
            {
                vfxObj.SetActive(false);
                _hitEffectPool.Enqueue(vfxObj);
            }
        });
    }

    private void Recoil(float dur)
    {
        StopCoroutine("VisualRecoilRoutine");
        StartCoroutine(VisualRecoilRoutine(dur));

        Player player = GameManager.instance.player;
        if (player != null)
        {
            player.isAttacking = true;
            player.isRecoiling = true;

            player.rigid.AddForce(-transform.right * playerKnockbackForce, ForceMode2D.Impulse);

            CancelInvoke("ResetKnockbackState");
            Invoke("ResetKnockbackState", 0.1f);
        }
    }

    private void ResetKnockbackState()
    {
        Player player = GameManager.instance?.player;
        if (player != null)
        {
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
        
        DamageText dt = DamageText.Spawn(damageTextPrefab, mouseWorldPos);
        if (dt != null) dt.Setup("총알 부족!", Color.red, 2f);
    }
}

public class DelayReturner : MonoBehaviour
{
    public void StartDelayReturn(float delay, System.Action onComplete)
    {
        StartCoroutine(Co_Delay(delay, onComplete));
    }

    private System.Collections.IEnumerator Co_Delay(float d, System.Action act)
    {
        yield return new WaitForSeconds(d);
        act?.Invoke();
    }
}