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
    [SerializeField] private float swapDuration = 0.15f;   // 전환 속도

    public PlayerInput InputActions { get; private set; }

    private Coroutine swapCoroutine;
    private bool isRangedMode = false;
    private Vector3 gunOriginPos; // 총의 원래 위치 (0,0,0)
    private Vector3 gunTargetPos; // 총이 나갔을 때 위치
    private Vector3 swordOriginPos; // 총의 원래 위치 (0,0,0)
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
            gunOriginPos = Vector3.zero; // 미장착, 빠지는 위치
            gunTargetPos = new Vector3(0.5f, 0, 0); // 최종 장착 위치
        }

        if (swordSpriteTransform != null)
        {
            swordOriginPos = new Vector3(0, -1, 0);  // 미장착, 빠지는 위치
            swordTargetPos = new Vector3(0, -2, 0);  // 최종 장착 위치
        }

        // 초기 상태 설정 (검 들기)
        InitializeWeapon(swordObject, swordRenderer, 1f);
        InitializeWeapon(gunObject, gunRenderer, 0f);

        // 시작할 때 총 이미지는 원점에 둠
        if (gunSpriteTransform != null) gunSpriteTransform.localPosition = gunOriginPos;

        gunObject.SetActive(false);
    }

    private void Update()
    {
        bool isHolding = InputActions.Player.Swap.IsPressed();

        // 상태 변경 감지 (애니메이션 버퍼링)
        if (isHolding && !isRangedMode)
        {
            if (swapCoroutine != null) StopCoroutine(swapCoroutine);
            swapCoroutine = StartCoroutine(SwapToGun());
        }
        else if (!isHolding && isRangedMode)
        {
            if (swapCoroutine != null) StopCoroutine(swapCoroutine);
            swapCoroutine = StartCoroutine(SwapToSword());
        }
    }

    private IEnumerator SwapToGun()
    {
        isRangedMode = true;

        gunObject.SetActive(true);
        swordObject.SetActive(true);

        float elapsed = 0f;

        while (elapsed < swapDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / swapDuration;

            // 1. 투명도 교차 (Cross-Fade)
            SetAlpha(swordRenderer, 1f - t); // 검: 1 -> 0
            SetAlpha(gunRenderer, t);        // 총: 0 -> 1

            // 2. 위치 이동 (Slide Out) - 총이 앞으로 나감
            if (gunSpriteTransform != null)
            {
                // Lerp(시작점, 도착점, 진행도)
                gunSpriteTransform.localPosition = Vector3.Lerp(gunOriginPos, gunTargetPos, t);
            }

            if (swordSpriteTransform != null)
            {
                // swordStartPos(아래) -> swordOriginPos(제자리)
                swordSpriteTransform.localPosition = Vector3.Lerp(swordTargetPos, swordOriginPos, t);
            }

            yield return null;
        }

        // 최종 상태 고정
        SetAlpha(swordRenderer, 0f);
        SetAlpha(gunRenderer, 1f);
        if (gunSpriteTransform != null) gunSpriteTransform.localPosition = gunTargetPos;

        GameManager.instance.weaponUI.gunPanel.SetActive(true);
        GameManager.instance.weaponUI.swordPanel.SetActive(false);
        swordObject.SetActive(false);
    }

    private IEnumerator SwapToSword()
    {
        isRangedMode = false;

        swordObject.SetActive(true);
        gunObject.SetActive(true);

        float elapsed = 0f;

        while (elapsed < swapDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / swapDuration;

            // 1. 투명도 교차
            SetAlpha(swordRenderer, t);      // 검: 0 -> 1
            SetAlpha(gunRenderer, 1f - t);   // 총: 1 -> 0

            // 2. 위치 이동 (Slide In) - 총이 뒤로 빠짐 [핵심 요청사항]
            if (gunSpriteTransform != null)
            {
                // Lerp(도착점, 시작점, 진행도) -> 반대로 돌아옴
                gunSpriteTransform.localPosition = Vector3.Lerp(gunTargetPos, gunOriginPos, t);
            }

            if (swordSpriteTransform != null)
            {
                // swordOriginPos(제자리) -> swordStartPos(아래)
                swordSpriteTransform.localPosition = Vector3.Lerp(swordOriginPos, swordTargetPos, t);
            }

            yield return null;
        }

        // 최종 상태 고정
        SetAlpha(swordRenderer, 1f);
        SetAlpha(gunRenderer, 0f);
        if (gunSpriteTransform != null) gunSpriteTransform.localPosition = gunOriginPos;

        gunObject.SetActive(false);

        GameManager.instance.weaponUI.gunPanel.SetActive(false);
        GameManager.instance.weaponUI.swordPanel.SetActive(true);
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


