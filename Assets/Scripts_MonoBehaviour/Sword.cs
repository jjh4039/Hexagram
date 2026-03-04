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
    [SerializeField] private float attackMoveSpeedMultiplier = 0.4f; // 검 공격 시 슬로우 정도
    public float AttackMoveSpeedMultiplier => attackMoveSpeedMultiplier; // 위 변수 외부 노출용

    [Header("Audio")]
    [SerializeField] private AudioClip sfxSlash;

    private float nextAttackUnlockTime = 0f;
    private float lastInputTime = -10f;
    private float lastAttackStartTime = 0f;
    private int comboStep = 0;
    private Vector2 mouseWorldPos;

    private void Awake()
    {
        weaponManager = GetComponentInParent<WeaponManager>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
    }

    public void TriggerAttack()
    {
        lastInputTime = Time.time;
        TryAttack();
    }

    private void Update()
    {
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            Vector2 mouseScreenPos = GameManager.instance.player.Input.Player.Look.ReadValue<Vector2>();
            mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        }

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

        float currentDmgMultiplier = GameManager.instance.stats.damageMultiplier;

        if (GameManager.instance.stats.remainingStrongAttacks > 0)
        {
            currentDmgMultiplier *= 2.0f;
            GameManager.instance.stats.remainingStrongAttacks--;
            Debug.Log("강화 공격 발동!");
        }

        if (sfxSlash != null)
            SoundManager.instance.PlaySFX(sfxSlash, 0.8f, 0.2f);

        if (comboStep == 1) { anim.Play("Sword_Attack", -1, 0f); anim.ResetTrigger("Attack"); }
        else { anim.ResetTrigger("Attack"); anim.SetInteger("comboStep", comboStep); anim.SetTrigger("Attack"); }

        RotateWeapon();
        ApplyPhysics();
        SpawnSlashEffect();
    }

    private void ApplyPhysics()
    {
        // 1. 공격 상태임을 알림
        GameManager.instance.player.isAttacking = true;

        // 2. 공격 시작 시 기존 속도를 초기화하여 관성 제거
        GameManager.instance.player.rigid.linearVelocity = Vector2.zero;

        // ★ [수정] 전진성(AddForce) 로직을 완전히 삭제했습니다. 이제 제자리에서 공격합니다.

        // 3. 공격 종료 판정 타이밍
        CancelInvoke("ResetAttackStatus");
        Invoke("ResetAttackStatus", 0.3f / attackSpeed);
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

        if (angle > 90 || angle < -90)
        {
            pivotScale.y = 1f;
            spriteRenderer.transform.localPosition = new Vector3(offset, 0, 0);
        }
        else
        {
            pivotScale.y = -1f;
            spriteRenderer.transform.localPosition = new Vector3(offset, 0, 0);
        }

        transform.localScale = pivotScale;
    }

    private void ResetAttackStatus()
    {
        GameManager.instance.player.isAttacking = false;
    }
}