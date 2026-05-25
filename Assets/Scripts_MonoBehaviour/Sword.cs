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
    [SerializeField] private float attackMoveSpeedMultiplier = 0.25f;
    public float AttackMoveSpeedMultiplier => attackMoveSpeedMultiplier;

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
        if (GameManager.instance && GameManager.instance.player)
        {
            mouseWorldPos = GameManager.instance.player.mouseWorldPos;
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
    }

    private void TryAttack()
    {
        if (Time.time - lastInputTime > inputBufferTime)
            return;

        if (weaponManager.IsSwapping)
            return;

        if (weaponManager.CurrentWeapon != WeaponManager.WeaponType.Sword)
            return;

        if (GameManager.instance != null && GameManager.instance.player != null && GameManager.instance.player.IsDashing)
            return;

        if (comboStep >= 3)
            return;

        if (Time.time < nextAttackUnlockTime)
            return;

        ExecuteAttack();
    }

    private void ExecuteAttack()
    {
        float finalAttackSpeed = GetFinalAttackSpeed();

        lastInputTime = -10f;

        float resetThreshold = 0.33f / finalAttackSpeed;
        if (Time.time - lastAttackStartTime > resetThreshold)
            comboStep = 0;

        if (comboStep >= 3)
            comboStep = 0;

        comboStep++;
        lastAttackStartTime = Time.time;
        anim.speed = finalAttackSpeed;
        nextAttackUnlockTime = Time.time + (activeDuration / finalAttackSpeed);

        float strongAttackMultiplier = 1f;
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            bool consumed = GameManager.instance.player.TryConsumeStrongAttack(out float consumedMultiplier);
            if (consumed)
            {
                strongAttackMultiplier = consumedMultiplier;
            }
        }

        if (sfxSlash != null)
            SoundManager.instance.PlaySFX(sfxSlash, 0.8f, 0.2f);

        if (comboStep == 1)
        {
            anim.Play("Sword_Attack", -1, 0f);
            anim.ResetTrigger("Attack");
        }
        else
        {
            anim.ResetTrigger("Attack");
            anim.SetInteger("comboStep", comboStep);
            anim.SetTrigger("Attack");
        }

        RotateWeapon();
        ApplyPhysics(finalAttackSpeed);
        SpawnSlashEffect(strongAttackMultiplier);
    }

    private float GetFinalAttackSpeed()
    {
        if (GameManager.instance == null || GameManager.instance.stats == null)
            return 1f;

        PlayerStats stats = GameManager.instance.stats;
        float finalAttackSpeed = stats.attackSpeed * stats.diceAttackSpeedMultiplier;
        return Mathf.Max(0.01f, finalAttackSpeed);
    }

    private void ApplyPhysics(float finalAttackSpeed)
    {
        GameManager.instance.player.isAttacking = true;
        GameManager.instance.player.rigid.linearVelocity = Vector2.zero;

        CancelInvoke("ResetAttackStatus");
        Invoke("ResetAttackStatus", 0.3f / finalAttackSpeed);
    }

    private void SpawnSlashEffect(float strongAttackMultiplier)
    {
        Vector2 dir = (mouseWorldPos - (Vector2)transform.position).normalized;
        GameObject currentEffect = slashEffects[comboStep - 1];
        float spawnOffset = 0.4f + (comboStep * 0.25f);

        currentEffect.transform.position = (Vector2)transform.position + (dir * spawnOffset);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (transform.localScale.y < 0f)
            currentEffect.transform.rotation = Quaternion.Euler(0f, 0f, angle + 95f);
        else
            currentEffect.transform.rotation = Quaternion.Euler(0f, 0f, angle - 95f);

        Vector3 effectScale = Vector3.one * 1.5f;
        if (transform.localScale.y < 0f)
            effectScale.y *= -1f;

        currentEffect.transform.localScale = effectScale;

        Sword_Effect swordEffect = currentEffect.GetComponent<Sword_Effect>();
        if (swordEffect != null)
        {
            swordEffect.SetupAttackData(strongAttackMultiplier);
        }

        currentEffect.SetActive(false);
        currentEffect.SetActive(true);
    }

    private void RotateWeapon()
    {
        float offset = 0f;
        Vector2 lookDir = mouseWorldPos - (Vector2)transform.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Vector3 pivotScale = Vector3.one;

        if (angle > 90f || angle < -90f)
        {
            pivotScale.y = 1f;
            spriteRenderer.transform.localPosition = new Vector3(offset, 0f, 0f);
        }
        else
        {
            pivotScale.y = -1f;
            spriteRenderer.transform.localPosition = new Vector3(offset, 0f, 0f);
        }

        transform.localScale = pivotScale;
    }

    private void ResetAttackStatus()
    {
        GameManager.instance.player.isAttacking = false;
    }
}