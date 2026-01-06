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

    public PlayerInput InputActions { get; private set; }

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

    private void Awake()
    {
        InputActions = new PlayerInput();
    }

    private void OnEnable() => InputActions.Enable();
    private void OnDisable() => InputActions.Disable();

    private void Start()
    {
        // 위치 초기화 계산
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
        // ★ [추가] 차징 중이면 스왑 로직 접근 불가 (제일 먼저 체크)
        if (GameManager.instance.player != null && GameManager.instance.player.isCharging) return;

        bool isHolding = InputActions.Player.Swap.IsPressed();

        if (IsSwapping) return;

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

            if (gunSpriteTransform != null)
            {
                gunSpriteTransform.localPosition = Vector3.Lerp(gunOriginPos, gunTargetPos, t);
            }

            if (swordSpriteTransform != null)
            {
                swordSpriteTransform.localPosition = Vector3.Lerp(swordTargetPos, swordOriginPos, t);
            }

            yield return null;
        }

        SetAlpha(swordRenderer, 0f);
        SetAlpha(gunRenderer, 1f);
        IsSwapping = false;
        if (gunSpriteTransform != null) gunSpriteTransform.localPosition = gunTargetPos;

        if (GameManager.instance.weaponUI != null)
        {
            GameManager.instance.weaponUI.gunPanel.SetActive(true);
            GameManager.instance.weaponUI.swordPanel.SetActive(false);
        }
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

            if (gunSpriteTransform != null)
            {
                gunSpriteTransform.localPosition = Vector3.Lerp(gunTargetPos, gunOriginPos, t);
            }

            if (swordSpriteTransform != null)
            {
                swordSpriteTransform.localPosition = Vector3.Lerp(swordOriginPos, swordTargetPos, t);
            }

            yield return null;
        }

        SetAlpha(swordRenderer, 1f);
        SetAlpha(gunRenderer, 0f);
        if (gunSpriteTransform != null) gunSpriteTransform.localPosition = gunOriginPos;
        IsSwapping = false;

        gunObject.SetActive(false);

        if (GameManager.instance.weaponUI != null)
        {
            GameManager.instance.weaponUI.gunPanel.SetActive(false);
            GameManager.instance.weaponUI.swordPanel.SetActive(true);
        }
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