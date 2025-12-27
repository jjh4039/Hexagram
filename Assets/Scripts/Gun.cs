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

    [Header("Shooting Settings")]
    [SerializeField] private GameObject bulletPrefab; // 1단계에서 만든 총알 프리팹
    [SerializeField] private float fireRate = 0.125f;   // 발사 간격 (초)
    private float nextFireTime = 0f;                  // 다음 발사 가능 시간

    [Header("Recoil Settings")]
    [SerializeField] private float playerKnockbackForce = 3f; // 캐릭터가 밀리는 힘
    [SerializeField] private float gunRecoilDistance = 0.2f;  // 총이 뒤로 후퇴하는 거리
    [SerializeField] private float gunRecoilDuration = 0.1f;  // 총이 제자리로 돌아오는 시간

    [Header("VFX Settings")]
    [SerializeField] private ParticleSystem muzzleFlashEffect;
    [SerializeField] private float shakeDuration = 0.1f;  // 짧고 굵게
    [SerializeField] private float shakeMagnitude = 0.2f; // 흔들림 세기

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

    private void OnAttack(InputAction.CallbackContext context)
    {
        // 1. 쿨타임 체크 (아직 쏠 시간이 안 됐으면 무시)
        if (Time.time < nextFireTime) return;

        // 2. [추가] 탄약 체크 (100 이상일 때만 발사 가능)
        if (GameManager.instance.stats.currentAmmo >= 100)
        {
            // 3. [추가] 탄약 100 소모
            GameManager.instance.stats.currentAmmo -= 100;

            // 발사 실행
            Shoot();

            // 쿨타임 갱신
            nextFireTime = Time.time + fireRate;
        }
        else
        {
            // 탄약 부족 시 피드백 (로그나 효과음)
            Debug.Log("탄약 부족! (Need 100)");
            // 나중에 여기에 '틱-틱-' 거리는 빈 총 소리를 넣으면 좋아.
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null || muzzlePoint == null) return;

        // 1. 총알 생성 (위치: 총구, 회전: 총구의 회전값)
        Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);

        // 반동
        Recoil();

        // 머즐 플래시   
        if (muzzleFlashEffect != null)
        {
            muzzleFlashEffect.Play();
        }

        // 카메라 흔들림
        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.Shake(shakeDuration, shakeMagnitude);
        }

        // (나중에 구현) 발사 소리 SoundManager.Play("Bang");

    }

    private void OnEnable()
    {
        if (lineRenderer != null) lineRenderer.enabled = true;

        // 총이 활성화될 때 공격 키 이벤트를 연결
        if (weaponManager != null && weaponManager.InputActions != null)
        {
            weaponManager.InputActions.Player.Attack.performed += OnAttack;
        }
    }

    private void OnDisable()
    {
        if (lineRenderer != null) lineRenderer.enabled = false;

        if (weaponManager != null && weaponManager.InputActions != null)
        {
            weaponManager.InputActions.Player.Attack.performed -= OnAttack;
        }
    }

    private void Recoil()
    {
        // 시각적 반동 (총 흔들기)
        StopCoroutine("VisualRecoilRoutine");
        StartCoroutine("VisualRecoilRoutine");

        // 물리적 반동 (캐릭터 밀기)
        StopCoroutine("KnockbackRoutine");
        StartCoroutine("KnockbackRoutine");
    }

    private System.Collections.IEnumerator KnockbackRoutine()
    {
        if (GameManager.instance.player != null)
        {
            var player = GameManager.instance.player;

            // 1. [핵심] 공격 중 상태로 변경 -> Player.cs의 Move()가 정지됨
            player.isAttacking = true;

            // 2. 힘 가하기 (반대 방향)
            Vector2 knockbackDir = -transform.right;
            player.rigid.AddForce(knockbackDir * playerKnockbackForce, ForceMode2D.Impulse);

            // 3. 밀려나는 시간 (0.1 ~ 0.15초 추천)
            yield return new WaitForSeconds(0.1f);

            // 4. 미끄러짐 방지 (딱 멈추기)
            player.rigid.linearVelocity = Vector2.zero;

            // 5. 상태 복구 -> 다시 키보드 이동 가능
            player.isAttacking = false;
        }
    }

    private System.Collections.IEnumerator VisualRecoilRoutine()
    {
        // 현재 위치(원점) 저장
        // 주의: WeaponManager가 잡아준 위치를 기준으로 해야 하므로, 시작 전 위치를 기억하는 게 안전함
        Vector3 originalPos = new Vector3(0.05f, 0, 0); // WeaponManager에서 설정한 gunOffset 값과 동일하게 맞추거나,
                                                        // 혹은 Start 시점에 transform.localPosition을 저장해두는 변수를 써도 됨.

        // 여기서는 간단하게 현재 위치를 원점으로 잡고 시작 (연사 시 조금씩 밀리는 걸 방지하려면 고정값 추천)
        // 일단 심플하게 현재 위치에서 뒤로 뺌

        Vector3 recoilPos = originalPos - (Vector3.right * gunRecoilDistance);

        // 1. 쏘는 순간 뒤로 확 뺌 (탁!)
        transform.localPosition = recoilPos;

        float elapsed = 0f;

        // 2. 부드럽게 원위치로 복귀 (스르륵)
        while (elapsed < gunRecoilDuration)
        {
            elapsed += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(recoilPos, originalPos, elapsed / gunRecoilDuration);
            yield return null;
        }

        // 위치 보정 (확실하게 원점으로)
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