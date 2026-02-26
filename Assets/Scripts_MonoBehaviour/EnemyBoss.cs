using UnityEngine;
using System.Collections;

public class EnemyBoss : Enemy
{
    [Header("Boss Specific Stats")]
    [SerializeField] private string bossName = "숲의 관리자";
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float spriteScale = 1f;

    [Header("Phase Settings")]
    [SerializeField] private bool isEnraged = false; // 체력 50% 이하 폭주 상태
    private SpriteRenderer spriteRenderer;

    [Header("Global Attack Settings")]
    [SerializeField] private float dealTime = 1.0f;   // ★ 모든 패턴 종료 후 플레이어의 확정 딜타임 (휴식기)

    [Header("Pattern 1: Dash")]
    [SerializeField] private float dashChargeTime = 1.0f; // ★ 2.0 -> 1.0으로 감소 (빠르게 차오름)
    [SerializeField] private float dashSpeed = 40f;
    [SerializeField] private float dashDuration = 0.5f;
    [SerializeField] private float dashRecoveryTime = 0.5f;

    [Header("Pattern 1: Dash Indicator")]
    [SerializeField] private GameObject maxDashRectPrefab;
    [SerializeField] private GameObject currentDashRectPrefab;
    [SerializeField] private float dashRectWidth = 1.5f;
    [SerializeField] private float dashRectLength = 20f;
    [SerializeField] private float dashHomingStrength = 2.0f;  // ★ 차징이 짧아졌으니 유도력도 조금 올림 (1.5 -> 2.0)
    [SerializeField] private Vector2 dashIndicatorOffset = new Vector2(-1f, -0.5f);

    [Header("Pattern 2: Point Blank AoE")]
    [SerializeField] private float aoeChargeTime = 2.0f;
    [SerializeField] private float hugeAoeRadius = 8f;
    [SerializeField] private float aoeRecoveryTime = 1.0f;

    [Header("Pattern 3: Multi-Lines")]
    [SerializeField] private float linesChargeTime = 1.5f;
    [SerializeField] private int minLines = 5;
    [SerializeField] private int maxLines = 10;
    [SerializeField] private float linesRecoveryTime = 0.5f;

    [Header("Pattern 4: Cross Grid")]
    [SerializeField] private float gridChargeTime = 2.5f;
    [SerializeField] private float gridStepDelay = 0.5f;
    [SerializeField] private float gridRecoveryTime = 1.5f;

    [Header("Indicators (Assign Prefabs)")]
    [SerializeField] private GameObject lineIndicatorPrefab;
    [SerializeField] private GameObject circleIndicatorPrefab;
    [SerializeField] private GameObject gridIndicatorPrefab;

    private Transform target;
    private Rigidbody2D rigid;
    private bool isAttacking = false;
    private bool isStunned = false;

    // 인디케이터 인스턴스 저장용 변수
    private GameObject maxRectInstance;
    private GameObject currentRectInstance;

    protected override void Awake()
    {
        base.Awake();
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected override void Start()
    {
        base.Start();

        if (BossHealthUI.instance != null)
            BossHealthUI.instance.SetupBoss(bossName, maxHealth);

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) target = playerObj.transform;

        StartCoroutine(Co_BossAI());
    }

    private void Update()
    {
        if (isDead || target == null) return;

        if (!isAttacking)
            LookAtTarget();
    }

    // (TakeDamage, EnterPhase2, Co_BossAI, Co_MoveTowardsPlayer, ExecutePattern 생략 - 기존과 완전 동일)
    public override void TakeDamage(float damage, bool isCritical = false)
    {
        if (isDead) return;
        base.TakeDamage(damage, isCritical);
        if (BossHealthUI.instance != null) BossHealthUI.instance.UpdateBossHealth(currentHealth);
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.05f, 0.04f);
        if (!isEnraged && currentHealth <= maxHealth * 0.5f) EnterPhase2();
    }

    private void EnterPhase2()
    {
        isEnraged = true;
        if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.5f, 0.5f);
        Debug.Log("보스 폭주! 패턴에 투사체 추가됨!");
    }

    IEnumerator Co_BossAI()
    {
        yield return new WaitForSeconds(2f);
        while (!isDead)
        {
            if (isStunned) { yield return null; continue; }
            int patternIndex = Random.Range(1, 2); // 1, 5가 정석, 현재 테스트
            yield return StartCoroutine(ExecutePattern(patternIndex));
            yield return new WaitForSeconds(dealTime);
            yield return StartCoroutine(Co_MoveTowardsPlayer(1.5f));
        }
    }

    IEnumerator Co_MoveTowardsPlayer(float moveDuration)
    {
        float timer = 0f;
        if (anim != null) anim.SetBool("isMoving", true);
        while (timer < moveDuration && !isDead && !isAttacking)
        {
            Vector2 dir = (target.position - transform.position).normalized;
            rigid.linearVelocity = dir * moveSpeed;
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        rigid.linearVelocity = Vector2.zero;
        if (anim != null) anim.SetBool("isMoving", false);
    }

    IEnumerator ExecutePattern(int patternIndex)
    {
        isAttacking = true;
        rigid.linearVelocity = Vector2.zero;
        if (anim != null) anim.SetBool("isMoving", false);

        switch (patternIndex)
        {
            case 1: yield return StartCoroutine(Co_Pattern1_Dash()); break;
            case 2: yield return StartCoroutine(Co_Pattern2_AoE()); break;
            case 3: yield return StartCoroutine(Co_Pattern3_MultiLines()); break;
            case 4: yield return StartCoroutine(Co_Pattern4_CrossGrid()); break;
        }

        isAttacking = false;
    }

    // ==========================================
    // 개별 패턴 로직
    // ==========================================

    IEnumerator Co_Pattern1_Dash()
    {
        Debug.Log($"패턴 1: 돌진 장판 차오르며 유도 시작!");
        if (anim != null) anim.SetTrigger("ReadyDash");

        // 초기 방향 설정
        Vector2 currentDir = (target.position - transform.position).normalized;

        // 인디케이터 생성
        if (maxDashRectPrefab != null) maxRectInstance = Instantiate(maxDashRectPrefab);
        if (currentDashRectPrefab != null) currentRectInstance = Instantiate(currentDashRectPrefab);

        if (maxRectInstance != null) maxRectInstance.SetActive(true);
        if (currentRectInstance != null) currentRectInstance.SetActive(true);

        float timer = 0f;

        // 1. 차징 & 플레이어 유도 (Lerp)
        while (timer < dashChargeTime && !isDead)
        {
            timer += Time.deltaTime;

            // 플레이어를 향하는 목표 방향
            Vector2 targetDir = (target.position - transform.position).normalized;

            // ★ 현재 방향에서 목표 방향으로 천천히 회전 (유도)
            currentDir = Vector2.Lerp(currentDir, targetDir, Time.deltaTime * dashHomingStrength);

            // 장판 시각적 업데이트
            UpdateRectangle(maxRectInstance, currentDir, dashRectLength);
            UpdateRectangle(currentRectInstance, currentDir, dashRectLength * (timer / dashChargeTime));

            // 보스도 장판 방향에 맞춰 좌우 반전
            LookAtDirection(currentDir.x);

            yield return null;
        }

        // 차징 끝, 인디케이터 삭제
        ClearRectangles();

        // 2. 최종 결정된 방향(currentDir)으로 돌진!
        if (anim != null) anim.SetTrigger("Dash");

        timer = 0f;
        while (timer < dashDuration && !isDead)
        {
            rigid.linearVelocity = currentDir * dashSpeed; // ★ 확정된 방향으로만 돌진
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rigid.linearVelocity = Vector2.zero;
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.1f, 0.08f);

        yield return new WaitForSeconds(dashRecoveryTime);
    }

    IEnumerator Co_Pattern2_AoE()
    {
        Debug.Log($"패턴 2: 모으기 {aoeChargeTime}초 -> 폭발");
        if (anim != null) anim.SetTrigger("ReadySlam");

        yield return new WaitForSeconds(aoeChargeTime);

        if (anim != null) anim.SetTrigger("Slam");

        // if (isEnraged) { /* 8방향 투사체 발사 */ }

        yield return new WaitForSeconds(aoeRecoveryTime);
    }

    IEnumerator Co_Pattern3_MultiLines()
    {
        Debug.Log($"패턴 3: 나뭇가지 장판 생성 후 {linesChargeTime}초 뒤 폭발");
        if (anim != null) anim.SetTrigger("RaiseHand");

        int lineCount = Random.Range(minLines, maxLines + 1);

        yield return new WaitForSeconds(linesChargeTime);

        // if (isEnraged) { /* 유도탄 발사 */ }

        yield return new WaitForSeconds(linesRecoveryTime);
    }

    IEnumerator Co_Pattern4_CrossGrid()
    {
        Debug.Log($"패턴 4: 격자 장판 생성 후 {gridChargeTime}초 뒤 폭발 시작");
        if (anim != null) anim.SetTrigger("GatherHands");

        yield return new WaitForSeconds(gridChargeTime);

        Debug.Log("  -> 1단계: 일부 가로 줄 폭발");
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.05f, 0.05f);
        yield return new WaitForSeconds(gridStepDelay);

        Debug.Log("  -> 2단계: 일부 세로 줄 폭발");
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.05f, 0.05f);
        yield return new WaitForSeconds(gridStepDelay);

        Debug.Log("  -> 3단계: 남은 가로 줄 폭발");
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.05f, 0.05f);
        yield return new WaitForSeconds(gridStepDelay);

        Debug.Log("  -> 4단계: 남은 세로 줄 폭발");
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.1f, 0.1f);

        // if (isEnraged) { /* 탄막 발사 */ }

        yield return new WaitForSeconds(gridRecoveryTime);
    }

    // ==========================================
    // 유틸리티
    // ==========================================
    // 장판 크기 및 회전 업데이트 함수 (오프셋 추가 버전)
    void UpdateRectangle(GameObject rect, Vector2 dir, float length)
    {
        if (rect == null) return;

        // dir은 보스가 돌진할 방향(앞쪽)입니다.
        Vector2 forward = dir.normalized;
        // 90도 회전시켜서 위/아래 방향 벡터를 구합니다.
        Vector2 up = new Vector2(-forward.y, forward.x);

        // ★ 보스 중심에서 X 오프셋(앞뒤)과 Y 오프셋(위아래)을 각각 적용한 시작 위치
        Vector2 startPos = (Vector2)transform.position
                           + (forward * dashIndicatorOffset.x)
                           + (up * dashIndicatorOffset.y);

        rect.transform.position = startPos;
        rect.transform.right = forward; // 장판이 가리키는 방향 유지

        SpriteRenderer sr = rect.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // 뒤에서 시작하는 만큼 길이를 보정해줍니다. (음수 x값을 뺀 만큼 길어짐)
            float adjustedLength = length - dashIndicatorOffset.x;
            sr.size = new Vector2(adjustedLength, dashRectWidth);
        }
    }

    void ClearRectangles()
    {
        if (maxRectInstance != null) Destroy(maxRectInstance);
        if (currentRectInstance != null) Destroy(currentRectInstance);
    }

    private void LookAtTarget()
    {
        LookAtDirection(target.position.x - transform.position.x);
    }

    // 방향만 받아서 좌우를 뒤집는 함수 분리 (돌진 시 유용함)
    private void LookAtDirection(float dirX)
    {
        if (dirX > 0)
            transform.localScale = new Vector3(spriteScale, spriteScale, 1);
        else
            transform.localScale = new Vector3(-spriteScale, spriteScale, 1);
    }

    protected override void OnHit()
    {
        if (isDead) return;
    }

    protected override void Die()
    {
        StopAllCoroutines();
        ClearRectangles(); // 죽으면 장판 지우기
        base.Die();

        if (BossHealthUI.instance != null) BossHealthUI.instance.HideUI();

        if (rigid != null)
        {
            rigid.linearVelocity = Vector2.zero;
            rigid.simulated = false;
        }
    }
}
