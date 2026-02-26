using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Queue를 사용하기 위해 추가

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

    [Header("Debug / Testing")]
    [Tooltip("0: 랜덤(1~4), 1~4: 해당 패턴만 무한 반복")]
    [SerializeField][Range(0, 4)] private int forcePatternIndex = 3; // 기본값을 3으로 두어 바로 테스트!

    [Header("Pattern 1: Dash")]
    [SerializeField] private float dashChargeTime = 1.0f; // 빠르게 차오름
    [SerializeField] private float dashSpeed = 40f;
    [SerializeField] private float dashDuration = 0.5f;
    [SerializeField] private float dashRecoveryTime = 0.5f;

    [Header("Pattern 1: Dash Indicator")]
    [SerializeField] private GameObject dashMaxRangeOrigin; 
    [SerializeField] private GameObject dashCurrentRangeOrigin;
    [SerializeField] private float dashRectWidth = 1.5f;
    [SerializeField] private float dashRectLength = 20f;
    [SerializeField] private float dashHomingStrength = 2.0f;
    [SerializeField] private Vector2 dashIndicatorOffset = new Vector2(-1f, 0f);

    [Header("Pattern 1: Dash Damage & Visuals")]
    [SerializeField] private float baseContactDamage = 10f;
    [SerializeField] private float dashDamageMultiplier = 1.5f;
    [SerializeField] private int trailPoolSize = 10;
    [SerializeField] private float trailSpawnDelay = 0.04f;
    [SerializeField] private float trailLifeTime = 0.4f;
    [SerializeField] private Color trailColor;
    [SerializeField] private GameObject dashDebrisPrefab;
    [SerializeField] private float debrisSpawnDelay = 0.05f;
    [SerializeField] private Vector2 debrisOffset = new Vector2(0f, 0f);

    [Header("Pattern 2: Point Blank AoE")]
    [SerializeField] private float aoeChargeTime = 2.0f;
    [SerializeField] private float hugeAoeRadius = 9f;
    [SerializeField] private float aoeDamage = 20f;
    [SerializeField] private float aoeRecoveryTime = 1.0f;
    [SerializeField] private LayerMask targetLayer;          // ★ Player 레이어 선택용
    [SerializeField] private GameObject aoeEffectPrefab;     // 폭발 이펙트 (선택)

    [Header("Pattern 2: AoE Indicator (자녀 오브젝트 연결)")]
    [SerializeField] private GameObject attackMaxRangeObj;   // ★ 자녀 연결용
    [SerializeField] private GameObject attackRangeObj;
    [SerializeField] private float aoeVisualScale = 4.5f;

    [Header("Pattern 2: Enrage Projectiles (Optional)")]
    [SerializeField] private GameObject aoeProjectilePrefab;   // 폭주 시 8방향으로 날아갈 투사체
    [SerializeField] private float aoeProjectileSpeed = 6f;    // 투사체 속도

    [Header("Pattern 3: Multi-Lines (Earth Spikes)")]
    [SerializeField] private float linesChargeTime = 1.5f;
    [SerializeField] private int minLines = 5;
    [SerializeField] private int maxLines = 8;              // 맵이 너무 꽉 차지 않게 8개 정도로 제한
    [SerializeField] private float linesRecoveryTime = 1.0f;
    [SerializeField] private float spikeRectWidth = 0.8f;  // ★ 추가: 패턴 3 전용 장판 너비 (얇게)

    // ★ 새로 추가된 송곳 관련 변수들
    [SerializeField] private GameObject earthSpikePrefab;   // 아까 만든 송곳 프리팹 연결!
    [SerializeField] private float spikeDamage = 15f;       // 송곳 1대당 데미지
    [SerializeField] private float spikeDistance = 1.5f;    // 송곳이 생성되는 간격 (촘촘함 조절)
    [SerializeField] private float spikeSpawnDelay = 0.05f; // 송곳이 솟아오르는 시간차 (파도 효과)
    [SerializeField] private float spikeLineLength = 20f;   // 직선의 최대 길이

    [Header("Pattern 4: Cross Grid")]
    [SerializeField] private float gridChargeTime = 2.5f;
    [SerializeField] private float gridStepDelay = 0.5f;
    [SerializeField] private float gridRecoveryTime = 1.5f;

    [Header("Indicators (Assign Prefabs)")]
    [SerializeField] private GameObject lineIndicatorPrefab;
    [SerializeField] private GameObject circleIndicatorPrefab;
    [SerializeField] private GameObject gridIndicatorPrefab;

    private GameObject maxRectInstance;
    private GameObject currentRectInstance;

    private Transform target;
    private Rigidbody2D rigid;
    private bool isAttacking = false;
    private bool isStunned = false;
    private bool isDashing = false; // 돌진 상태 체크용
    private Queue<GameObject> trailPool = new Queue<GameObject>();
    private GameObject trailContainer; // ★ 추가: 잔상들을 묶어둔 부모 폴더 기억용

    // Player.cs 에서 접근하기 위한 프로퍼티
    public bool IsDashing => isDashing;
    public float BaseContactDamage => baseContactDamage;
    public float DashDamageMultiplier => dashDamageMultiplier;

    protected override void Awake()
    {
        base.Awake();
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // ★ 시작할 때 잔상 오브젝트 10개를 미리 만들어서 큐에 넣습니다.
        InitializeTrailPool();
    }

    protected override void Start()
    {
        base.Start();

        if (BossHealthUI.instance != null)
            BossHealthUI.instance.SetupBoss(bossName, maxHealth);

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) target = playerObj.transform;

        ClearRectangles(); // ★ 시작할 때 모든 자녀 장판 숨기기
        StartCoroutine(Co_BossAI());
    }

    private void Update()
    {
        if (isDead || target == null) return;

        if (!isAttacking)
            LookAtTarget();
    }

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

    // ==========================================
    // AI 메인 루프
    // ==========================================
    IEnumerator Co_BossAI()
    {
        yield return new WaitForSeconds(2f);
        while (!isDead)
        {
            if (isStunned) { yield return null; continue; }

            // ★ 수정됨: forcePatternIndex가 0이면 1~4 랜덤, 아니면 설정한 패턴 번호 강제 실행
            int patternIndex = (forcePatternIndex == 0) ? Random.Range(1, 5) : forcePatternIndex;

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
        Debug.Log($"패턴 1: 돌진 장판 시작!");
        if (anim != null) anim.SetTrigger("ReadyDash");

        Vector2 currentDir = (target.position - transform.position).normalized;

        // ★ 수정됨: 씬에 있는 자식을 원본으로 삼아 복제본을 만듭니다.
        if (dashMaxRangeOrigin != null)
            maxRectInstance = Instantiate(dashMaxRangeOrigin, transform.position, Quaternion.identity);

        if (dashCurrentRangeOrigin != null)
            currentRectInstance = Instantiate(dashCurrentRangeOrigin, transform.position, Quaternion.identity);

        // 복제본이 보스를 따라다니지 않도록 부모 관계를 해제합니다.
        if (maxRectInstance != null) maxRectInstance.transform.SetParent(null);
        if (currentRectInstance != null) currentRectInstance.transform.SetParent(null);

        if (maxRectInstance != null) maxRectInstance.SetActive(true);
        if (currentRectInstance != null) currentRectInstance.SetActive(true);

        float timer = 0f;
        while (timer < dashChargeTime && !isDead)
        {
            timer += Time.deltaTime;
            Vector2 targetDir = (target.position - transform.position).normalized;
            currentDir = Vector2.Lerp(currentDir, targetDir, Time.deltaTime * dashHomingStrength);

            UpdateRectangle(maxRectInstance, currentDir, dashRectLength, dashRectWidth);
            UpdateRectangle(currentRectInstance, currentDir, dashRectLength * (timer / dashChargeTime), dashRectWidth);
            LookAtDirection(currentDir.x);

            yield return null;
        }

        ClearRectangles();

        if (anim != null) anim.SetTrigger("Dash");

        isDashing = true;
        // ★ 돌진과 동시에 잔상과 파티클 코루틴 시작
        Coroutine trailCoroutine = StartCoroutine(Co_SpawnTrail());
        Coroutine debrisCoroutine = StartCoroutine(Co_SpawnDebris());

        timer = 0f;
        while (timer < dashDuration && !isDead)
        {
            rigid.linearVelocity = currentDir * dashSpeed;
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        isDashing = false;
        // ★ 돌진이 끝나면 코루틴 정지
        if (trailCoroutine != null) StopCoroutine(trailCoroutine);
        if (debrisCoroutine != null) StopCoroutine(debrisCoroutine);

        rigid.linearVelocity = Vector2.zero;
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.1f, 0.08f);

        yield return new WaitForSeconds(dashRecoveryTime);
    }

    IEnumerator Co_Pattern2_AoE()
    {
        Debug.Log($"패턴 2: 모으기 {aoeChargeTime}초 -> 거대 폭발");
        if (anim != null) anim.SetTrigger("ReadySlam");

        // 1. 최대 범위 장판 켜기 및 크기 고정
        if (attackMaxRangeObj != null)
        {
            attackMaxRangeObj.SetActive(true);
            attackMaxRangeObj.transform.localPosition = Vector3.zero;
            // ★ 물리 범위와 상관없이 시각적 스케일(aoeVisualScale) 적용
            attackMaxRangeObj.transform.localScale = new Vector3(aoeVisualScale, aoeVisualScale, 1f);
        }

        // 2. 차오르는 장판 켜기 및 점(0)에서 시작
        if (attackRangeObj != null)
        {
            attackRangeObj.SetActive(true);
            attackRangeObj.transform.localPosition = Vector3.zero;
            attackRangeObj.transform.localScale = Vector3.zero;
        }

        float timer = 0f;

        // 3. 차징 진행 (원이 점점 커짐)
        while (timer < aoeChargeTime && !isDead)
        {
            timer += Time.deltaTime;
            float progress = timer / aoeChargeTime;

            if (attackRangeObj != null)
            {
                // ★ 여기도 aoeVisualScale 적용
                float currentScale = Mathf.Lerp(0f, aoeVisualScale, progress);
                attackRangeObj.transform.localScale = new Vector3(currentScale, currentScale, 1f);
            }
            yield return null;
        }

        ClearRectangles(); // 장판 숨기기

        if (isDead) yield break;

        // 4. 폭발 발동!
        if (anim != null) anim.SetTrigger("Slam");

        // ★ 화면 진동 아주 강하게!
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.35f, 0.3f);

        if (aoeEffectPrefab != null)
        {
            GameObject vfx = Instantiate(aoeEffectPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        // 5. 데미지 판정 (여기는 실제 물리 범위인 hugeAoeRadius를 그대로 사용!)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hugeAoeRadius, targetLayer);
        foreach (Collider2D hit in hits)
        {
            Player player = hit.GetComponent<Player>();
            if (player != null)
            {
                player.OnDamage(aoeDamage);
                Debug.Log($"보스의 거대 폭발에 적중! 데미지: {aoeDamage}");
            }
        }

        yield return new WaitForSeconds(aoeRecoveryTime);
    }

    IEnumerator Co_Pattern3_MultiLines()
    {
        Debug.Log($"패턴 3: 대지의 송곳 차징 시작! ({linesChargeTime}초)");
        if (anim != null) anim.SetTrigger("RaiseHand");

        int lineCount = Random.Range(minLines, maxLines + 1);

        List<GameObject> maxRects = new List<GameObject>();
        List<GameObject> currentRects = new List<GameObject>();
        List<Vector2> lineDirections = new List<Vector2>();

        // 1. 방향 설정 및 장판 생성
        for (int i = 0; i < lineCount; i++)
        {
            Vector2 dir;
            if (i == 0 && target != null)
            {
                // ★ 첫 번째 줄은 무조건 플레이어를 향하도록! (정밀 타격)
                dir = (target.position - transform.position).normalized;
            }
            else
            {
                // 나머지는 무작위 360도 방향
                float randomAngle = Random.Range(0f, 360f);
                dir = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad));
            }

            lineDirections.Add(dir);

            // ★ 돌진 예고 장판(Origin)을 재활용하여 다중 생성
            if (dashMaxRangeOrigin != null)
            {
                GameObject maxObj = Instantiate(dashMaxRangeOrigin, transform.position, Quaternion.identity);
                maxObj.transform.SetParent(null);
                maxObj.SetActive(true);
                maxRects.Add(maxObj);
            }
            if (dashCurrentRangeOrigin != null)
            {
                GameObject curObj = Instantiate(dashCurrentRangeOrigin, transform.position, Quaternion.identity);
                curObj.transform.SetParent(null);
                curObj.SetActive(true);
                currentRects.Add(curObj);
            }
        }

        float timer = 0f;

        // 2. 장판 차오르기 (모든 줄 동시에)
        while (timer < linesChargeTime && !isDead)
        {
            timer += Time.deltaTime;
            for (int i = 0; i < lineDirections.Count; i++)
            {
                if (i < maxRects.Count)
                    UpdateRectangle(maxRects[i], lineDirections[i], spikeLineLength, spikeRectWidth);
                if (i < currentRects.Count)
                    UpdateRectangle(currentRects[i], lineDirections[i], spikeLineLength * (timer / linesChargeTime), spikeRectWidth);
            }
            yield return null;
        }

        // 3. 장판 지우기
        foreach (var rect in maxRects) if (rect != null) Destroy(rect);
        foreach (var rect in currentRects) if (rect != null) Destroy(rect);

        if (isDead) yield break;

        // 4. 공격 실행! (모든 줄에서 동시에 파도처럼 송곳이 솟구침)
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.2f, 0.15f);

        foreach (Vector2 dir in lineDirections)
        {
            StartCoroutine(Co_SpawnSpikeWave(dir));
        }

        // 5. 보스 후딜레이
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

        yield return new WaitForSeconds(gridRecoveryTime);
    }

    // ==========================================
    // 오브젝트 풀링 및 시각 효과 (잔상, 파티클)
    // ==========================================

    private void InitializeTrailPool()
    {
        // ★ 지역 변수 대신 클래스 변수인 trailContainer에 저장합니다.
        trailContainer = new GameObject($"Guardian_TrailPool");

        for (int i = 0; i < trailPoolSize; i++)
        {
            GameObject trailObj = new GameObject($"BossTrail_{i}");
            trailObj.transform.SetParent(trailContainer.transform);
            trailObj.AddComponent<SpriteRenderer>();
            trailObj.SetActive(false);
            trailPool.Enqueue(trailObj);
        }
    }

    IEnumerator Co_SpawnTrail()
    {
        while (isDashing && !isDead)
        {
            if (spriteRenderer != null && spriteRenderer.sprite != null && trailPool.Count > 0)
            {
                GameObject trailObj = trailPool.Dequeue();

                // ★ 위치, 크기, 회전값을 보스의 "현재 순간" 상태로 덮어씌움
                trailObj.transform.position = transform.position;
                trailObj.transform.localScale = transform.localScale;
                trailObj.transform.rotation = transform.rotation; // (추가)

                SpriteRenderer trailSr = trailObj.GetComponent<SpriteRenderer>();
                trailSr.sprite = spriteRenderer.sprite;
                trailSr.color = trailColor;

                // (선택) 보스보다 무조건 뒤에 나오도록 Order in Layer 조절
                trailSr.sortingOrder = spriteRenderer.sortingOrder - 1;

                trailObj.SetActive(true);
                StartCoroutine(Co_FadeOutTrail(trailObj, trailSr));
            }
            yield return new WaitForSeconds(trailSpawnDelay);
        }
    }

    IEnumerator Co_FadeOutTrail(GameObject trailObj, SpriteRenderer trailSr)
    {
        float timer = 0f;
        Color startColor = trailSr.color;

        while (timer < trailLifeTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, timer / trailLifeTime);
            trailSr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        // ★ 삭제(Destroy)하는 대신 끄고 풀에 다시 반납합니다.
        trailObj.SetActive(false);
        trailPool.Enqueue(trailObj);
    }

    IEnumerator Co_SpawnDebris()
    {
        while (isDashing && !isDead)
        {
            if (dashDebrisPrefab != null)
            {
                // 보스 위치 + 발밑 오프셋
                Vector2 spawnPos = (Vector2)transform.position + debrisOffset;
                GameObject debris = Instantiate(dashDebrisPrefab, spawnPos, Quaternion.identity);

                // 파티클 시스템 수명에 맞춰 자동 파괴 (대략 1초)
                Destroy(debris, 1f);
            }
            yield return new WaitForSeconds(debrisSpawnDelay);
        }
    }

    private void Fire8WayProjectiles()
    {
        Debug.Log("보스 폭주! 8방향 파편 발사!");
        // 45도 간격으로 8번 반복
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            // 각도를 방향 벡터(Vector2)로 변환
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

            GameObject proj = Instantiate(aoeProjectilePrefab, transform.position, Quaternion.identity);
            EnemyProjectile projectileScript = proj.GetComponent<EnemyProjectile>();

            if (projectileScript != null)
            {
                projectileScript.Initialize(dir, aoeProjectileSpeed);
            }
        }
    }

    // ==========================================
    // 유틸리티
    // ==========================================
    void UpdateRectangle(GameObject rect, Vector2 dir, float length, float width)
    {
        if (rect == null) return;
        float facingDirection = Mathf.Sign(transform.localScale.x);
        Vector2 visualOffset = new Vector2(dashIndicatorOffset.x * facingDirection, dashIndicatorOffset.y);
        Vector2 startPos = (Vector2)transform.position + visualOffset;

        rect.transform.position = startPos;
        rect.transform.right = dir.normalized;

        SpriteRenderer sr = rect.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float adjustedLength = length + Mathf.Abs(dashIndicatorOffset.x);
            // ★ dashRectWidth 대신 인자로 받은 width를 사용!
            sr.size = new Vector2(adjustedLength, width);
        }
    }

    void ClearRectangles()
    {
        // 1번 패턴 (대쉬) : 월드에 생성된 것이므로 파괴(Destroy)
        if (maxRectInstance != null) Destroy(maxRectInstance);
        if (currentRectInstance != null) Destroy(currentRectInstance);

        // 2번 패턴 (폭발) : 자녀 오브젝트이므로 숨기기(SetActive)
        if (attackMaxRangeObj != null) attackMaxRangeObj.SetActive(false);
        if (attackRangeObj != null) attackRangeObj.SetActive(false);
    }

    private void LookAtTarget()
    {
        LookAtDirection(target.position.x - transform.position.x);
    }

    private void LookAtDirection(float dirX)
    {
        if (dirX > 0)
            transform.localScale = new Vector3(spriteScale, spriteScale, 1);
        else
            transform.localScale = new Vector3(-spriteScale, spriteScale, 1);
    }

    protected override void OnHit() { if (isDead) return; }

    protected override void Die()
    {
        StopAllCoroutines();
        ClearRectangles();

        // 1. UI 숨기기 (파괴 전에 실행)
        if (BossHealthUI.instance != null)
            BossHealthUI.instance.HideUI();

        // 2. 물리 정지 (파괴 전에 실행해야 에러가 안 남!)
        if (rigid != null)
        {
            rigid.linearVelocity = Vector2.zero;
            rigid.simulated = false;
        }

        // 3. 잔상 폴더 파괴 예약 (파괴 전에 실행)
        if (trailContainer != null)
        {
            Destroy(trailContainer, trailLifeTime);
        }

        // 4. ★ 모든 뒷정리가 끝난 후 마지막으로 본체를 파괴!
        base.Die();
    }

    IEnumerator Co_SpawnSpikeWave(Vector2 dir)
    {
        // 직선 길이에 맞춰 생성할 송곳의 총 개수 계산
        int spikeCount = Mathf.FloorToInt(spikeLineLength / spikeDistance);

        // i를 1부터 시작하여 보스 몸 정중앙이 아닌 살짝 앞부터 솟구치게 함
        for (int i = 1; i <= spikeCount; i++)
        {
            if (isDead) yield break; // 보스가 죽으면 파도 중단

            // 보스 중심에서 dir 방향으로 일정 간격(spikeDistance)만큼 떨어진 위치
            Vector2 spawnPos = (Vector2)transform.position + dir * (i * spikeDistance);

            if (earthSpikePrefab != null)
            {
                GameObject spikeObj = Instantiate(earthSpikePrefab, spawnPos, Quaternion.identity);
                EarthSpike spikeScript = spikeObj.GetComponent<EarthSpike>();
                if (spikeScript != null)
                {
                    // 아까 만든 스크립트의 Initialize 호출 (데미지 전달 및 솟아오름 시작)
                    spikeScript.Initialize(spikeDamage);
                }
            }

            // ★ 다음 송곳이 나오기까지 아주 짧은 대기시간 (좌르륵 솟구치는 느낌)
            yield return new WaitForSeconds(spikeSpawnDelay);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 2번 패턴(거대 폭발)의 실제 데미지 판정 범위 (노란색 선)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hugeAoeRadius);
    }
}