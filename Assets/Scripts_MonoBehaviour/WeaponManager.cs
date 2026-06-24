using System.Collections;
using UnityEngine;

// 플레이어의 무기 교체 및 공격 명령을 분배하는 매니저
public class WeaponManager : MonoBehaviour
{
    [Header("Weapon Objects")] [SerializeField]
    private GameObject swordObject; // 검 객체

    [SerializeField] private GameObject gunObject; // 총 객체

    [Header("Visual Settings")] [SerializeField]
    private SpriteRenderer swordRenderer; // 검 렌더러

    [SerializeField] private SpriteRenderer gunRenderer; // 총 렌더러
    [SerializeField] private Transform swordSpriteTransform; // 검 이미지 트랜스폼
    [SerializeField] private Transform gunSpriteTransform; // 총 이미지 트랜스폼

    [Space] [SerializeField] private float swapDuration = 0.05f;

    public bool IsAiming => CurrentWeapon == WeaponType.Gun && !IsSwapping;

    public enum WeaponType
    {
        Sword,
        Gun
    }

    public bool IsSwapping { get; private set; } // 현재 교체 중인지
    public WeaponType CurrentWeapon { get; private set; } = WeaponType.Sword; // 현재 든 무기

    private Coroutine swapCoroutine;
    private Vector3 gunOriginPos;
    private Vector3 gunTargetPos;
    private Vector3 swordOriginPos;
    private Vector3 swordTargetPos;

    private Sword cachedSword; // 검 컴포넌트 캐싱
    private Gun cachedGun; // 총 컴포넌트 캐싱

    private void Start()
    {
        if (swordObject != null) cachedSword = swordObject.GetComponent<Sword>();
        if (gunObject != null) cachedGun = gunObject.GetComponent<Gun>();

        if (gunSpriteTransform != null)
        {
            gunOriginPos = Vector2.zero;
            gunTargetPos = new Vector3(0.05f, 0f, 0f);
        }

        if (swordSpriteTransform != null)
        {
            swordOriginPos = new Vector3(0f, -0.1f, 0f);
            swordTargetPos = new Vector3(0f, -0.2f, 0f);
        }

        InitializeWeapon(swordObject, swordRenderer, 1f);
        InitializeWeapon(gunObject, gunRenderer, 0f);

        if (gunSpriteTransform != null) gunSpriteTransform.localPosition = gunOriginPos;
        gunObject.SetActive(false);
    }

    private void Update()
    {
        if (GameManager.instance.player == null || InputStateManager.Instance == null) return;

        // 제어 불가능하거나 UI 모드일 때는 스왑 로직 정지
        if (!GameManager.instance.player.canControl ||
            InputStateManager.Instance.CurrentInputState == InputState.UI) return;

        if (IsSwapping) return;

        // 매니저를 통해 조준 키(우클릭) 상태를 확인
        bool isHoldingAim = CheckAimInput();

        if (isHoldingAim && CurrentWeapon == WeaponType.Sword)
        {
            if (swapCoroutine != null) StopCoroutine(swapCoroutine);
            swapCoroutine = StartCoroutine(SwapToGun());
        }
        else if (!isHoldingAim && CurrentWeapon == WeaponType.Gun)
        {
            if (swapCoroutine != null) StopCoroutine(swapCoroutine);
            swapCoroutine = StartCoroutine(SwapToSword());
        }
    }

    // 인풋 시스템을 통해 현재 조준 버튼이 눌려있는지 확인
    private bool CheckAimInput()
    {
        var state = InputStateManager.Instance.CurrentInputState;
        var actions = InputStateManager.Instance.Actions;

        if (state == InputState.Normal) return actions.Normal.Aim.ReadValue<float>() > 0.5f;
        if (state == InputState.Combat) return actions.Combat.Aim.ReadValue<float>() > 0.5f;

        return false;
    }

    public void OnAttackInput()
    {
        if (IsSwapping) return;

        if (CurrentWeapon == WeaponType.Sword && cachedSword != null) cachedSword.TriggerAttack();
        else if (CurrentWeapon == WeaponType.Gun && cachedGun != null) cachedGun.TriggerAttack();
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
                gunSpriteTransform.localPosition = Vector3.Lerp(gunOriginPos, gunTargetPos, t);
            if (swordSpriteTransform != null)
                swordSpriteTransform.localPosition = Vector3.Lerp(swordTargetPos, swordOriginPos, t);

            yield return null;
        }

        SetAlpha(swordRenderer, 0f);
        SetAlpha(gunRenderer, 1f);
        if (gunSpriteTransform != null) gunSpriteTransform.localPosition = gunTargetPos;

        swordObject.SetActive(false);
        IsSwapping = false;
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
                gunSpriteTransform.localPosition = Vector3.Lerp(gunTargetPos, gunOriginPos, t);
            if (swordSpriteTransform != null)
                swordSpriteTransform.localPosition = Vector3.Lerp(swordOriginPos, swordTargetPos, t);

            yield return null;
        }

        SetAlpha(swordRenderer, 1f);
        SetAlpha(gunRenderer, 0f);
        if (gunSpriteTransform != null) gunSpriteTransform.localPosition = gunOriginPos;

        gunObject.SetActive(false);
        IsSwapping = false;
    }

    public void OnSwapInput()
    {
        if (IsSwapping) return;

        if (swapCoroutine != null) StopCoroutine(swapCoroutine);

        if (CurrentWeapon == WeaponType.Sword) swapCoroutine = StartCoroutine(SwapToGun());
        else swapCoroutine = StartCoroutine(SwapToSword());
    }

    public float GetCurrentAttackMoveMultiplier()
    {
        return (CurrentWeapon == WeaponType.Sword && cachedSword != null) ? cachedSword.AttackMoveSpeedMultiplier : 0f;
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