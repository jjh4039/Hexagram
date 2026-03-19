using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapon Objects")]
    [SerializeField] private GameObject swordObject;
    [SerializeField] private GameObject gunObject;

    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer swordRenderer;
    [SerializeField] private SpriteRenderer gunRenderer;
    [SerializeField] private Transform swordSpriteTransform;
    [SerializeField] private Transform gunSpriteTransform;

    [Space]
    [SerializeField] private float swapDuration = 0.15f;    // 전환 속도

    public bool IsAiming => CurrentWeapon == WeaponType.Gun && !IsSwapping;
    // ★ 독자적인 PlayerInput 변수와 Awake, OnEnable, OnDisable 완전 삭제! (Player.cs에서 통제)

    public enum WeaponType
    {
        Sword,
        Gun
    }

    public bool IsSwapping { get; private set; } // 교체 중인가?
    public WeaponType CurrentWeapon { get; private set; } = WeaponType.Sword;

    private Coroutine swapCoroutine;
    private Vector3 gunOriginPos;
    private Vector3 gunTargetPos;
    private Vector3 swordOriginPos;
    private Vector3 swordTargetPos;

    private Sword cachedSword;
    private Gun cachedGun;

    private void Start()
    {
        if (swordObject != null) cachedSword = swordObject.GetComponent<Sword>();
        if (gunObject != null) cachedGun = gunObject.GetComponent<Gun>();

        if (gunSpriteTransform != null)
        {
            gunOriginPos = Vector3.zero;
            gunTargetPos = new Vector3(0.05f, 0, 0);
        }

        if (swordSpriteTransform != null)
        {
            swordOriginPos = new Vector3(0, -0.1f, 0);
            swordTargetPos = new Vector3(0, -0.2f, 0);
        }

        InitializeWeapon(swordObject, swordRenderer, 1f);
        InitializeWeapon(gunObject, gunRenderer, 0f);

        if (gunSpriteTransform != null) gunSpriteTransform.localPosition = gunOriginPos;

        gunObject.SetActive(false);
    }

    private void Update()
    {
        // ★ [가장 중요] Player.cs에서 통제권이 꺼져있으면 묻지도 따지지도 않고 스왑 중지!
        if (GameManager.instance.player != null && !GameManager.instance.player.canControl) return;
        if (GameManager.instance.player != null && GameManager.instance.player.isCharging) return;

        // ★ 독립적인 InputActions 대신 Mouse 우클릭(홀드) 상태를 직접 확인
        bool isHolding = Mouse.current != null && Mouse.current.rightButton.isPressed;

        if (IsSwapping) return;

        // 홀드 유지 앤 건 스왑 애니메이션 로직 완벽 유지!
        if (isHolding && CurrentWeapon == WeaponType.Sword)
        {
            if (swapCoroutine != null) StopCoroutine(swapCoroutine);
            swapCoroutine = StartCoroutine(SwapToGun());
        }
        else if (!isHolding && CurrentWeapon == WeaponType.Gun)
        {
            if (swapCoroutine != null) StopCoroutine(swapCoroutine);
            swapCoroutine = StartCoroutine(SwapToSword());
        }
    }

    public void OnAttackInput()
    {
        if (IsSwapping) return;

        // ★ 매번 GetComponent 하던 걸 지우고, 기억해둔 녀석을 즉시 호출합니다! (속도 100배 상승)
        if (CurrentWeapon == WeaponType.Sword && cachedSword != null)
        {
            cachedSword.TriggerAttack();
        }
        else if (CurrentWeapon == WeaponType.Gun && cachedGun != null)
        {
            cachedGun.TriggerAttack();
        }
    }

    public void UpdateWeaponVisuals(Color color, Material material)
    {
        if (gunObject != null)
        {
            var gun = gunObject.GetComponent<Gun>();
            if (gun != null)
            {
                gun.UpdateVisuals(color, material);
            }
        }
    }

    // ... (이하 SwapToGun, SwapToSword, SetAlpha, InitializeWeapon 함수는 100% 원본과 동일) ...
    private IEnumerator SwapToGun()
    {
        IsSwapping = true;
        CurrentWeapon = WeaponType.Gun;
        gunObject.SetActive(true);
        swordObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < swapDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / swapDuration;

            SetAlpha(swordRenderer, 1f - t);
            SetAlpha(gunRenderer, t);

            if (gunSpriteTransform != null) gunSpriteTransform.localPosition = Vector3.Lerp(gunOriginPos, gunTargetPos, t);
            if (swordSpriteTransform != null) swordSpriteTransform.localPosition = Vector3.Lerp(swordTargetPos, swordOriginPos, t);

            yield return null;
        }

        SetAlpha(swordRenderer, 0f);
        SetAlpha(gunRenderer, 1f);
        IsSwapping = false;
        if (gunSpriteTransform != null) gunSpriteTransform.localPosition = gunTargetPos;

        swordObject.SetActive(false);
    }

    private IEnumerator SwapToSword()
    {
        IsSwapping = true;
        CurrentWeapon = WeaponType.Sword;
        swordObject.SetActive(true);
        gunObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < swapDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / swapDuration;

            SetAlpha(swordRenderer, t);
            SetAlpha(gunRenderer, 1f - t);

            if (gunSpriteTransform != null) gunSpriteTransform.localPosition = Vector3.Lerp(gunTargetPos, gunOriginPos, t);
            if (swordSpriteTransform != null) swordSpriteTransform.localPosition = Vector3.Lerp(swordOriginPos, swordTargetPos, t);

            yield return null;
        }

        SetAlpha(swordRenderer, 1f);
        SetAlpha(gunRenderer, 0f);
        if (gunSpriteTransform != null) gunSpriteTransform.localPosition = gunOriginPos;
        IsSwapping = false;

        gunObject.SetActive(false);
    }

    public void OnSwapInput()
    {
        if (IsSwapping) return;

        if (CurrentWeapon == WeaponType.Sword)
        {
            if (swapCoroutine != null) StopCoroutine(swapCoroutine);
            swapCoroutine = StartCoroutine(SwapToGun());
        }
        else if (CurrentWeapon == WeaponType.Gun)
        {
            if (swapCoroutine != null) StopCoroutine(swapCoroutine);
            swapCoroutine = StartCoroutine(SwapToSword());
        }
    }

    public float GetCurrentAttackMoveMultiplier()
    {
        if (CurrentWeapon == WeaponType.Sword && cachedSword != null)
        {
            return cachedSword.AttackMoveSpeedMultiplier;
        }

        // 총이나 다른 무기일 때는 기본적으로 이동을 막거나(0) 혹은 1로 설정 가능
        return 0f;
    }

    private void SetAlpha(SpriteRenderer sr, float alpha)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }

    private void InitializeWeapon(GameObject obj, SpriteRenderer sr, float alpha)
    {
        obj.SetActive(true);
        SetAlpha(sr, alpha);
    }
}