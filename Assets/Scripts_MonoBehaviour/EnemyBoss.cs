using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Queue를 사용하기 위해 추가

public class EnemyBoss : Enemy
{
    [Header("Debug / Testing")]
    [Tooltip("0: 랜덤(1~4), 1~4: 해당 패턴만 무한 반복")]
    [SerializeField][Range(0, 4)] private int forcePatternIndex = 1; // 돌진 테스트를 위해 임시로 1로 세팅
    [Tooltip("체력과 상관없이 강제로 폭주(Phase 2) 패턴을 켭니다.")]
    [SerializeField] private bool forceEnrage = false;

    [Header("Boss Specific Stats")]
    [SerializeField] private string bossName = "숲의 관리자";
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float spriteScale = 1f;

    [Header("Phase Settings")]
    [SerializeField] private bool isEnraged = false; // 체력 50% 이하 폭주 상태
    private SpriteRenderer spriteRenderer;

    [SerializeField] private float enragePauseTime = 2.0f; // 포효하며 멈춰있는 시간
    [SerializeField] private float enrageKnockbackForce = 30f; // 플레이어를 밀쳐내는 힘

    [Header("Global Attack Settings")]
    [SerializeField] private float dealTime = 1.0f;   // ★ 모든 패턴 종료 후 플레이어의 확정 딜타임 (휴식기)

    [Header("Pattern 1: Dash")]
    [SerializeField] private float dashChargeTime = 1.0f; // 빠르게 차오름
    [SerializeField] private float dashSpeed = 80f;
    [SerializeField] private float dashDuration = 0.5f;
    [SerializeField] private float dashRecoveryTime = 0.5f;

    [Header("Pattern 1: Dash Indicator")]
    [SerializeField] private GameObject dashMaxRangeOrigin;
    [SerializeField] private GameObject dashCurrentRangeOrigin;
    [SerializeField] private float dashRectWidth = 2f;
    [Tooltip("돌진 장판의 최대 허용 길이 (레이캐스트가 닿으면 이보다 짧아짐)")]
    [SerializeField] private float dashMaxLimitLength = 24f;
    [SerializeField] private float dashHomingStrength = 2.0f;
    [SerializeField] private Vector2 dashIndicatorOffset = new Vector2(-1f, 0f);
    [SerializeField] private AnimationCurve dashSpeedCurve = AnimationCurve.Linear(0, 1, 1, 0);

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
    [SerializeField] private float aoeProjectileSpeed = 10f;    // 투사체 속도

    [Header("Pattern 3: Multi-Lines (Earth Spikes)")]
    [SerializeField] private float linesChargeTime = 1.5f;
    [SerializeField] private int minLines = 7;
    [SerializeField] private int maxLines = 10;
    [SerializeField] private float linesRecoveryTime = 0.5f;
    [SerializeField] private float spikeRectWidth = 1.5f;
    [SerializeField] private GameObject earthSpikePrefab;
    [SerializeField] private float spikeDamage = 15f;
    [SerializeField] private float spikeDistance = 1.5f;
    [SerializeField] private float spikeSpawnDelay = 0.05f;
    [Tooltip("송곳 장판의 최대 허용 길이")]
    [SerializeField] private float spikeMaxLimitLength = 30f; // ★ 이름 변경됨 (spikeLineLength -> spikeMaxLimitLength)

    [Header("Pattern 4: Cross Grid (Vine)")]
    [SerializeField] private float gridStartupDelay = 1.5f;
    // 모든 예고가 끝난 뒤 폭발 직전의 긴장감 도는 대기 시간
    [SerializeField] private float gridChargeTime = 0.5f;

    // ★ 예고 장판이 서서히 켜졌다 꺼지는 시간 (기존 0.5 -> 1.0으로 2배 늘림!)
    [SerializeField] private float gridTelegraphDuration = 0.8f;

    // ★ 예고 장판과 다음 예고 장판 사이의 짧은 정적 (기존 0.3 -> 0.5로 늘림)
    [SerializeField] private float gridTelegraphGap = 0.2f;

    // ★ 실제 덩굴이 발사되고 다음 덩굴이 발사되기 전의 대기 시간 
    // (기존 0.6 -> 1.5로 크게 늘려 덩굴이 완전히 사라진 뒤 다음 공격이 나오게 함)
    [SerializeField] private float gridFireDelay = 1f;

    // 패턴이 모두 끝난 후 보스의 후딜레이
    [SerializeField] private float gridRecoveryTime = 2.0f;

    [SerializeField] private GameObject giantVinePrefab;
    [SerializeField] private float vineDamage = 25f;

    [Tooltip("맵의 벽(Wall) 레이어를 선택하세요 (콜라이더 필수)")]
    [SerializeField] private LayerMask wallLayer;

    [Tooltip("장판의 두께")]
    [SerializeField] private float gridLineWidth = 3f;

    private Vector2 initialSpawnPos;

    [Tooltip("스폰 위치 기준 가로줄의 Y 오프셋 (위에서 아래 순서대로 입력)")]
    [SerializeField] private float[] gridOffsetY = new float[] { 4.5f, 3f, 1.5f, 0f, -1.5f, -3f, -4.5f };
    [Tooltip("스폰 위치 기준 세로줄의 X 오프셋 (왼쪽에서 오른쪽 순서대로 입력)")]
    [SerializeField] private float[] gridOffsetX = new float[] { -6f, -4.5f, -3f, -1.5f, 0f, 1.5f, 3f, 4.5f, 6f, 7.5f };

    [Header("Sound")]
    [SerializeField] private AudioClip bossBGM;
    [SerializeField] private AudioClip sfxDashFire;    // 돌진 쾅! 소리
    [SerializeField] private AudioClip sfxAoeExplode;  // 2번 거대 폭발 소리
    [SerializeField] private AudioClip sfxSpikeWave;   // 3번 가시 뻗어나가는 소리
    [SerializeField] private AudioClip sfxVineExplode; // 4번 덩굴 솟아오르는 소리
    [SerializeField] private AudioClip sfxEnrageRoar;  // 50% 체력 폭주 소리

    private GameObject maxRectInstance;
    private GameObject currentRectInstance;
    private GameObject sniperMaxInstance;
    private GameObject sniperCurrentInstance;

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
        initialSpawnPos = transform.position;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) target = playerObj.transform;

        ClearRectangles();

        if (bossBGM != null && SoundManager.instance != null)
        {
            SoundManager.instance.PlayBGM(bossBGM);
        }

        // ★ [추가] 처음 스폰될 때 보스를 잠든 것처럼 어둡게(회색) 만듭니다.
        if (spriteRenderer != null) spriteRenderer.color = Color.gray;

        // ★ [수정됨] 컷신 타이밍에 맞춰 동작하도록 콜백(Action) 세팅
        if (CinematicManager.instance != null)
        {
            StartCoroutine(CinematicManager.instance.Co_PlayBossIntro(
                this.transform,
                onSunsetStart: () =>
                {
                    // [신호 1] 지형이 어두워지기 시작하면, 보스는 서서히 흰색으로 밝아짐
                    StartCoroutine(Co_WakeUpColorLerp(CinematicManager.instance.SunsetDuration));
                },
                onSunsetDone: () =>
                {
                    // ★ [수정됨] 지속시간 2초, 강도 0.5, 감쇠율 1.0 (아주 서서히 줄어드는 긴 진동!)
                    if (CameraFollow.instance != null) CameraFollow.instance.HitShake(1.7f, 0.1f, 1f);

                    if (sfxSpikeWave != null) SoundManager.instance.PlaySFX(sfxSpikeWave, 1.2f);
                    if (anim != null) anim.SetTrigger("Start");
                },
                onFinish: () =>
                {
                    // [신호 3] 컷신이 완전히 끝나면 체력바 채우기 시작
                    StartCoroutine(Co_PostCutsceneSetup());
                }
            ));
        }
        else
        {
            StartCoroutine(Co_PostCutsceneSetup());
        }
    }

    private IEnumerator Co_WakeUpColorLerp(float duration)
    {
        if (spriteRenderer == null) yield break;

        float elapsed = 0f;
        Color startColor = Color.gray; // 시작 시 어두운 회색
        Color endColor = Color.white;  // 완료 시 원래 색상(흰색)

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(startColor, endColor, elapsed / duration);
            yield return null;
        }
        spriteRenderer.color = endColor;
    }

    // ★ [추가] 컷신 이후 체력바 연출과 AI 시작을 담당
    private IEnumerator Co_PostCutsceneSetup()
    {
        // 체력바 UI는 CinematicManager에서 미리 켰으므로 여기서는 켜기만 합니다.
        if (BossHealthUI.instance != null)
        {
            BossHealthUI.instance.SetupBoss(bossName, maxHealth);
        }

        // ==============================================================
        // ★ [테스트 로직] 컷신 연출이 다 끝난 후, 인스펙터 체크가 되어있다면!
        // ==============================================================
        if (forceEnrage)
        {
            Debug.Log("테스트 모드: 컷신 종료 후 보스 체력 강제 50% 삭감!");
            currentHealth = maxHealth * 0.5f;

            // UI에 깎인 체력 반영 (SetupBoss 애니메이션 씹히는 걸 방지하기 위해 0.5초 대기)
            yield return new WaitForSeconds(0.5f);
            if (BossHealthUI.instance != null) BossHealthUI.instance.UpdateBossHealth(currentHealth);
        }

        // 모든 준비가 끝났으므로 보스 전투 즉시 시작!
        StartCoroutine(Co_BossAI());
    }

    private void Update()
    {
        if (isDead || target == null) return;

        if (!isAttacking)
            LookAtTarget();

        // 끄기(테스트 해제)를 눌렀을 때의 복구 로직만 남겨둠
        if (!forceEnrage && isEnraged && currentHealth > maxHealth * 0.5f)
        {
            isEnraged = false;
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            Debug.Log("보스 폭주 강제 해제!");
        }
    }

    public override void TakeDamage(float damage, bool isCritical = false)
    {
        if (isDead) return;
        base.TakeDamage(damage, isCritical);
        if (BossHealthUI.instance != null) BossHealthUI.instance.UpdateBossHealth(currentHealth);
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.05f, 0.04f);
    }

    private IEnumerator Co_EnragePattern()
    {
        Debug.Log("보스 폭주 패턴 시작! 포효 및 넉백 발동!");
        isEnraged = true;
        isAttacking = true; // 다른 행동 잠금
        rigid.linearVelocity = Vector2.zero; // 제자리에 우뚝 섬

        // 1. 애니메이션 트리거 작동
        if (anim != null) anim.SetTrigger("Enrage");

        // 2. 포효 사운드 & 긴 카메라 진동 (1.5초 유지, 강도 0.4)
        if (sfxEnrageRoar != null) SoundManager.instance.PlaySFX(sfxEnrageRoar, 1.2f);
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(enragePauseTime, 0.2f, 1f);

        // 3. 플레이어 강제 넉백 발동
        KnockbackPlayer();

        // 4. 몸 색깔이 서서히 붉게 변함 (Lerp)
        float elapsed = 0f;
        Color startColor = Color.white;
        Color enrageColor = new Color(1f, 0.4f, 0.4f);

        while (elapsed < enragePauseTime)
        {
            elapsed += Time.deltaTime;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(startColor, enrageColor, elapsed / enragePauseTime);
            }
            yield return null;
        }

        // 최종 색상 고정 및 해제
        if (spriteRenderer != null) spriteRenderer.color = enrageColor;
        isAttacking = false;

        Debug.Log("폭주 포효 완료! 광폭화 전투 돌입.");
    }

    // ★ [추가] 플레이어 밀쳐내기 유틸리티 함수
    private void KnockbackPlayer()
    {
        if (GameManager.instance == null || GameManager.instance.player == null) return;

        Player playerScript = GameManager.instance.player;

        // 보스에서 플레이어로 향하는 방향 계산 (완전히 겹쳤을 때 에러 방지)
        Vector2 knockbackDir = (playerScript.transform.position - transform.position);
        if (knockbackDir == Vector2.zero) knockbackDir = Vector2.down;

        // ★ 넉백 힘을 인스펙터 값의 2배로 뻥튀기! 시간은 0.35초로 아주 짧고 굵게!
        playerScript.ApplyKnockback(knockbackDir.normalized, enrageKnockbackForce * 2f, 0.35f);
    }

    // ==========================================
    // AI 메인 루프
    // ==========================================
    // ==========================================
    // AI 메인 루프 (순서 교체!)
    // ==========================================
    IEnumerator Co_BossAI()
    {
        yield return new WaitForSeconds(0.5f);

        while (!isDead)
        {
            if (isStunned) { yield return null; continue; }

            // 1. 공격 패턴 실행
            int patternIndex = (forcePatternIndex == 0) ? Random.Range(1, 5) : forcePatternIndex;
            yield return StartCoroutine(ExecutePattern(patternIndex));

            // 2. 휴식 (딜 타임) - 패턴 끝나고 유저가 팰 시간을 확실히 줌
            yield return new WaitForSeconds(dealTime);

            // =========================================================
            // ★ [핵심 수정] 딜타임이 끝난 직후, 움직이기 '전'에 폭주 검사!
            // =========================================================
            if (!isEnraged && currentHealth <= maxHealth * 0.5f)
            {
                // 움직이지 않고 그 자리에서 즉시 포효!
                yield return StartCoroutine(Co_EnragePattern());
                yield return new WaitForSeconds(dealTime); // 포효 끝난 후 잠깐 딜레이
                continue; // 포효를 했으니 다가가지 않고 다음 공격 패턴으로 바로 넘어감
            }

            // 3. 폭주 조건이 아니라면 평소처럼 플레이어에게 다가가기
            yield return StartCoroutine(Co_MoveTowardsPlayer(1.4f));
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
        Debug.Log($"패턴 1: 돌진 시작!");

        // ★ 폭주 상태(또는 강제 폭주 테스트 켜짐)라면 돌진 횟수를 2번으로 설정
        int dashCount = (isEnraged || forceEnrage) ? 2 : 1;

        for (int i = 0; i < dashCount; i++)
        {
            if (anim != null) anim.SetTrigger("ReadyDash");

            // ★ 핵심 수정: 2번째 돌진일 때는 예고 시간, 돌진 시간, 그리고 '장판의 최대 길이'도 절반으로 줄입니다.
            float currentChargeTime = (i == 0) ? dashChargeTime : dashChargeTime * 0.5f;
            float currentDashDuration = (i == 0) ? dashDuration : dashDuration * 0.5f;
            float currentLimitLength = (i == 0) ? dashMaxLimitLength : dashMaxLimitLength * 0.5f; // <--- 이 부분 추가!

            Vector2 currentDir = (target.position - transform.position).normalized;

            if (dashMaxRangeOrigin != null)
                maxRectInstance = Instantiate(dashMaxRangeOrigin, transform.position, Quaternion.identity);

            if (dashCurrentRangeOrigin != null)
                currentRectInstance = Instantiate(dashCurrentRangeOrigin, transform.position, Quaternion.identity);

            if (maxRectInstance != null) { maxRectInstance.transform.SetParent(null); maxRectInstance.SetActive(true); }
            if (currentRectInstance != null) { currentRectInstance.transform.SetParent(null); currentRectInstance.SetActive(true); }

            float timer = 0f;
            while (timer < currentChargeTime && !isDead)
            {
                timer += Time.deltaTime;
                Vector2 targetDir = (target.position - transform.position).normalized;
                currentDir = Vector2.Lerp(currentDir, targetDir, Time.deltaTime * dashHomingStrength);

                // ★ dashMaxLimitLength 대신 새로 만든 currentLimitLength 를 넘겨줍니다.
                UpdateRectangle(maxRectInstance, currentDir, currentLimitLength, dashRectWidth);
                UpdateRectangle(currentRectInstance, currentDir, currentLimitLength * (timer / currentChargeTime), dashRectWidth);
                LookAtDirection(currentDir.x);

                yield return null;
            }

            ClearRectangles();

            if (anim != null) anim.SetTrigger("Dash");

            if (sfxDashFire != null) SoundManager.instance.PlaySFX(sfxDashFire, 1f, 0.1f);

            isDashing = true;
            Coroutine trailCoroutine = StartCoroutine(Co_SpawnTrail());
            Coroutine debrisCoroutine = StartCoroutine(Co_SpawnDebris());

            // ==============================================================
            // ★ [벽 끼임 방지 로직] 돌진 시작 위치와 안전 거리 계산
            // ==============================================================
            Vector2 startDashPos = transform.position;
            RaycastHit2D hit = Physics2D.Raycast(startDashPos, currentDir, currentLimitLength, wallLayer);

            // 벽에 닿으면 '벽까지의 거리 - 보스 몸통 크기(약 1.5f)' 만큼만 이동하도록 제한
            // 안 닿으면 목표했던 길이(currentLimitLength) 전체를 허용
            float safeDistance = hit.collider != null ? Mathf.Max(0, hit.distance - 1.5f) : currentLimitLength;

            timer = 0f;
            while (timer < currentDashDuration && !isDead)
            {
                // ★ 핵심: 보스가 안전 거리(safeDistance)만큼 이동했다면 강제로 while문 탈출! (속도 주입 중단)
                if (Vector2.Distance(startDashPos, transform.position) >= safeDistance)
                {
                    break;
                }

                float progress = timer / currentDashDuration;
                float speedMultiplier = dashSpeedCurve.Evaluate(progress);

                rigid.linearVelocity = currentDir * (dashSpeed * speedMultiplier);

                timer += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            // 돌진 종료 처리
            isDashing = false;
            if (trailCoroutine != null) StopCoroutine(trailCoroutine);
            if (debrisCoroutine != null) StopCoroutine(debrisCoroutine);

            rigid.linearVelocity = Vector2.zero; // 속도 완전히 초기화
            if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.1f, 0.08f);

            // 1차 돌진이 끝나고 2차 돌진을 시작하기 전, 아주 짧은 찰나의 정적(0.2초)
            if (i == 0 && dashCount > 1)
            {
                yield return new WaitForSeconds(0.2f);
            }
        } // for문 끝

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
                float currentScale = Mathf.Lerp(0f, aoeVisualScale, progress);
                attackRangeObj.transform.localScale = new Vector3(currentScale, currentScale, 1f);
            }
            yield return null;
        }

        ClearRectangles(); // 장판 숨기기

        if (isDead) yield break;

        // 4. 폭발 발동!
        if (anim != null) anim.SetTrigger("Slam");
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.35f, 0.3f);

        if (sfxAoeExplode != null) SoundManager.instance.PlaySFX(sfxAoeExplode, 0.9f);

        if (aoeEffectPrefab != null)
        {
            GameObject vfx = Instantiate(aoeEffectPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        // ==============================================================
        // ★ [폭주 기믹] 폭주 상태일 때 16방향으로 투사체 흩뿌리기!
        // ==============================================================
        if (isEnraged || forceEnrage)
        {
            FireProjectiles(32);
        }

        // 5. 데미지 판정
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

        // 1차 발사 방향과 벽에 부딪힌 끝점들을 저장할 리스트
        List<Vector2> lineDirections = new List<Vector2>();
        List<Vector2> wallHitPoints = new List<Vector2>();

        for (int i = 0; i < lineCount; i++)
        {
            Vector2 dir;
            // 타겟(플레이어) 정보는 이제 GameManager를 통해 안전하게 가져옵니다.
            Transform currentTarget = GameManager.instance?.player?.transform;

            if (i == 0 && currentTarget != null)
                dir = (currentTarget.position - transform.position).normalized;
            else
            {
                float randomAngle = Random.Range(0f, 360f);
                dir = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad));
            }
            lineDirections.Add(dir);

            // 미리 레이캐스트를 쏴서 벽의 끝점(Hit Point)을 계산해 둡니다.
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, spikeMaxLimitLength, wallLayer);
            float finalLength = hit.collider != null ? Mathf.Max(0, hit.distance - 1f) : spikeMaxLimitLength;
            wallHitPoints.Add((Vector2)transform.position + (dir * finalLength));

            if (dashMaxRangeOrigin != null)
            {
                GameObject maxObj = Instantiate(dashMaxRangeOrigin, transform.position, Quaternion.identity);
                maxObj.transform.SetParent(null); maxObj.SetActive(true); maxRects.Add(maxObj);
            }
            if (dashCurrentRangeOrigin != null)
            {
                GameObject curObj = Instantiate(dashCurrentRangeOrigin, transform.position, Quaternion.identity);
                curObj.transform.SetParent(null); curObj.SetActive(true); currentRects.Add(curObj);
            }
        }

        // [Phase 1: 1차 예고]
        float timer = 0f;
        while (timer < linesChargeTime && !isDead)
        {
            timer += Time.deltaTime;
            for (int i = 0; i < lineDirections.Count; i++)
            {
                if (i < maxRects.Count) UpdateRectangle(maxRects[i], lineDirections[i], spikeMaxLimitLength, spikeRectWidth);
                if (i < currentRects.Count) UpdateRectangle(currentRects[i], lineDirections[i], spikeMaxLimitLength * (timer / linesChargeTime), spikeRectWidth);
            }
            yield return null;
        }

        foreach (var rect in maxRects) if (rect != null) Destroy(rect);
        foreach (var rect in currentRects) if (rect != null) Destroy(rect);

        if (isDead) yield break;
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.2f, 0.15f);

        // [Phase 2: 1차 발사]
        if (sfxSpikeWave != null) SoundManager.instance.PlaySFX(sfxSpikeWave, 1.2f); // ★ 1차 소리
        foreach (Vector2 dir in lineDirections)
        {
            StartCoroutine(Co_SpawnSpikeWave(transform.position, dir, spikeMaxLimitLength));
        }

        // ==============================================================
        // ★ [폭주 기믹] 올레인지 록온 (사방에서 플레이어를 향해 쇄도)
        // ==============================================================
        if (isEnraged || forceEnrage)
        {
            float maxSpikeCount = spikeMaxLimitLength / spikeDistance;
            float waveDuration = maxSpikeCount * spikeSpawnDelay;
            yield return new WaitForSeconds(waveDuration + 0.1f); // 1차 파도가 끝날 때까지 대기

            Transform targetPlayer = GameManager.instance?.player?.transform;
            if (targetPlayer != null)
            {
                maxRects.Clear();
                currentRects.Clear();
                List<Vector2> reverseDirections = new List<Vector2>();

                // 저장해둔 벽의 끝점(wallHitPoints)에서 플레이어를 바라보는 새로운 방향을 계산합니다.
                for (int i = 0; i < wallHitPoints.Count; i++)
                {
                    Vector2 startPos = wallHitPoints[i];
                    Vector2 toPlayerDir = ((Vector2)targetPlayer.position - startPos).normalized;
                    reverseDirections.Add(toPlayerDir);

                    if (dashMaxRangeOrigin != null)
                    {
                        GameObject maxObj = Instantiate(dashMaxRangeOrigin, startPos, Quaternion.identity);
                        maxObj.transform.SetParent(null); maxObj.SetActive(true); maxRects.Add(maxObj);
                    }
                    if (dashCurrentRangeOrigin != null)
                    {
                        GameObject curObj = Instantiate(dashCurrentRangeOrigin, startPos, Quaternion.identity);
                        curObj.transform.SetParent(null); curObj.SetActive(true); currentRects.Add(curObj);
                    }
                }

                // [Phase 3: 2차 예고 (플레이어 조준)]
                float returnChargeTime = linesChargeTime * 0.6f; // 살짝 빠르게!
                timer = 0f;
                while (timer < returnChargeTime && !isDead)
                {
                    timer += Time.deltaTime;
                    float progress = timer / returnChargeTime;

                    for (int i = 0; i < reverseDirections.Count; i++)
                    {
                        Vector2 startPos = wallHitPoints[i];
                        // 이번엔 UpdateRectangle을 벽에서부터 쏘는 용도로 재활용합니다.
                        if (i < maxRects.Count) UpdateRectangleFromPoint(maxRects[i], startPos, reverseDirections[i], spikeMaxLimitLength, 1f, spikeRectWidth);
                        if (i < currentRects.Count) UpdateRectangleFromPoint(currentRects[i], startPos, reverseDirections[i], spikeMaxLimitLength, progress, spikeRectWidth);
                    }
                    yield return null;
                }

                foreach (var rect in maxRects) if (rect != null) Destroy(rect);
                foreach (var rect in currentRects) if (rect != null) Destroy(rect);

                if (isDead) yield break;
                if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.25f, 0.2f);

                // [Phase 4: 2차 발사 (벽에서 플레이어로 쇄도)]
                if (sfxSpikeWave != null) SoundManager.instance.PlaySFX(sfxSpikeWave, 1.2f); // ★ 1차 소리
                for (int i = 0; i < wallHitPoints.Count; i++)
                {
                    StartCoroutine(Co_SpawnSpikeWave(wallHitPoints[i], reverseDirections[i], spikeMaxLimitLength));
                }

                // yield return new WaitForSeconds(0.2f);
            }
        }

        yield return new WaitForSeconds(linesRecoveryTime);
    }

    IEnumerator Co_Pattern4_CrossGrid()
    {
        Debug.Log($"패턴 4: 가로 -> 세로 -> 전부 터지는 궁극기 시작!");

        if (anim != null) anim.SetTrigger("GatherHands");
        yield return new WaitForSeconds(gridStartupDelay);

        List<int> hSet1 = new List<int>(); List<int> hSet2 = new List<int>();
        for (int i = 0; i < gridOffsetY.Length; i++) { if (i % 2 == 0) hSet1.Add(i); else hSet2.Add(i); }

        List<int> vSet1 = new List<int>(); List<int> vSet2 = new List<int>();
        for (int i = 0; i < gridOffsetX.Length; i++) { if (i % 2 == 0) vSet1.Add(i); else vSet2.Add(i); }

        // [Phase 1: 예고 (Telegraph) - 격자가 깜빡임]
        yield return StartCoroutine(Co_FlashTelegraph(hSet1, null));
        yield return StartCoroutine(Co_FlashTelegraph(null, vSet1));
        yield return StartCoroutine(Co_FlashTelegraph(hSet2, vSet2));

        // ==============================================================
        // ★ [폭주 기믹] Phase 2: 격자 예고가 모두 끝난 직후부터 스나이퍼 조준 시작!
        // ==============================================================
        Vector2 lockedSniperDir = Vector2.zero;
        Vector2 lockedSniperStartPos = Vector2.zero;
        float lockedSniperLength = 0f;
        bool isSniperTracking = false;

        if (isEnraged || forceEnrage)
        {
            if (dashMaxRangeOrigin != null)
            {
                sniperMaxInstance = Instantiate(dashMaxRangeOrigin, transform.position, Quaternion.identity);
                sniperMaxInstance.transform.SetParent(null);
                sniperMaxInstance.SetActive(true);
            }
            if (dashCurrentRangeOrigin != null)
            {
                sniperCurrentInstance = Instantiate(dashCurrentRangeOrigin, transform.position, Quaternion.identity);
                sniperCurrentInstance.transform.SetParent(null);
                sniperCurrentInstance.SetActive(true);
            }
            isSniperTracking = true;

            // ★ 수정됨: 장판이 차오르는 총 시간 = 쉬는시간 + 1차대기 + 2차대기
            float totalSniperChargeTime = gridChargeTime + (gridFireDelay * 2f);

            StartCoroutine(Co_SniperTrackingRoutine(
                (startPos, dir, len) => { lockedSniperStartPos = startPos; lockedSniperDir = dir; lockedSniperLength = len; },
                () => isSniperTracking,
                totalSniperChargeTime // 계산된 총 시간을 넘겨줌
            ));
        }

        // 이 시간 동안 스나이퍼 장판이 차오릅니다.
        yield return new WaitForSeconds(gridChargeTime);

        // [Phase 3: 폭발 (Fire) - 격자 1, 2차 발사]
        if (sfxVineExplode != null) SoundManager.instance.PlaySFX(sfxVineExplode, 1f, 0.1f); // ★ 1차
        FireVineSet(hSet1, null);
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.2f, 0.2f);
        yield return new WaitForSeconds(gridFireDelay);

        if (sfxVineExplode != null) SoundManager.instance.PlaySFX(sfxVineExplode, 1f, 0.1f); // ★ 2차
        FireVineSet(null, vSet1);
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.2f, 0.2f);
        yield return new WaitForSeconds(gridFireDelay);

        // ==============================================================
        // ★ [폭주 기믹] Phase 4: 3번째 덩굴 폭발 직전 타겟 고정 (Lock-On)
        // ==============================================================
        if (isEnraged || forceEnrage)
        {
            isSniperTracking = false; // 추적 중지! (이 순간의 방향과 위치로 락온)
            if (sniperMaxInstance != null) sniperMaxInstance.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0f, 0.5f);
            if (sniperCurrentInstance != null) sniperCurrentInstance.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0f, 0.9f); // 락온 경고
        }

        if (sfxVineExplode != null) SoundManager.instance.PlaySFX(sfxVineExplode, 1.2f, 0.1f); // ★ 3차 (십자)
        FireVineSet(hSet2, vSet2);
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.35f, 0.35f);

        // ==============================================================
        // ★ [폭주 기믹] Phase 5: 마지막 스나이퍼 덩굴 발사!
        // ==============================================================
        if (isEnraged || forceEnrage)
        {
            yield return new WaitForSeconds(1f); // 엇박자 딜레이

            if (giantVinePrefab != null && lockedSniperDir != Vector2.zero)
            {
                if (sfxVineExplode != null) SoundManager.instance.PlaySFX(sfxVineExplode, 1.2f, 0.05f);

                float angle = Mathf.Atan2(lockedSniperDir.y, lockedSniperDir.x) * Mathf.Rad2Deg - 90f;

                GameObject sniperVine = Instantiate(giantVinePrefab, lockedSniperStartPos, Quaternion.Euler(0, 0, angle));
                sniperVine.transform.localScale = new Vector3(2, 2, 1);

                GiantVine vineScript = sniperVine.GetComponent<GiantVine>();
                if (vineScript != null)
                {
                    vineScript.Fire(vineDamage * 1.5f, lockedSniperLength);
                }
                if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.5f, 0.4f);
            }

            if (sniperMaxInstance != null) Destroy(sniperMaxInstance);
            if (sniperCurrentInstance != null) Destroy(sniperCurrentInstance);
        }

        if (anim != null) anim.SetTrigger("StopGatherHands");
        yield return new WaitForSeconds(gridRecoveryTime);
    }

    private (Vector2 startPos, float length) GetLineData(bool isHorizontal, int index)
    {
        Vector2 origin = isHorizontal ?
            new Vector2(initialSpawnPos.x, initialSpawnPos.y + gridOffsetY[index]) :
            new Vector2(initialSpawnPos.x + gridOffsetX[index], initialSpawnPos.y);

        Vector2 dir1 = isHorizontal ? Vector2.left : Vector2.down;
        Vector2 dir2 = isHorizontal ? Vector2.right : Vector2.up;

        RaycastHit2D hit1 = Physics2D.Raycast(origin, dir1, 100f, wallLayer);
        RaycastHit2D hit2 = Physics2D.Raycast(origin, dir2, 100f, wallLayer);

        Debug.DrawRay(origin, dir1 * (hit1.collider != null ? hit1.distance : 100f), Color.red, 2f);
        Debug.DrawRay(origin, dir2 * (hit2.collider != null ? hit2.distance : 100f), Color.red, 2f);

        float dist1 = hit1.collider != null ? hit1.distance : 50f;
        float dist2 = hit2.collider != null ? hit2.distance : 50f;

        Vector2 startPos = origin + (dir1 * dist1);
        float totalLength = dist1 + dist2;

        return (startPos, totalLength);
    }

    IEnumerator Co_FlashTelegraph(List<int> hIndices, List<int> vIndices)
    {
        List<SpriteRenderer> srs = new List<SpriteRenderer>();
        List<GameObject> markers = new List<GameObject>();

        if (hIndices != null)
        {
            foreach (int index in hIndices)
            {
                // ★ dashMaxRangeOrigin 대신 dashCurrentRangeOrigin 사용
                if (dashCurrentRangeOrigin == null) continue;
                var data = GetLineData(true, index);

                GameObject telegraph = Instantiate(dashCurrentRangeOrigin, data.startPos, Quaternion.identity);
                telegraph.transform.SetParent(null);
                telegraph.transform.localScale = Vector3.one;
                telegraph.SetActive(true);

                SpriteRenderer sr = telegraph.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    telegraph.transform.rotation = Quaternion.Euler(0, 0, 0);
                    sr.size = new Vector2(data.length, gridLineWidth);
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f);
                    srs.Add(sr);
                }
                markers.Add(telegraph);
            }
        }

        if (vIndices != null)
        {
            foreach (int index in vIndices)
            {
                // ★ dashMaxRangeOrigin 대신 dashCurrentRangeOrigin 사용
                if (dashCurrentRangeOrigin == null) continue;
                var data = GetLineData(false, index);

                GameObject telegraph = Instantiate(dashCurrentRangeOrigin, data.startPos, Quaternion.identity);
                telegraph.transform.SetParent(null);
                telegraph.transform.localScale = Vector3.one;
                telegraph.SetActive(true);

                SpriteRenderer sr = telegraph.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    telegraph.transform.rotation = Quaternion.Euler(0, 0, 90);
                    sr.size = new Vector2(data.length, gridLineWidth);
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f);
                    srs.Add(sr);
                }
                markers.Add(telegraph);
            }
        }

        // 페이드 인
        float halfTime = gridTelegraphDuration / 2f;
        // ★ 만약 여전히 덜 선명하다면, 이 maxAlpha 값을 0.6f 나 0.8f 로 올려주시면 엄청 뚜렷해집니다!
        float maxAlpha = 0.7f;

        for (float t = 0; t < halfTime; t += Time.deltaTime)
        {
            float alpha = Mathf.Lerp(0f, maxAlpha, t / halfTime);
            foreach (var sr in srs) if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
            yield return null;
        }

        // 페이드 아웃
        for (float t = 0; t < halfTime; t += Time.deltaTime)
        {
            float alpha = Mathf.Lerp(maxAlpha, 0f, t / halfTime);
            foreach (var sr in srs) if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
            yield return null;
        }

        foreach (var m in markers) if (m != null) Destroy(m);
        yield return new WaitForSeconds(gridTelegraphGap);
    }

    private void FireVineSet(List<int> hIndices, List<int> vIndices)
    {
        if (giantVinePrefab == null) return;

        if (hIndices != null)
        {
            foreach (int index in hIndices)
            {
                var data = GetLineData(true, index);
                GameObject vine = Instantiate(giantVinePrefab, data.startPos, Quaternion.Euler(0, 0, -90));

                vine.transform.localScale = new Vector3(2, 2, 1);

                GiantVine vineScript = vine.GetComponent<GiantVine>();
                if (vineScript != null)
                    vineScript.Fire(vineDamage, data.length);
            }
        }

        if (vIndices != null)
        {
            foreach (int index in vIndices)
            {
                var data = GetLineData(false, index);
                GameObject vine = Instantiate(giantVinePrefab, data.startPos, Quaternion.Euler(0, 0, 0));

                vine.transform.localScale = new Vector3(2, 2, 1);

                GiantVine vineScript = vine.GetComponent<GiantVine>();
                if (vineScript != null)
                    vineScript.Fire(vineDamage, data.length);
            }
        }
    }

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

    private void FireProjectiles(int count)
    {
        if (aoeProjectilePrefab == null) return;

        Debug.Log($"보스 폭주! {count}방향 파편 발사!");

        float angleStep = 360f / count; // 360도를 파편 개수로 나눔

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep;
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

            GameObject proj = Instantiate(aoeProjectilePrefab, transform.position, Quaternion.identity);
            EnemyProjectile projectileScript = proj.GetComponent<EnemyProjectile>();

            if (projectileScript != null)
            {
                projectileScript.Initialize(dir, aoeProjectileSpeed);
            }
        }
    }

    // ★ 폭주 기믹 전용: 맵 끝에서부터 보스 방향으로 돌아오는 송곳 파도
    IEnumerator Co_SpawnSpikeWave(Vector2 origin, Vector2 dir, float maxLength)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, maxLength, wallLayer);
        float availableLength = hit.collider != null ? Mathf.Max(0, hit.distance - 1f) : maxLength;
        int spikeCount = Mathf.FloorToInt(availableLength / spikeDistance);

        for (int i = 1; i <= spikeCount; i++)
        {
            if (isDead) yield break;

            Vector2 spawnPos = origin + dir * (i * spikeDistance);

            if (earthSpikePrefab != null)
            {
                GameObject spikeObj = Instantiate(earthSpikePrefab, spawnPos, Quaternion.identity);
                EarthSpike spikeScript = spikeObj.GetComponent<EarthSpike>();
                if (spikeScript != null) spikeScript.Initialize(spikeDamage);
            }

            yield return new WaitForSeconds(spikeSpawnDelay);
        }
    }

    // ★ 천천히 차오르는 양방향(관통형) 스나이퍼 추적 코루틴
    IEnumerator Co_SniperTrackingRoutine(System.Action<Vector2, Vector2, float> onUpdateData, System.Func<bool> isTracking, float chargeDuration)
    {
        float timer = 0f;

        // ★ 추적 개선: 시작할 때의 초기 방향을 잡아둠
        Vector2 currentDir = Vector2.right;
        if (GameManager.instance?.player != null)
        {
            currentDir = (GameManager.instance.player.transform.position - transform.position).normalized;
        }

        while (isTracking() && !isDead)
        {
            if (GameManager.instance?.player != null)
            {
                timer += Time.deltaTime;
                // 위에서 넘겨준 정확한 총 시간(chargeDuration)에 맞춰 0 -> 1로 차오름
                float progress = Mathf.Clamp01(timer / chargeDuration);

                Vector2 bossPos = transform.position;
                Vector2 targetPos = GameManager.instance.player.transform.position;

                // ★ 추적 개선: 목표 방향으로 즉시 꺾이지 않고 부드럽게(Lerp) 따라감 
                // (숫자 5f를 조절하면 따라가는 속도를 더 느리게/빠르게 바꿀 수 있습니다)
                Vector2 targetDir = (targetPos - bossPos).normalized;
                currentDir = Vector2.Lerp(currentDir, targetDir, Time.deltaTime * 5f).normalized;

                // 1. 보스 기준 앞/뒤 양방향 레이캐스트 발사
                RaycastHit2D backHit = Physics2D.Raycast(bossPos, -currentDir, 100f, wallLayer);
                RaycastHit2D frontHit = Physics2D.Raycast(bossPos, currentDir, 100f, wallLayer);

                float backDist = backHit.collider != null ? backHit.distance : 50f;
                float frontDist = frontHit.collider != null ? frontHit.distance : 50f;

                // 2. 장판의 시작점을 '보스 등 뒤의 벽'으로 설정 (끼임 방지로 0.1f 띄움)
                Vector2 startPos = bossPos - (currentDir * (backDist - 0.1f));

                // 3. 총 길이 계산
                float totalLength = (backDist - 0.1f) + (frontDist - 0.5f);

                // 코루틴으로 정보 전달
                onUpdateData(startPos, currentDir, totalLength);

                // 4. Max 장판(어두운 배경)과 Current 장판(차오르는 붉은 선) 업데이트
                if (sniperMaxInstance != null)
                {
                    sniperMaxInstance.transform.position = startPos;
                    sniperMaxInstance.transform.right = currentDir;
                    sniperMaxInstance.GetComponent<SpriteRenderer>().size = new Vector2(totalLength, gridLineWidth);
                }

                if (sniperCurrentInstance != null)
                {
                    sniperCurrentInstance.transform.position = startPos;
                    sniperCurrentInstance.transform.right = currentDir;
                    sniperCurrentInstance.GetComponent<SpriteRenderer>().size = new Vector2(totalLength * progress, gridLineWidth);
                }
            }
            yield return null;
        }
    }

    // ==========================================
    // 유틸리티
    // ==========================================
    // ★ 레이캐스트가 적용된 장판 그리기 함수 (패턴 1, 3 공통 사용)
    void UpdateRectangle(GameObject rect, Vector2 dir, float requestedLength, float width)
    {
        if (rect == null) return;
        float facingDirection = Mathf.Sign(transform.localScale.x);
        Vector2 visualOffset = new Vector2(dashIndicatorOffset.x * facingDirection, dashIndicatorOffset.y);
        Vector2 startPos = (Vector2)transform.position + visualOffset;

        rect.transform.position = startPos;
        rect.transform.right = dir.normalized;

        // ★ 레이캐스트로 벽 감지
        RaycastHit2D hit = Physics2D.Raycast(startPos, dir.normalized, requestedLength, wallLayer);

        // 벽에 닿았다면 거리를 잰 뒤 살짝(0.5f) 여백을 주고, 안 닿았다면 요청받은 원래 길이를 씁니다.
        float finalLength = hit.collider != null ? Mathf.Max(0, hit.distance - 0.5f) : requestedLength;

        SpriteRenderer sr = rect.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float adjustedLength = finalLength + Mathf.Abs(dashIndicatorOffset.x);
            sr.size = new Vector2(adjustedLength, width);
        }
    }

    void UpdateRectangleFromPoint(GameObject rect, Vector2 startPos, Vector2 dir, float maxLimitLength, float progress, float width)
    {
        if (rect == null) return;

        rect.transform.position = startPos;
        rect.transform.right = dir.normalized;

        RaycastHit2D hit = Physics2D.Raycast(startPos, dir.normalized, maxLimitLength, wallLayer);
        float finalLength = hit.collider != null ? Mathf.Max(0, hit.distance - 0.5f) : maxLimitLength;

        SpriteRenderer sr = rect.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float currentLength = finalLength * progress;
            sr.size = new Vector2(currentLength, width);
        }
    }

    void ClearRectangles()
    {
        if (maxRectInstance != null) Destroy(maxRectInstance);
        if (currentRectInstance != null) Destroy(currentRectInstance);
        if (attackMaxRangeObj != null) attackMaxRangeObj.SetActive(false);
        if (attackRangeObj != null) attackRangeObj.SetActive(false);

        // ★ 조준선도 2개 다 지우도록 수정
        if (sniperMaxInstance != null) Destroy(sniperMaxInstance);
        if (sniperCurrentInstance != null) Destroy(sniperCurrentInstance);
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
}