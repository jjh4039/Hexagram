using UnityEngine;
using UnityEngine.InputSystem;

public class Sword : MonoBehaviour
{
    private WeaponManager weaponManager;
    private SpriteRenderer spriteRenderer;
    private Animator anim;

    [SerializeField] private GameObject[] slashEffects;

    [Header("Timing Settings")]
    [SerializeField] private float activeDuration = 0.25f;
    [SerializeField] private float inputBufferTime = 0.5f;

    [Header("Stats")]
    [SerializeField] private float attackSpeed = 1.0f;

    [SerializeField] private float fadeSpeed = 10f;

    // ★ [추가] 검 휘두르는 소리 파일
    [Header("Audio")]
    [SerializeField] private AudioClip sfxSlash;

    private float nextAttackUnlockTime = 0f;
    private float lastInputTime = -10f;
    private float lastAttackStartTime = 0f;
    private int comboStep = 0;
    private Vector2 mouseWorldPos;

    // ... (Awake, OnEnable, OnDisable, OnAttackInput, Update, HandleChargingVisuals, TryAttack 등 기존 로직 유지) ...
    // (위쪽 코드는 바뀐 게 없어서 생략합니다. ExecuteAttack만 보시면 됩니다!)

    private void Awake()
    {
        weaponManager = GetComponentInParent<WeaponManager>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        if (anim != null) anim.Rebind();
        if (weaponManager?.InputActions != null)
            weaponManager.InputActions.Player.Attack.performed += OnAttackInput;
    }

    private void OnDisable()
    {
        if (weaponManager?.InputActions != null)
            weaponManager.InputActions.Player.Attack.performed -= OnAttackInput;
    }

    private void OnAttackInput(InputAction.CallbackContext context)
    {
        lastInputTime = Time.time;
        TryAttack();
    }

    private void Update()
    {
        Vector2 mouseScreenPos = weaponManager.InputActions.Player.Look.ReadValue<Vector2>();
        mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        bool isIdle = stateInfo.IsName("Base Layer.Sword_Idle") || stateInfo.IsName("Sword_Idle");

        if (isIdle && !anim.IsInTransition(0))
        {
            if (Time.time - lastAttackStartTime > 0.1f)
            {
                comboStep = 0;
                anim.speed = 1f;
                anim.ResetTrigger("Attack");
            }
            RotateWeapon();
        }

        TryAttack();
        HandleChargingVisuals();
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

    private void TryAttack()
    {
        if (GameManager.instance.player.isCharging) return;
        if (Time.time - lastInputTime > inputBufferTime) return;
        if (weaponManager.IsSwapping) return;
        if (weaponManager.CurrentWeapon != WeaponManager.WeaponType.Sword) return;
        if (comboStep >= 3) return;
        if (Time.time < nextAttackUnlockTime) return;

        ExecuteAttack();
    }

    private void ExecuteAttack()
    {
        lastInputTime = -10f;
        float resetThreshold = 0.33f / attackSpeed;
        if (Time.time - lastAttackStartTime > resetThreshold) comboStep = 0;
        if (comboStep >= 3) comboStep = 0;
        comboStep++;
        lastAttackStartTime = Time.time;
        anim.speed = attackSpeed;
        nextAttackUnlockTime = Time.time + (activeDuration / attackSpeed);

        float currentDmgMultiplier = GameManager.instance.player.damageMultiplier;

        if (GameManager.instance.player.remainingStrongAttacks > 0)
        {
            currentDmgMultiplier *= 2.0f;
            GameManager.instance.player.remainingStrongAttacks--;
            Debug.Log("강화 공격 발동!");
        }

        // ★ [핵심] 검 휘두르는 소리 재생!
        // 콤보마다 피치를 조금씩 다르게 줘도 좋지만, SoundManager가 알아서 랜덤 피치를 섞어주니 그냥 재생하면 됩니다.
        if (sfxSlash != null)
            SoundManager.instance.PlaySFX(sfxSlash, 0.8f, 0.2f);

        if (comboStep == 1) { anim.Play("Sword_Attack", -1, 0f); anim.ResetTrigger("Attack"); }
        else { anim.ResetTrigger("Attack"); anim.SetInteger("comboStep", comboStep); anim.SetTrigger("Attack"); }

        RotateWeapon();
        ApplyPhysics();
        SpawnSlashEffect();
    }

    // ... (ApplyPhysics, SpawnSlashEffect, RotateWeapon, ResetAttackStatus 등 나머지 기존 유지) ...
    private void ApplyPhysics()
    {
        GameManager.instance.player.isAttacking = true;
        GameManager.instance.player.rigid.linearVelocity = Vector2.zero;
        float force = (comboStep == 3) ? 1.5f : 0.75f;
        Vector2 pushDir = (mouseWorldPos - (Vector2)GameManager.instance.player.transform.position).normalized;
        GameManager.instance.player.rigid.AddForce(pushDir * force, ForceMode2D.Impulse);
        CancelInvoke("ResetAttackStatus");
        Invoke("ResetAttackStatus", 0.2f / attackSpeed);
    }
    private void SpawnSlashEffect()
    {
        Vector2 dir = (mouseWorldPos - (Vector2)transform.position).normalized;
        GameObject currentEffect = slashEffects[comboStep - 1];
        float spawnOffset = 0.4f + (comboStep * 0.25f);
        currentEffect.transform.position = (Vector2)transform.position + (dir * spawnOffset);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (transform.localScale.y < 0) currentEffect.transform.rotation = Quaternion.Euler(0, 0, angle + 95f);
        else currentEffect.transform.rotation = Quaternion.Euler(0, 0, angle - 95f);
        Vector3 effectScale = Vector3.one * 1.5f;
        if (transform.localScale.y < 0) effectScale.y *= -1;
        currentEffect.transform.localScale = effectScale;
        currentEffect.SetActive(false);
        currentEffect.SetActive(true);
    }
    private void RotateWeapon()
    {
        float offset = 0f;
        Vector2 lookDir = mouseWorldPos - (Vector2)transform.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        Vector3 pivotScale = Vector3.one;
        if (angle > 90 || angle < -90) { pivotScale.y = 1f; spriteRenderer.transform.localPosition = new Vector3(offset, 0, 0); }
        else { pivotScale.y = -1f; spriteRenderer.transform.localPosition = new Vector3(offset, 0, 0); }
        transform.localScale = pivotScale;
    }
    private void ResetAttackStatus()
    {
        GameManager.instance.player.isAttacking = false;
    }
}